import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/features/medicacao/apresentacao_tratamento.dart';
import 'package:dosecerta/features/medicacao/formulario_tratamento_controller.dart';
import 'package:dosecerta/features/medicacao/formulario_tratamento_page.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/ficheiro_recusado_erro.dart';
import 'package:dosecerta/shared/validador_tratamento.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_tratamentos.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  Future<void> abrir(WidgetTester tester, String inicio) async {
    app = AppFichas(inicio: inicio);
    app.fichas.juntar(fichaExemplo());
    app.tratamentos.juntar(tratamentoExemplo());
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

  Future<void> escrever(
    WidgetTester tester,
    String rotulo,
    String texto,
  ) async {
    await tester.ensureVisible(campo(rotulo));
    await tester.enterText(campo(rotulo), texto);
    await tester.pump();
  }

  Future<void> acrescentarHora(WidgetTester tester, String hora) async {
    await escrever(tester, 'Hora a acrescentar', hora);
    await tocar(tester, find.text('Adicionar hora'));
  }

  Future<void> preencher(WidgetTester tester) async {
    await escrever(tester, 'Medicamento', 'Metformina');
    await escrever(tester, 'Dose', '500 mg');
    await tocar(tester, find.text('Moderado'));
    await acrescentarHora(tester, '20:00');
    await acrescentarHora(tester, '8:00');
  }

  Finder registar() => find.widgetWithText(FilledButton, 'Registar tratamento');

  String rotulo(String campo, String regra) =>
      MensagensErro.comRotulo(ValidadorTratamento.rotulos[campo]!, regra);

  group('E16 Registar tratamento', () {
    testWidgets(
      'valida os obrigatórios junto dos campos, sem enviar (RF-050, RF-051, RNF-028)',
      (tester) async {
        await abrir(tester, AppRouter.novoTratamento('f-1'));

        expect(campo('Data de início'), findsOneWidget);
        expect(find.text('09/11/2026'), findsOneWidget);

        await tocar(tester, registar());

        expect(
          find.text(rotulo('medicamento', 'É obrigatório.')),
          findsOneWidget,
        );
        expect(find.text(rotulo('dose', 'É obrigatório.')), findsOneWidget);
        expect(
          find.text(
            rotulo('grauImportancia', 'Escolha Leve, Moderado ou Grave.'),
          ),
          findsOneWidget,
        );
        expect(
          find.text(rotulo('horarios', 'Indique pelo menos uma hora de toma.')),
          findsOneWidget,
        );
        expect(app.tratamentos.registos, 0);
      },
    );

    testWidgets(
      'recusa a data de fim anterior à de início e mantém os dados (RF-050)',
      (tester) async {
        await abrir(tester, AppRouter.novoTratamento('f-1'));
        await preencher(tester);
        await escrever(tester, 'Data de fim', '01/11/2026');

        await tocar(tester, registar());

        expect(
          find.text(
            rotulo('dataFim', 'Não pode ser anterior à data de início.'),
          ),
          findsOneWidget,
        );
        expect(find.text('Metformina'), findsOneWidget);
        expect(find.text('08:00'), findsOneWidget);
        expect(app.tratamentos.registos, 0);
      },
    );

    testWidgets('não aceita horas mal escritas nem repetidas', (tester) async {
      await abrir(tester, AppRouter.novoTratamento('f-1'));

      await acrescentarHora(tester, '25:00');
      expect(
        find.text(
          'Hora: indique uma hora no formato hh:mm, por exemplo 08:00.',
        ),
        findsOneWidget,
      );

      await acrescentarHora(tester, '08:00');
      await acrescentarHora(tester, '8:00');
      expect(find.text('Hora: a hora 08:00 já está na lista.'), findsOneWidget);

      await tocar(tester, find.byTooltip('Retirar 08:00'));
      expect(find.text('Ainda não há horas de toma.'), findsOneWidget);
    });

    testWidgets(
      'regista com os horários por ordem e volta à medicação (OP-27)',
      (tester) async {
        await abrir(tester, AppRouter.novoTratamento('f-1'));
        await preencher(tester);

        await tocar(tester, registar());

        final dados = app.tratamentos.ultimosDados!;
        expect(dados.toJson(), {
          'medicamento': 'Metformina',
          'dose': '500 mg',
          'grauImportancia': 'Moderado',
          'dataInicio': '2026-11-09',
          'horarios': ['08:00', '20:00'],
        });
        expect(app.tratamentos.ultimaReceita, isNull);
        expect(app.local, AppRouter.medicacaoFicha('f-1'));
        expect(find.text(FormularioTratamentoPage.registado), findsOneWidget);
      },
    );

    testWidgets('envia a receita escolhida pela galeria (RF-052)', (
      tester,
    ) async {
      await abrir(tester, AppRouter.novoTratamento('f-1'));
      await preencher(tester);

      await tocar(tester, find.text('Carregar: Receita médica (opcional)'));
      await tocar(tester, find.text('Galeria'));
      expect(find.text('receita.pdf'), findsOneWidget);

      await tocar(tester, registar());

      expect(app.tratamentos.ultimaReceita?.nome, 'receita.pdf');
      expect(app.tratamentos.ultimaReceita?.tipoConteudo, 'application/pdf');
    });

    testWidgets('recusa uma receita que não é PDF, JPG nem PNG (RNF-008)', (
      tester,
    ) async {
      await abrir(tester, AppRouter.novoTratamento('f-1'));
      app.fonte.erro = const FicheiroRecusadoErro(
        FicheiroRecusadoErro.formatoInvalido,
      );

      await tocar(tester, find.text('Carregar: Receita médica (opcional)'));
      await tocar(tester, find.text('Ficheiro'));

      expect(
        find.text(
          rotulo('receita.ficheiro', FicheiroRecusadoErro.formatoInvalido),
        ),
        findsOneWidget,
      );
    });

    testWidgets(
      'com DOCUMENTO_NAO_GUARDADO fica no ecrã com os dados e a receita (RF-053)',
      (tester) async {
        await abrir(tester, AppRouter.novoTratamento('f-1'));
        await preencher(tester);
        await tocar(tester, find.text('Carregar: Receita médica (opcional)'));
        await tocar(tester, find.text('Câmara'));
        app.tratamentos.erroGuardar = const ProblemaApi(
          codigo: 'DOCUMENTO_NAO_GUARDADO',
          mensagem: '',
          estadoHttp: 503,
        );

        await tocar(tester, registar());

        expect(
          find.text(FormularioTratamentoController.receitaNaoGuardadaRegisto),
          findsOneWidget,
        );
        expect(app.local, AppRouter.novoTratamento('f-1'));
        expect(find.text('Metformina'), findsOneWidget);
        expect(find.text('20:00'), findsOneWidget);
        expect(find.text('receita.pdf'), findsOneWidget);
      },
    );

    testWidgets(
      'os erros de validação do servidor aparecem junto dos campos (RNF-028)',
      (tester) async {
        await abrir(tester, AppRouter.novoTratamento('f-1'));
        await preencher(tester);
        app.tratamentos.erroGuardar = const ProblemaApi(
          codigo: 'VALIDACAO_INVALIDA',
          mensagem: 'Dados inválidos.',
          estadoHttp: 400,
          campos: [
            ErroCampo(campo: 'dose', regra: 'Tem no máximo 60 caracteres.'),
            ErroCampo(
              campo: 'horarios[1]',
              regra: 'Tem de ser uma hora no formato HH:mm, por exemplo 08:00.',
            ),
          ],
        );

        await tocar(tester, registar());

        expect(
          find.text(rotulo('dose', 'Tem no máximo 60 caracteres.')),
          findsOneWidget,
        );
        expect(
          find.text(
            rotulo(
              'horarios',
              'Tem de ser uma hora no formato HH:mm, por exemplo 08:00.',
            ),
          ),
          findsOneWidget,
        );
        expect(find.text('Metformina'), findsOneWidget);
      },
    );

    testWidgets('o Familiar não regista (RF-048)', (tester) async {
      app = AppFichas(inicio: AppRouter.novoTratamento('f-1'));
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.text(MensagensErro.catalogo['ACESSO_PROIBIDO']!),
        findsOneWidget,
      );
      expect(campo('Medicamento'), findsNothing);
    });

    testWidgets('numa ficha arquivada não se regista (RF-120)', (tester) async {
      app = AppFichas(inicio: AppRouter.novoTratamento('f-1'));
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
        findsOneWidget,
      );
      expect(campo('Medicamento'), findsNothing);
    });
  });

  group('E16 Editar tratamento', () {
    testWidgets(
      'envia só os campos alterados, sem a data de início (RF-055, OP-30)',
      (tester) async {
        await abrir(tester, AppRouter.editarTratamento('f-1', 't-1'));

        final inicio = tester.widget<TextField>(campo('Data de início'));
        expect(inicio.enabled, isFalse);
        expect(find.text('07/10/2026'), findsOneWidget);
        expect(find.text('Metformina'), findsOneWidget);

        await escrever(tester, 'Dose', '850 mg');
        await tocar(tester, find.text('Grave'));
        await tocar(tester, find.widgetWithText(FilledButton, 'Guardar'));

        expect(app.tratamentos.ultimosDados!.toJson(), {
          'dose': '850 mg',
          'grauImportancia': 'Grave',
        });
        expect(app.local, AppRouter.tratamento('f-1', 't-1'));
        expect(find.text('Metformina 850 mg'), findsOneWidget);
        expect(find.text('Alterações guardadas.'), findsOneWidget);
        expect(
          app.tratamentos.tratamentos['t-1']!.grauImportancia,
          GrauImportancia.grave,
        );
      },
    );

    testWidgets('não deixa retirar a data de fim', (tester) async {
      await abrir(tester, AppRouter.editarTratamento('f-1', 't-1'));

      await escrever(tester, 'Data de fim', '');
      await tocar(tester, find.widgetWithText(FilledButton, 'Guardar'));

      expect(
        find.text(
          rotulo('dataFim', ValidadorTratamento.regraDataFimNaoRetirada),
        ),
        findsOneWidget,
      );
      expect(app.tratamentos.atualizacoes, 0);
    });

    testWidgets('um tratamento terminado não se edita (RF-055)', (
      tester,
    ) async {
      app = AppFichas(inicio: AppRouter.editarTratamento('f-1', 't-1'));
      app.fichas.juntar(fichaExemplo());
      app.tratamentos.juntar(
        tratamentoExemplo(estado: EstadoTratamento.terminado),
      );
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text(ApresentacaoTratamento.naoEditavel), findsOneWidget);
      expect(campo('Dose'), findsNothing);
    });

    testWidgets(
      'TRATAMENTO_TERMINADO do servidor mostra a mensagem e mantém os dados',
      (tester) async {
        await abrir(tester, AppRouter.editarTratamento('f-1', 't-1'));
        app.tratamentos.erroGuardar = const ProblemaApi(
          codigo: 'TRATAMENTO_TERMINADO',
          mensagem: '',
          estadoHttp: 409,
        );

        await escrever(tester, 'Dose', '850 mg');
        await tocar(tester, find.widgetWithText(FilledButton, 'Guardar'));

        expect(find.text(ApresentacaoTratamento.naoEditavel), findsOneWidget);
        expect(find.text('850 mg'), findsOneWidget);
      },
    );
  });
}
