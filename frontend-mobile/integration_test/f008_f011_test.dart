// Testes de sistema da F008 a F011 na aplicação mobile (I079, QG-07), contra a API real indicada em
// `--dart-define=URL_API`. Especificação: m4-especificacao-testes-sistema-frontend-mobile-v03.md.
//
// As datas e as horas são calculadas no calendário de Lisboa (o do servidor): os compromissos e as tomas futuras ficam
// amanhã e a toma confirmável de hoje às 23:59, como na F006 web. Os cenários não dependem do fuso do emulador (UTC).
//
// Cada cenário tem um limite de 3 minutos, exceto os que esperam pelo aviso do compromisso (processo periódico do
// servidor, até 60 segundos, RNF-040).
@Timeout(Duration(minutes: 3))
library;

import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/data/notificacoes_repositorio.dart';
import 'package:dosecerta/models/notificacao_dto.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

import 'apoio.dart';

/// Repositório das notificações com uma resposta fixa da OP-51 ([respostas]) ou, sem respostas, sem ligação ao
/// servidor. Só nos cenários «[duplo]» (especificação, secção 6.4).
class NotificacoesFixas implements NotificacoesRepositorio {
  NotificacoesFixas([this.respostas]);

  final List<Map<String, dynamic>>? respostas;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<NotificacaoDto>> listar({String? de, String? ate}) async {
    final lista = respostas;
    if (lista == null) {
      throw const SemLigacaoErro();
    }
    return lista.map(NotificacaoDto.fromJson).toList();
  }
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  /// Hora da toma confirmável de hoje: existe na ficha acabada de criar e fica dentro da tolerância (F006).
  const horaDeHoje = '23:59';

  final hoje = dataLisboa();
  final amanha = dataLisboa(1);

  /// Posição vertical do texto, para verificar a ordem dos cartões.
  double topo(WidgetTester tester, Finder alvo) =>
      tester.getTopLeft(alvo.first).dy;

  Future<void> entrarComo(WidgetTester tester, ContaApi conta) async {
    await arrancar(tester, FonteFicticia());
    await iniciarSessao(tester, conta);
  }

  /// Abre E21 no separador Agenda e, se for preciso, avança para o período que contém amanhã.
  Future<void> abrirAgendaDeAmanha(
    WidgetTester tester, {
    String vista = 'Dia',
  }) async {
    await tocar(tester, find.byTooltip('Agenda'));
    await esperarPor(tester, find.byTooltip('Período seguinte'));
    if (vista != 'Dia') {
      await tocar(tester, find.text(vista));
    }
    await tocar(tester, find.widgetWithText(TextButton, 'Hoje'));
    final h = DateTime.parse(hoje);
    final a = DateTime.parse(amanha);
    final mudaPeriodo = switch (vista) {
      'Dia' => true,
      'Semana' => h.weekday == DateTime.sunday,
      _ => a.month != h.month,
    };
    if (mudaPeriodo) {
      await tocar(tester, find.byTooltip('Período seguinte'));
    }
  }

  /// Abre E23 (separador Registos) da ficha [nome].
  Future<void> abrirRegistos(WidgetTester tester, String nome) async {
    await abrirFicha(tester, nome);
    await esperarPor(tester, find.text(nome));
    await tocar(tester, find.widgetWithText(ChoiceChip, 'Registos'));
    await esperarPor(tester, find.widgetWithText(AppBar, 'Registos'));
  }

  /// Toca num botão do fim de um formulário. O `ListView` só constrói o botão quando fica perto do ecrã, e o teclado
  /// do emulador, ao abrir ou fechar, muda a altura do ecrã: fecha-se o teclado e desloca-se a lista até o botão
  /// estar construído e visível, e só então se toca.
  Future<void> tocarNoFim(WidgetTester tester, Finder alvo) async {
    for (var tentativa = 0; tentativa < 30; tentativa++) {
      FocusManager.instance.primaryFocus?.unfocus();
      await tester.pump(const Duration(milliseconds: 300));
      if (alvo.evaluate().isNotEmpty) {
        await tester.ensureVisible(alvo.first);
        await tester.pump(const Duration(milliseconds: 300));
        if (alvo.hitTestable().evaluate().isNotEmpty) {
          await tester.tap(alvo.hitTestable().first);
          await tester.pump();
          return;
        }
      }
      await tester.drag(find.byType(ListView).first, const Offset(0, -300));
    }
    expect(alvo.hitTestable(), findsWidgets);
  }

  Finder descricao() => find.widgetWithText(TextField, 'Descrição');

  group('F008 Agenda e compromissos', () {
    testWidgets(
      'TS-M-F008-01 regista um compromisso e mostra-o na agenda do dia com a ficha (RF-080, RF-084, RF-089, RF-116)',
      (tester) async {
        final ana = await contaNaApi('m-f008-registar');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await entrarComo(tester, ana);

        await tocar(tester, find.byTooltip('Agenda'));
        await esperarPor(tester, find.text('Registar compromisso'));
        // Com uma só ficha, abre E22 sem pedir a ficha.
        await tocar(tester, find.text('Registar compromisso'));
        await esperarPor(tester, campo('Local'));
        expect(find.text('Ficha: Maria Fictícia Santos'), findsOneWidget);
        expect(find.text('24 horas'), findsOneWidget);

        await tester.enterText(campo('Data'), dataNoEcra(amanha));
        await tester.enterText(campo('Hora'), '10:30');
        await tester.enterText(campo('Local'), 'Centro de Saúde Fictício');
        await tester.enterText(campo('Motivo'), 'Consulta de rotina');
        await tocarNoFim(
          tester,
          find.widgetWithText(FilledButton, 'Registar compromisso'),
        );
        await esperarPor(tester, find.text('Compromisso registado.'));

        await tocar(tester, find.byTooltip('Período seguinte'));
        await esperarPor(tester, find.text('Consulta de rotina'));
        expect(find.textContaining('10:30 · Compromisso'), findsOneWidget);
        expect(find.textContaining('Maria Fictícia Santos'), findsOneWidget);

        final compromissos = await compromissosNaApi(ana, fichaId);
        expect(compromissos, hasLength(1));
        expect(
          compromissos.single['dataHora'],
          instanteLisboa(amanha, '10:30'),
        );
        expect(compromissos.single['antecedenciaAvisoMinutos'], 1440);
      },
    );

    testWidgets(
      'TS-M-F008-02 recusa hora inválida e local em falta junto a cada campo, mantendo os dados (RF-080, RNF-028)',
      (tester) async {
        final ana = await contaNaApi('m-f008-validar');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await entrarComo(tester, ana);
        await tocar(tester, find.byTooltip('Agenda'));
        await esperarPor(tester, find.text('Registar compromisso'));
        await tocar(tester, find.text('Registar compromisso'));
        await esperarPor(tester, campo('Local'));

        await tester.enterText(campo('Data'), dataNoEcra(amanha));
        await tester.enterText(campo('Hora'), '25h');
        await tester.enterText(campo('Motivo'), 'Consulta de rotina');
        await tocarNoFim(
          tester,
          find.widgetWithText(FilledButton, 'Registar compromisso'),
        );

        await esperarPor(
          tester,
          find.text('Hora: indique a hora no formato hh:mm.'),
        );
        expect(find.text('Local: é obrigatório.'), findsOneWidget);
        expect(find.text(dataNoEcra(amanha)), findsOneWidget);
        expect(find.text('25h'), findsOneWidget);
        expect(find.text('Consulta de rotina'), findsOneWidget);
        expect(await compromissosNaApi(ana, fichaId), isEmpty);
      },
    );

    testWidgets(
      'TS-M-F008-03 edita o motivo e a antecedência do aviso de um compromisso (RF-081, RF-116)',
      (tester) async {
        final ana = await contaNaApi('m-f008-editar');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await compromissoNaApi(
          ana,
          fichaId,
          dataHora: instanteLisboa(amanha, '10:30'),
          motivo: 'Consulta de rotina',
        );
        await entrarComo(tester, ana);
        await abrirAgendaDeAmanha(tester);
        await esperarPor(tester, find.text('Consulta de rotina'));

        await tocar(tester, find.text('Consulta de rotina'));
        await esperarPor(tester, find.text('Editar compromisso'));
        await esperarPor(tester, find.text('Centro de Saúde Fictício'));
        await tester.enterText(campo('Motivo'), 'Consulta de cardiologia');
        await tocar(tester, find.text('24 horas'));
        await esperarPor(tester, find.text('2 horas'));
        await tester.tap(find.text('2 horas').last);
        await tester.pumpAndSettle();
        await tocarNoFim(tester, find.widgetWithText(FilledButton, 'Guardar'));

        await esperarPor(tester, find.text('Compromisso guardado.'));
        await esperarPor(tester, find.text('Consulta de cardiologia'));
        final compromisso = (await compromissosNaApi(ana, fichaId)).single;
        expect(compromisso['motivo'], 'Consulta de cardiologia');
        expect(compromisso['antecedenciaAvisoMinutos'], 120);
      },
    );

    testWidgets(
      'TS-M-F008-04 remove um compromisso depois de confirmar a remoção e a agenda deixa de o mostrar (RF-082)',
      (tester) async {
        final ana = await contaNaApi('m-f008-remover');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await compromissoNaApi(
          ana,
          fichaId,
          dataHora: instanteLisboa(amanha, '10:30'),
          motivo: 'Análises ao sangue',
        );
        await entrarComo(tester, ana);
        await abrirAgendaDeAmanha(tester);
        await esperarPor(tester, find.text('Análises ao sangue'));

        await tocar(tester, find.text('Análises ao sangue'));
        await esperarPor(tester, find.text('Remover compromisso'));
        await tocarNoFim(tester, find.text('Remover compromisso'));
        await esperarPor(tester, find.widgetWithText(FilledButton, 'Remover'));
        await tocar(tester, find.widgetWithText(FilledButton, 'Remover'));

        await esperarPor(tester, find.text('Compromisso removido.'));
        await esperarPor(tester, find.text('Não há eventos neste período.'));
        expect(await compromissosNaApi(ana, fichaId), isEmpty);
      },
    );

    testWidgets(
      'TS-M-F008-05 reúne os compromissos e as tomas de várias fichas nas vistas semana e mês, sem alterar as tomas (RF-083, RF-085 a RF-089)',
      (tester) async {
        final ana = await contaNaApi('m-f008-vistas');
        final maria = await fichaNaApi(ana, 'Maria Fictícia Santos');
        final joao = await fichaNaApi(ana, 'João Fictício Lopes');
        await compromissoNaApi(
          ana,
          maria,
          dataHora: instanteLisboa(amanha, '10:30'),
          motivo: 'Consulta de rotina',
        );
        await tratamentoDoDiaNaApi(
          ana,
          joao,
          medicamento: 'Metformina',
          data: amanha,
          hora: '09:00',
        );
        await entrarComo(tester, ana);

        for (final vista in ['Semana', 'Mês']) {
          await abrirAgendaDeAmanha(tester, vista: vista);
          await esperarPor(tester, find.text('Consulta de rotina'));
          await esperarPor(tester, find.textContaining('Metformina'));
          expect(
            find.textContaining('10:30 · Compromisso\nMaria Fictícia Santos'),
            findsOneWidget,
          );
          expect(
            find.textContaining('09:00 · Toma · Pendente\nJoão Fictício Lopes'),
            findsOneWidget,
          );
          // O dia aparece antes dos eventos e a toma das 09:00 antes do compromisso das 10:30.
          expect(find.text(dataNoEcra(amanha)), findsOneWidget);
          expect(
            topo(tester, find.textContaining('Metformina')),
            lessThan(topo(tester, find.text('Consulta de rotina'))),
          );
        }

        // Tocar na toma não abre nenhum ecrã de alteração nem de confirmação (RF-087, RF-088).
        await tester.tap(find.textContaining('Metformina'));
        await tester.pumpAndSettle();
        expect(find.text('Editar compromisso'), findsNothing);
        expect(find.text('Confirmar'), findsNothing);
        expect(find.byTooltip('Período seguinte'), findsOneWidget);
      },
    );

    testWidgets(
      'TS-M-F008-06 o Familiar consulta o compromisso sem o poder registar, alterar nem remover (RF-048, RF-080 a RF-082)',
      (tester) async {
        final ana = await contaNaApi('m-f008-principal');
        final rui = await contaNaApi('m-f008-familiar', nome: 'Rui Fictício');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await associarNaApi(ana, fichaId, rui, 'Familiar');
        await compromissoNaApi(
          ana,
          fichaId,
          dataHora: instanteLisboa(amanha, '10:30'),
          motivo: 'Consulta de rotina',
        );
        await entrarComo(tester, rui);
        await abrirAgendaDeAmanha(tester);
        await esperarPor(tester, find.text('Consulta de rotina'));
        expect(find.text('Registar compromisso'), findsNothing);

        await tocar(tester, find.text('Consulta de rotina'));
        await esperarPor(
          tester,
          find.text('O Familiar só consulta os compromissos.'),
        );
        expect(find.widgetWithText(AppBar, 'Compromisso'), findsOneWidget);
        expect(find.widgetWithText(FilledButton, 'Guardar'), findsNothing);
        expect(find.text('Remover compromisso'), findsNothing);
      },
    );
  });

  group('F009 Ocorrências e notas', () {
    testWidgets(
      'TS-M-F009-01 regista uma ocorrência com a data e a hora e mostra o autor e o momento do registo (RF-090, RF-092, RF-094)',
      (tester) async {
        final ana = await contaNaApi('m-f009-ocorrencia');
        await fichaNaApi(ana, 'Maria Fictícia Santos');
        await entrarComo(tester, ana);
        await abrirRegistos(tester, 'Maria Fictícia Santos');
        await esperarPor(
          tester,
          find.text('Ainda não há ocorrências registadas nesta ficha.'),
        );

        await tocar(tester, find.text('Novo registo'));
        await esperarPor(tester, find.text('Data e hora em que aconteceu'));
        // A data e a hora propostas são as de agora, em Lisboa; a ocorrência aconteceu há 10 minutos.
        final ocorrida = momentoLisboa(-10);
        await tester.enterText(descricao(), 'Queda ligeira sem lesões.');
        await tester.enterText(campo('Data'), dataNoEcra(ocorrida.data));
        await tester.enterText(campo('Hora'), ocorrida.hora);
        await tocarNoFim(tester, find.widgetWithText(FilledButton, 'Guardar'));

        await esperarPor(tester, find.text('Ocorrência registada.'));
        await esperarPor(tester, find.text('Queda ligeira sem lesões.'));
        expect(
          find.text(
            'Aconteceu em ${dataNoEcra(ocorrida.data)} ${ocorrida.hora}',
          ),
          findsOneWidget,
        );
        expect(
          find.textContaining(RegExp(r'^Registada em .* por Ana Fictícia$')),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'TS-M-F009-02 regista uma nota para os Cuidadores, que aparece no separador Notas (RF-091, RF-092, RF-095)',
      (tester) async {
        final ana = await contaNaApi('m-f009-nota');
        await fichaNaApi(ana, 'Maria Fictícia Santos');
        await entrarComo(tester, ana);
        await abrirRegistos(tester, 'Maria Fictícia Santos');
        await esperarPor(tester, find.text('Novo registo'));

        await tocar(tester, find.text('Novo registo'));
        await esperarPor(tester, find.text('Nota para os Cuidadores'));
        await tocar(tester, find.text('Nota para os Cuidadores'));
        await tester.pump();
        expect(find.text('Data e hora em que aconteceu'), findsNothing);
        await tester.enterText(
          descricao(),
          'Trazer a receita na próxima visita.',
        );
        await tocarNoFim(tester, find.widgetWithText(FilledButton, 'Guardar'));

        await esperarPor(tester, find.text('Nota registada.'));
        await esperarPor(
          tester,
          find.text('Trazer a receita na próxima visita.'),
        );
        expect(find.textContaining('por Ana Fictícia'), findsOneWidget);
        expect(find.textContaining('Aconteceu em'), findsNothing);
      },
    );

    testWidgets(
      'TS-M-F009-03 recusa a descrição em falta e a hora inválida junto a cada campo, mantendo os dados (RF-090, RNF-028)',
      (tester) async {
        final ana = await contaNaApi('m-f009-validar');
        await fichaNaApi(ana, 'Maria Fictícia Santos');
        await entrarComo(tester, ana);
        await abrirRegistos(tester, 'Maria Fictícia Santos');
        await esperarPor(tester, find.text('Novo registo'));
        await tocar(tester, find.text('Novo registo'));
        await esperarPor(tester, find.text('Data e hora em que aconteceu'));

        await tester.enterText(campo('Hora'), '25h');
        await tocarNoFim(tester, find.widgetWithText(FilledButton, 'Guardar'));

        await esperarPor(
          tester,
          find.textContaining('Descrição: é obrigatória.'),
        );
        expect(
          find.textContaining('Hora: indique a hora em que aconteceu'),
          findsOneWidget,
        );
        expect(find.text('25h'), findsOneWidget);
        expect(find.text('Ocorrência registada.'), findsNothing);
        expect(find.text('Data e hora em que aconteceu'), findsOneWidget);
      },
    );

    testWidgets(
      'TS-M-F009-04 outro Cuidador vê as ocorrências e as notas ao atualizar; o Familiar só vê as ocorrências (RF-093 a RF-095, RF-048)',
      (tester) async {
        final ana = await contaNaApi('m-f009-autora');
        final carla = await contaNaApi(
          'm-f009-cuidadora',
          nome: 'Carla Fictícia',
        );
        final rui = await contaNaApi('m-f009-familiar', nome: 'Rui Fictício');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await associarNaApi(ana, fichaId, carla, 'Cuidador');
        await associarNaApi(ana, fichaId, rui, 'Familiar');

        // A Carla tem o E23 aberto quando a Ana regista: puxar para baixo mostra os registos novos (RF-093).
        await entrarComo(tester, carla);
        await abrirRegistos(tester, 'Maria Fictícia Santos');
        await esperarPor(
          tester,
          find.text('Ainda não há ocorrências registadas nesta ficha.'),
        );
        final ocorrida = momentoLisboa(-5);
        await ocorrenciaNaApi(
          ana,
          fichaId,
          'Dormiu mal durante a noite.',
          instanteLisboa(ocorrida.data, ocorrida.hora),
        );
        await notaNaApi(ana, fichaId, 'Verificar a tensão ao almoço.');
        await puxarParaAtualizar(tester);
        await esperarPor(tester, find.text('Dormiu mal durante a noite.'));
        expect(find.textContaining('por Ana Fictícia'), findsOneWidget);
        await tocar(tester, find.text('Notas'));
        await esperarPor(tester, find.text('Verificar a tensão ao almoço.'));

        // O Familiar vê a ocorrência, sem o separador Notas nem «Novo registo» (RF-094, RF-095, RF-048).
        await entrarComo(tester, rui);
        await abrirRegistos(tester, 'Maria Fictícia Santos');
        await esperarPor(tester, find.text('Dormiu mal durante a noite.'));
        expect(find.text('Notas'), findsNothing);
        expect(find.text('Verificar a tensão ao almoço.'), findsNothing);
        expect(find.text('Novo registo'), findsNothing);
      },
    );
  });

  group('F010 Lista diária', () {
    testWidgets(
      'TS-M-F010-01 mostra as tarefas de hoje de várias fichas por hora e confirma uma toma com um toque (RF-100 a RF-102)',
      (tester) async {
        final ana = await contaNaApi('m-f010-confirmar');
        final clara = await fichaNaApi(ana, 'Clara Fictícia');
        final jorge = await fichaNaApi(ana, 'Jorge Fictício');
        await tratamentoDoDiaNaApi(
          ana,
          clara,
          medicamento: 'Metformina',
          data: hoje,
          hora: horaDeHoje,
        );
        await tratamentoDoDiaNaApi(
          ana,
          jorge,
          medicamento: 'Insulina',
          data: hoje,
          hora: '23:58',
        );
        await entrarComo(tester, ana);

        await esperarPor(tester, find.text('Lista de tarefas diária'));
        await esperarPor(tester, find.text('Metformina 500 mg'));
        expect(find.text('$horaDeHoje | Clara Fictícia'), findsOneWidget);
        expect(find.text('23:58 | Jorge Fictício'), findsOneWidget);
        expect(
          topo(tester, find.text('Insulina 500 mg')),
          lessThan(topo(tester, find.text('Metformina 500 mg'))),
        );
        expect(
          find.widgetWithText(FilledButton, 'Confirmar'),
          findsNWidgets(2),
        );

        await tocar(
          tester,
          find.descendant(
            of: find.ancestor(
              of: find.text('Metformina 500 mg'),
              matching: find.byType(Card),
            ),
            matching: find.widgetWithText(FilledButton, 'Confirmar'),
          ),
        );
        await esperarPor(tester, find.text('Toma confirmada.'));
        await esperarPor(
          tester,
          find.textContaining('Confirmada por Ana Fictícia em'),
        );
        expect(find.widgetWithText(FilledButton, 'Confirmar'), findsOneWidget);
      },
    );

    testWidgets(
      'TS-M-F010-02 amanhã mostra o compromisso sem confirmação e a toma ainda sem o botão Confirmar (RF-100, RF-087)',
      (tester) async {
        final ana = await contaNaApi('m-f010-amanha');
        final fichaId = await fichaNaApi(ana, 'Clara Fictícia');
        await compromissoNaApi(
          ana,
          fichaId,
          dataHora: instanteLisboa(amanha, '10:30'),
          motivo: 'Consulta de rotina',
        );
        await tratamentoDoDiaNaApi(
          ana,
          fichaId,
          medicamento: 'Metformina',
          data: amanha,
          hora: '09:00',
        );
        await entrarComo(tester, ana);
        await esperarPor(tester, find.text('Amanhã'));

        await tocar(tester, find.text('Amanhã'));
        await esperarPor(tester, find.text('Consulta de rotina'));
        await esperarPor(tester, find.text('Metformina 500 mg'));
        expect(find.text('Compromisso, sem confirmação'), findsOneWidget);
        expect(find.text('10:30 | Clara Fictícia'), findsOneWidget);
        expect(find.text('09:00 | Clara Fictícia'), findsOneWidget);
        expect(find.widgetWithText(FilledButton, 'Confirmar'), findsNothing);
      },
    );

    testWidgets(
      'TS-M-F010-03 confirmação repetida: a recusa indica quem confirmou e a lista passa a mostrar a confirmação (RF-104, RF-105)',
      (tester) async {
        final ana = await contaNaApi('m-f010-repetida');
        final carla = await contaNaApi('m-f010-outra', nome: 'Carla Fictícia');
        final fichaId = await fichaNaApi(ana, 'Clara Fictícia');
        await associarNaApi(ana, fichaId, carla, 'Cuidador');
        await tratamentoDoDiaNaApi(
          ana,
          fichaId,
          medicamento: 'Metformina',
          data: hoje,
          hora: horaDeHoje,
        );
        await entrarComo(tester, ana);
        await esperarPor(
          tester,
          find.widgetWithText(FilledButton, 'Confirmar'),
        );

        // A Carla confirma entretanto noutro dispositivo (OP-33).
        await confirmarTomaNaApi(carla, fichaId, hoje, 'Metformina');
        await tocar(tester, find.widgetWithText(FilledButton, 'Confirmar'));

        await esperarPor(
          tester,
          find.textContaining('A toma já foi confirmada por Carla Fictícia em'),
        );
        await esperarPor(
          tester,
          find.textContaining('Confirmada por Carla Fictícia em'),
        );
        expect(find.widgetWithText(FilledButton, 'Confirmar'), findsNothing);
      },
    );

    testWidgets(
      'TS-M-F010-04 a lista do Familiar não tem as tomas da ficha, que não pode confirmar (RF-100, RF-048)',
      (tester) async {
        final ana = await contaNaApi('m-f010-principal');
        final rui = await contaNaApi('m-f010-familiar', nome: 'Rui Fictício');
        final fichaId = await fichaNaApi(ana, 'Clara Fictícia');
        await associarNaApi(ana, fichaId, rui, 'Familiar');
        await tratamentoDoDiaNaApi(
          ana,
          fichaId,
          medicamento: 'Metformina',
          data: hoje,
          hora: horaDeHoje,
        );
        await entrarComo(tester, rui);

        await esperarPor(tester, find.text('Não há tarefas para este dia.'));
        expect(find.text('Metformina 500 mg'), findsNothing);
      },
    );
  });

  group('F011 Histórico de notificações', () {
    testWidgets(
      'TS-M-F011-01 mostra o convite, a ocorrência e o compromisso próximo, do mais recente para o mais antigo (RF-140, RF-141, RF-041, RF-115, RF-117)',
      timeout: const Timeout(Duration(minutes: 5)),
      (tester) async {
        final ana = await contaNaApi('m-f011-ana');
        final rui = await contaNaApi('m-f011-rui', nome: 'Rui Fictício');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await associarNaApi(ana, fichaId, rui, 'Cuidador');
        final ocorrida = momentoLisboa(-10);
        await ocorrenciaNaApi(
          ana,
          fichaId,
          'Queda ligeira sem lesões.',
          instanteLisboa(ocorrida.data, ocorrida.hora),
        );
        // O momento do aviso (30 minutos antes de daqui a 20) já passou: o servidor avisa no ciclo seguinte (RNF-040).
        final consulta = momentoLisboa(20);
        await compromissoNaApi(
          ana,
          fichaId,
          dataHora: instanteLisboa(consulta.data, consulta.hora),
          motivo: 'Consulta de rotina',
          antecedenciaAvisoMinutos: 30,
        );
        await esperarNotificacoes(rui, 3);
        // O aviso do compromisso fica com a hora do ciclo do processo periódico, que pode ter começado antes da
        // ocorrência: a ordem esperada é a dos momentos devolvidos pela OP-51, do mais recente para o mais antigo.
        const rotulos = {
          'ConviteRecebido': 'Convite recebido',
          'NovaOcorrencia': 'Nova ocorrência',
          'ProximoCompromisso': 'Compromisso próximo',
        };
        final recebidas =
            (await pedido(
              'GET',
              '/notificacoes',
              token: rui.token,
            )).lista.cast<Map<String, Object?>>()..sort(
              (a, b) =>
                  DateTime.parse(b['criadaEm']! as String)
                      .compareTo(DateTime.parse(a['criadaEm']! as String)),
            );
        final ordem = [for (final n in recebidas) rotulos[n['tipo']]!];
        expect(ordem.toSet(), rotulos.values.toSet());

        await entrarComo(tester, rui);
        await tocar(tester, find.byTooltip('Notificações'));
        await esperarPor(tester, find.text('Compromisso próximo'));
        expect(find.text('Nova ocorrência'), findsOneWidget);
        expect(find.text('Convite recebido'), findsOneWidget);
        for (var i = 1; i < ordem.length; i++) {
          expect(
            topo(tester, find.text(ordem[i - 1])),
            lessThan(topo(tester, find.text(ordem[i]))),
            reason: '«${ordem[i - 1]}» antes de «${ordem[i]}»',
          );
        }
        expect(
          find.textContaining(
            RegExp(r'^\d{2}/\d{2}/\d{4} \d{2}:\d{2} · Maria Fictícia Santos$'),
          ),
          findsNWidgets(3),
        );
        expect(
          find.textContaining(
            RegExp(r'^E-mail (enviado|pendente|não entregue)$'),
          ),
          findsNWidgets(3),
        );
      },
    );

    testWidgets(
      'TS-M-F011-02 só mostra as notificações do próprio Utilizador: o autor da ocorrência não é notificado (RF-140, RF-115)',
      (tester) async {
        final ana = await contaNaApi('m-f011-autor');
        final rui = await contaNaApi('m-f011-familiar', nome: 'Rui Fictício');
        final fichaId = await fichaNaApi(ana, 'Maria Fictícia Santos');
        await associarNaApi(ana, fichaId, rui, 'Familiar');
        final ocorrida = momentoLisboa(-5);
        await ocorrenciaNaApi(
          ana,
          fichaId,
          'Dormiu mal.',
          instanteLisboa(ocorrida.data, ocorrida.hora),
        );
        await esperarNotificacoes(rui, 2);

        await entrarComo(tester, ana);
        await tocar(tester, find.byTooltip('Notificações'));
        await esperarPor(tester, find.text('Não há notificações.'));
        expect(find.text('Nova ocorrência'), findsNothing);
      },
    );

    testWidgets(
      'TS-M-F011-03 atualiza ao puxar para baixo e filtra por datas, recusando um período inválido (RF-140, RNF-028)',
      (tester) async {
        final sofia = await contaNaApi('m-f011-vazio', nome: 'Sofia Fictícia');
        final ana = await contaNaApi('m-f011-convida');
        await entrarComo(tester, sofia);
        await tocar(tester, find.byTooltip('Notificações'));
        await esperarPor(tester, find.text('Não há notificações.'));

        final fichaId = await fichaNaApi(ana, 'João Fictício Lopes');
        await convidarNaApi(ana, fichaId, sofia.email, 'Familiar');
        await puxarParaAtualizar(tester);
        await esperarPor(tester, find.text('Convite recebido'));
        expect(find.textContaining('· João Fictício Lopes'), findsOneWidget);

        // Até anterior a De: recusado antes do pedido, junto ao campo.
        await tester.enterText(campo('De'), dataNoEcra(hoje));
        await tester.enterText(campo('Até'), dataNoEcra(dataLisboa(-1)));
        await tocar(tester, find.text('Filtrar'));
        await esperarPor(
          tester,
          find.text('Até: tem de ser igual ou posterior à data De.'),
        );

        // Ontem: sem notificações nesse período.
        await tester.enterText(campo('De'), dataNoEcra(dataLisboa(-1)));
        await tocar(tester, find.text('Filtrar'));
        await esperarPor(
          tester,
          find.text('Não há notificações neste período.'),
        );

        // Hoje: o convite volta a aparecer.
        await tester.enterText(campo('De'), dataNoEcra(hoje));
        await tester.enterText(campo('Até'), dataNoEcra(hoje));
        await tocar(tester, find.text('Filtrar'));
        await esperarPor(tester, find.text('Convite recebido'));
      },
    );

    testWidgets(
      'TS-M-F011-04 [duplo] tomas em atraso dos Cuidadores e dos Familiares e e-mail não entregue (RF-110 a RF-113, RF-118, RF-119)',
      (tester) async {
        Map<String, dynamic> notificacao(
          String tipo,
          String criadaEm,
          String medicamento,
          String previstaEm,
          String email,
        ) => {
          'id':
              '00000000-0000-4000-8000-0000000009${criadaEm.substring(11, 13)}',
          'tipo': tipo,
          'mensagem':
              'A toma de $medicamento de Maria Fictícia Santos das ${previstaEm.substring(11, 16)} não foi confirmada.',
          'fichaId': '00000000-0000-4000-8000-000000000101',
          'nomeFicha': 'Maria Fictícia Santos',
          'medicamento': medicamento,
          'previstaEm': previstaEm,
          'criadaEm': criadaEm,
          'email': email,
        };

        final ana = await contaNaApi('m-f011-duplo');
        await arrancar(
          tester,
          FonteFicticia(),
          outras: [
            notificacoesRepositorioProvider.overrideWithValue(
              NotificacoesFixas([
                notificacao(
                  'TomaEmAtraso',
                  '2026-11-25T09:00:00+00:00',
                  'Metformina',
                  '2026-11-25T08:00:00+00:00',
                  'Pendente',
                ),
                notificacao(
                  'TomaGraveFamiliares',
                  '2026-11-25T11:00:00+00:00',
                  'Insulina',
                  '2026-11-25T10:00:00+00:00',
                  'NaoEntregue',
                ),
              ]),
            ),
          ],
        );
        await iniciarSessao(tester, ana);
        await tocar(tester, find.byTooltip('Notificações'));

        await esperarPor(tester, find.text('Toma Grave em atraso prolongado'));
        expect(find.text('Toma em atraso'), findsOneWidget);
        expect(
          topo(tester, find.text('Toma Grave em atraso prolongado')),
          lessThan(topo(tester, find.text('Toma em atraso'))),
        );
        expect(
          find.text('25/11/2026 11:00 · Maria Fictícia Santos'),
          findsOneWidget,
        );
        expect(
          find.text('Medicamento: Insulina · prevista às 10:00'),
          findsOneWidget,
        );
        expect(
          find.text('Medicamento: Metformina · prevista às 08:00'),
          findsOneWidget,
        );
        expect(find.text('E-mail não entregue'), findsOneWidget);
        expect(find.text('E-mail pendente'), findsOneWidget);
      },
    );

    testWidgets(
      'TS-M-F011-05 [duplo] sem ligação ao servidor mostra a mensagem de erro e permite tentar novamente',
      (tester) async {
        final ana = await contaNaApi('m-f011-rede');
        await arrancar(
          tester,
          FonteFicticia(),
          outras: [
            notificacoesRepositorioProvider.overrideWithValue(
              NotificacoesFixas(),
            ),
          ],
        );
        await iniciarSessao(tester, ana);
        await tocar(tester, find.byTooltip('Notificações'));

        // O fornecedor das notificações usa as novas tentativas automáticas do Riverpod 3 (até 10, com intervalos
        // crescentes até 6,4 s): o erro só aparece cerca de 40 segundos depois. Espera-se até 90 segundos.
        final erro = find.text(SemLigacaoErro.mensagemPadrao);
        for (var i = 0; i < 450 && erro.evaluate().isEmpty; i++) {
          await tester.pump(const Duration(milliseconds: 200));
        }
        expect(erro, findsOneWidget);
        expect(find.text('Tentar novamente'), findsOneWidget);
        expect(find.text('Não há notificações.'), findsNothing);
      },
    );
  });
}
