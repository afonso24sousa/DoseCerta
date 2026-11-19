// Testes de sistema da F002 e da F003 na aplicação mobile (I070, QG-07), contra a API real indicada em
// `--dart-define=URL_API`. Especificação: m4-especificacao-testes-sistema-frontend-mobile-v02.md.
import 'dart:convert';
import 'dart:typed_data';

import 'package:dosecerta/features/fichas/estado_ficha_page.dart';
import 'package:dosecerta/shared/origem_ficheiro.dart';
import 'package:dosecerta/shared/validador_ficha.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

import 'apoio.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> preencherIdentificacao(WidgetTester tester, String nome) async {
    await tester.enterText(campo('Nome completo'), nome);
    await tester.enterText(campo('Data de nascimento'), '17/05/1941');
    await tester.enterText(campo('Contacto de emergência'), '+351 910 000 010');
  }

  Future<void> abrirNovaFicha(WidgetTester tester) async {
    await tocar(tester, find.byTooltip('Fichas'));
    await esperarPor(tester, find.text('Nova ficha'));
    await tocar(tester, find.text('Nova ficha'));
    await esperarPor(tester, find.text('Seguinte'));
  }

  testWidgets(
    'TS-M-F002-01 cria uma ficha de terceiro com uma receita, sendo o Cuidador principal (RF-020 a RF-022, RF-024)',
    (tester) async {
      final conta = await contaNaApi('m-f002-criar');
      final fonte = FonteFicticia()
        ..ficheiros.add((bytes: pdfFicticio(), nome: 'receita-ficticia.pdf'));
      await arrancar(tester, fonte);
      await iniciarSessao(tester, conta);
      await abrirNovaFicha(tester);

      await tocar(tester, find.text('Ficha de uma terceira pessoa'));
      await preencherIdentificacao(tester, 'Maria Fictícia Santos');
      await tocar(tester, find.text('Seguinte'));
      await esperarPor(tester, find.text('Eu sou o Cuidador principal'));
      await tocar(tester, find.text('Eu sou o Cuidador principal'));
      await tocar(tester, find.text('Carregar: Receita médica'));
      await esperarPor(tester, find.text('Galeria'));
      await tocar(tester, find.text('Galeria'));
      await esperarPor(
        tester,
        find.textContaining('resultado-esperado-errado.pdf'),
      );
      await tocar(tester, find.text('Criar ficha'));

      // O título «Ficha» aparece antes de E08 carregar os dados: espera-se pelo nome da ficha criada.
      await esperarPor(tester, find.text('Maria Fictícia Santos'));
      expect(find.text('Ficha'), findsWidgets);
      expect(fonte.pedidas, [OrigemFicheiro.galeria]);
    },
  );

  testWidgets(
    'TS-M-F002-02 cria uma ficha com convite para Cuidador principal, ficando o criador provisório (RF-025)',
    (tester) async {
      final conta = await contaNaApi('m-f002-convite');
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, conta);
      await abrirNovaFicha(tester);

      await tocar(tester, find.text('Ficha de uma terceira pessoa'));
      await preencherIdentificacao(tester, 'João Fictício Lopes');
      await tocar(tester, find.text('Seguinte'));
      await esperarPor(tester, find.textContaining('Convidar outra pessoa'));
      await tocar(tester, find.textContaining('Convidar outra pessoa'));
      await esperarPor(tester, campo('E-mail da pessoa a convidar'));
      await tester.enterText(
        campo('E-mail da pessoa a convidar'),
        'principal.${DateTime.now().microsecondsSinceEpoch}@exemplo.pt',
      );
      await tocar(tester, find.text('Criar ficha'));
      await esperarPor(tester, find.text('Ficha'));

      final fichas = await pedido('GET', '/fichas', token: conta.token);
      final fichaId =
          (fichas.lista.single! as Map<String, Object?>)['id']! as String;
      final pessoas = await pedido(
        'GET',
        '/fichas/$fichaId/pessoas',
        token: conta.token,
      );
      final criador = pessoas.lista.cast<Map<String, Object?>>().single;
      expect(criador['principal'], isTrue);
      expect(criador['provisorio'], isTrue);
    },
  );

  testWidgets(
    'TS-M-F002-03 recusa nome em falta e data futura junto a cada campo, mantendo os dados (RF-021, RNF-028)',
    (tester) async {
      final conta = await contaNaApi('m-f002-validar');
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, conta);
      await abrirNovaFicha(tester);

      await tocar(tester, find.text('Ficha de uma terceira pessoa'));
      await tester.enterText(campo('Data de nascimento'), '01/01/2099');
      await tester.enterText(
        campo('Contacto de emergência'),
        '+351 910 000 011',
      );
      await tocar(tester, find.text('Seguinte'));

      await esperarPor(tester, find.text('Nome completo: é obrigatório.'));
      expect(find.text(ValidadorFicha.erroData), findsOneWidget);
      expect(find.text('01/01/2099'), findsOneWidget);
      expect(find.text('+351 910 000 011'), findsOneWidget);
      expect(find.text('Eu sou o Cuidador principal'), findsNothing);
    },
  );

  testWidgets('TS-M-F002-04 edita o nome da ficha (RF-029)', (tester) async {
    final conta = await contaNaApi('m-f002-editar');
    await fichaNaApi(conta, 'Rosa Fictícia');
    await arrancar(tester, FonteFicticia());
    await iniciarSessao(tester, conta);
    await abrirFicha(tester, 'Rosa Fictícia');

    await tocar(tester, find.text('Editar'));
    await esperarPor(tester, campo('Nome completo'));
    await tester.enterText(campo('Nome completo'), 'Rosa Fictícia Pereira');
    await tocar(tester, find.text('Guardar'));

    await esperarPor(tester, find.text('Rosa Fictícia Pereira'));
    expect(find.text('Ficha'), findsOneWidget);
  });

  testWidgets(
    'TS-M-F002-05 em E10 recusa um .txt antes de enviar e carrega um PDF (RF-022, RNF-008)',
    (tester) async {
      final conta = await contaNaApi('m-f002-docs');
      await fichaNaApi(conta, 'Carlos Fictício');
      final fonte = FonteFicticia()
        ..ficheiros.add((
          bytes: Uint8List.fromList(utf8.encode('Texto fictício.')),
          nome: 'notas-ficticias.txt',
        ))
        ..ficheiros.add((bytes: pdfFicticio(), nome: 'relatorio-ficticio.pdf'));
      await arrancar(tester, fonte);
      await iniciarSessao(tester, conta);
      await abrirFicha(tester, 'Carlos Fictício');
      await tocar(tester, find.widgetWithText(ChoiceChip, 'Documentos'));
      await esperarPor(tester, find.text('Carregar: Documento a acrescentar'));

      await tocar(tester, find.text('Carregar: Documento a acrescentar'));
      await esperarPor(tester, find.text('Relatório clínico'));
      await tocar(tester, find.text('Relatório clínico'));
      await esperarPor(tester, find.text('Ficheiro'));
      await tocar(tester, find.text('Ficheiro'));
      await esperarPor(
        tester,
        find.textContaining('O formato do ficheiro não é suportado'),
      );
      expect(find.textContaining('notas-ficticias.txt'), findsNothing);

      await tocar(tester, find.text('Carregar: Documento a acrescentar'));
      await esperarPor(tester, find.text('Relatório clínico'));
      await tocar(tester, find.text('Relatório clínico'));
      await esperarPor(tester, find.text('Ficheiro'));
      await tocar(tester, find.text('Ficheiro'));
      await esperarPor(tester, find.textContaining('relatorio-ficticio.pdf'));
    },
  );

  testWidgets('TS-M-F002-06 arquiva e reativa a ficha (RF-120, RF-121)', (
    tester,
  ) async {
    final conta = await contaNaApi('m-f002-arquivar');
    await fichaNaApi(conta, 'Lúcia Fictícia');
    await arrancar(tester, FonteFicticia());
    await iniciarSessao(tester, conta);
    await abrirFicha(tester, 'Lúcia Fictícia');

    await tocar(tester, find.text('Arquivar ou eliminar'));
    await esperarPor(tester, find.text(EstadoFichaPage.textoArquivo));
    await tocar(tester, find.widgetWithText(FilledButton, 'Arquivar'));
    // A folha de confirmação acrescenta um segundo «Arquivar».
    await esperarPor(
      tester,
      find.widgetWithText(FilledButton, 'Arquivar').at(1),
    );
    await tocar(tester, find.widgetWithText(FilledButton, 'Arquivar').last);
    await esperarPor(tester, find.text('Ficha arquivada.'));
    expect(find.text('Editar'), findsNothing);

    await tocar(tester, find.text('Reativar ou eliminar'));
    await esperarPor(tester, find.text(EstadoFichaPage.textoReativacao));
    await tocar(tester, find.widgetWithText(FilledButton, 'Reativar'));
    // A folha de confirmação acrescenta um segundo «Reativar».
    await esperarPor(
      tester,
      find.widgetWithText(FilledButton, 'Reativar').at(1),
    );
    await tocar(tester, find.widgetWithText(FilledButton, 'Reativar').last);
    await esperarPor(tester, find.text('Editar'));
  });

  testWidgets(
    'TS-M-F002-07 recusa eliminar uma ficha com medicação e propõe arquivar (RF-123)',
    (tester) async {
      final conta = await contaNaApi('m-f002-historico');
      final fichaId = await fichaNaApi(conta, 'Teresa Fictícia');
      await tratamentoNaApi(conta, fichaId);
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, conta);
      await abrirFicha(tester, 'Teresa Fictícia');

      await tocar(tester, find.text('Arquivar ou eliminar'));
      await esperarPor(tester, find.widgetWithText(OutlinedButton, 'Eliminar'));
      await tocar(tester, find.widgetWithText(OutlinedButton, 'Eliminar'));
      await esperarPor(tester, find.widgetWithText(FilledButton, 'Eliminar'));
      await tocar(tester, find.widgetWithText(FilledButton, 'Eliminar').last);

      await esperarPor(
        tester,
        find.text(
          'A ficha tem medicação ou histórico e não pode ser eliminada. Arquive-a.',
        ),
      );
      expect(
        (await pedido('GET', '/fichas/$fichaId', token: conta.token)).estado,
        200,
      );
    },
  );

  testWidgets(
    'TS-M-F002-08 o Familiar consulta a ficha sem Editar nem gerir o estado (RF-028, RF-048)',
    (tester) async {
      final cuidador = await contaNaApi('m-f002-cuidador');
      final familiar = await contaNaApi(
        'm-f002-familiar',
        nome: 'Filipe Fictício',
      );
      final fichaId = await fichaNaApi(cuidador, 'Helena Fictícia');
      await associarNaApi(cuidador, fichaId, familiar, 'Familiar');
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, familiar);
      await abrirFicha(tester, 'Helena Fictícia');

      expect(find.text('Editar'), findsNothing);
      expect(find.text('Arquivar ou eliminar'), findsNothing);
      await tocar(tester, find.widgetWithText(ChoiceChip, 'Documentos'));
      await esperarPor(
        tester,
        find.text('Esta ficha ainda não tem documentos.'),
      );
      expect(find.text('Carregar: Documento a acrescentar'), findsNothing);
    },
  );

  testWidgets(
    'TS-M-F003-01 cria a ficha própria, sem o passo do Cuidador principal (RF-030)',
    (tester) async {
      final conta = await contaNaApi('m-f003-criar');
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, conta);
      await abrirNovaFicha(tester);

      await tocar(tester, find.text('Gerir a minha própria saúde'));
      await preencherIdentificacao(tester, 'Ana Fictícia');
      await tocar(tester, find.text('Seguinte'));
      await esperarPor(tester, find.text('Criar ficha'));
      expect(find.text('Eu sou o Cuidador principal'), findsNothing);
      await tocar(tester, find.text('Criar ficha'));
      await esperarPor(tester, find.text('Ficha'));

      final fichas = await pedido('GET', '/fichas', token: conta.token);
      final ficha = fichas.lista.cast<Map<String, Object?>>().single;
      expect(ficha['eProprio'], isTrue);
      expect(ficha['souPrincipal'], isTrue);
    },
  );

  testWidgets(
    'TS-M-F003-02 quem já tem ficha própria não a pode criar de novo (RF-031)',
    (tester) async {
      final conta = await contaNaApi('m-f003-segunda');
      await fichaNaApi(conta, 'Ana Fictícia', propria: true);
      await arrancar(tester, FonteFicticia());
      await iniciarSessao(tester, conta);
      await abrirNovaFicha(tester);

      await esperarPor(
        tester,
        find.text('Já tem uma ficha própria. Só pode ter uma.'),
      );
    },
  );
}
