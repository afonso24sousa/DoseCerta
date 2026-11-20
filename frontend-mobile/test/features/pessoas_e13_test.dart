import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/pessoas/convidar_pessoa_controller.dart';
import 'package:dosecerta/features/pessoas/convidar_pessoa_page.dart';
import 'package:dosecerta/features/pessoas/pessoas_ficha_page.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_pessoas.dart';

const String rotuloPrincipal = 'Oferecer o papel de Cuidador principal';

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.pessoasFicha('f-1'));
    app.fichas.juntar(fichaExemplo());
    app.pessoas.pessoas.add(
      pessoaExemplo(
        id: AppFichas.utilizadorId,
        nome: 'Eu Fictício',
        principal: true,
      ),
    );
  });

  /// Abre E13 a partir de E12, como o Utilizador.
  Future<void> abrirE13(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
    await tester.tap(find.text('Convidar pessoa'));
    await tester.pumpAndSettle();
    expect(find.byType(ConvidarPessoaPage), findsOneWidget);
  }

  Future<void> preencher(
    WidgetTester tester, {
    String email = 'rui@exemplo.pt',
    String? papel,
  }) async {
    await tester.enterText(find.byType(TextField), email);
    if (papel != null) {
      await tester.tap(find.text(papel));
    }
    await tester.pumpAndSettle();
  }

  Future<void> enviar(WidgetTester tester) async {
    await tester.ensureVisible(find.text('Enviar convite'));
    await tester.tap(find.text('Enviar convite'));
    await tester.pumpAndSettle();
  }

  group('ConvidarPessoaController.validar', () {
    test('exige o e-mail e o papel (RF-040, RNF-028)', () {
      final erros = ConvidarPessoaController.validar(email: ' ', papel: null);
      expect(erros['email'], 'E-mail da pessoa: é obrigatório.');
      expect(erros['papel'], 'Papel: escolha Cuidador ou Familiar.');
    });

    test('recusa um e-mail sem o formato esperado', () {
      final erros = ConvidarPessoaController.validar(
        email: 'rui@exemplo',
        papel: Papel.familiar,
      );
      expect(
        erros['email'],
        'E-mail da pessoa: indique um e-mail válido, por exemplo rui@exemplo.pt.',
      );
      expect(erros.containsKey('papel'), isFalse);
    });

    test('aceita um e-mail válido e o papel', () {
      expect(
        ConvidarPessoaController.validar(
          email: ' rui@exemplo.pt ',
          papel: Papel.cuidador,
        ),
        isEmpty,
      );
    });
  });

  group('E13 Convidar pessoa', () {
    testWidgets('sem e-mail nem papel mostra os erros e não envia (RNF-028)', (
      tester,
    ) async {
      await abrirE13(tester);

      await enviar(tester);

      expect(find.text('E-mail da pessoa: é obrigatório.'), findsOneWidget);
      expect(find.text('Papel: escolha Cuidador ou Familiar.'), findsOneWidget);
      expect(app.convites.enviados, isEmpty);
    });

    testWidgets(
      'envia o convite de Familiar, volta a E12 e atualiza a lista (RF-040)',
      (tester) async {
        await abrirE13(tester);
        final listagens = app.pessoas.listagens;

        await preencher(tester, email: ' rui@exemplo.pt ', papel: 'Familiar');
        await enviar(tester);

        final (fichaId, dados) = app.convites.enviados.single;
        expect(fichaId, 'f-1');
        expect(dados.toJson(), {
          'email': 'rui@exemplo.pt',
          'papel': 'Familiar',
          'paraPrincipal': false,
        });
        expect(find.byType(ConvidarPessoaPage), findsNothing);
        expect(find.byType(PessoasFichaPage), findsOneWidget);
        expect(
          find.text('Convite enviado para rui@exemplo.pt.'),
          findsOneWidget,
        );
        expect(app.pessoas.listagens, listagens + 1);
      },
    );

    testWidgets(
      'oferecer o papel de Cuidador principal só com Cuidador (RF-025, RF-027)',
      (tester) async {
        await abrirE13(tester);
        expect(find.text(rotuloPrincipal), findsNothing);

        await preencher(tester, papel: 'Cuidador');
        expect(find.text(rotuloPrincipal), findsOneWidget);
        await tester.tap(find.text(rotuloPrincipal));
        await tester.pumpAndSettle();

        // Mudar para Familiar esconde a opção e desliga-a.
        await tester.tap(find.text('Familiar'));
        await tester.pumpAndSettle();
        expect(find.text(rotuloPrincipal), findsNothing);
        await tester.tap(find.text('Cuidador'));
        await tester.pumpAndSettle();
        await tester.tap(find.text(rotuloPrincipal));
        await tester.pumpAndSettle();

        await enviar(tester);

        expect(app.convites.enviados.single.$2.toJson(), {
          'email': 'rui@exemplo.pt',
          'papel': 'Cuidador',
          'paraPrincipal': true,
        });
      },
    );

    testWidgets(
      'na ficha própria não se oferece o papel de principal (RF-034)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(propria: true));
        await abrirE13(tester);

        await preencher(tester, papel: 'Cuidador');

        expect(find.text(rotuloPrincipal), findsNothing);
      },
    );

    testWidgets(
      'convite pendente duplicado: mensagem no e-mail e dados mantidos (RF-043)',
      (tester) async {
        app.convites.erroConvidar = const ProblemaApi(
          codigo: 'CONVITE_PENDENTE_EXISTE',
          mensagem: 'Convite pendente.',
          estadoHttp: 409,
        );
        await abrirE13(tester);

        await preencher(tester, papel: 'Familiar');
        await enviar(tester);

        expect(
          find.text(ConvidarPessoaController.convitePendenteDuplicado),
          findsOneWidget,
        );
        expect(find.byType(ConvidarPessoaPage), findsOneWidget);
        expect(find.text('rui@exemplo.pt'), findsOneWidget);
      },
    );

    testWidgets(
      'já há um convite para principal pendente: mensagem geral (OP-19)',
      (tester) async {
        app.convites.erroConvidar = const ProblemaApi(
          codigo: 'CONVITE_PENDENTE_EXISTE',
          mensagem: 'Convite pendente.',
          estadoHttp: 409,
        );
        await abrirE13(tester);

        await preencher(tester, papel: 'Cuidador');
        await tester.tap(find.text(rotuloPrincipal));
        await tester.pumpAndSettle();
        await enviar(tester);

        expect(
          find.text(ConvidarPessoaController.principalPendente),
          findsOneWidget,
        );
      },
    );

    testWidgets('pessoa já associada: mensagem no e-mail', (tester) async {
      app.convites.erroConvidar = const ProblemaApi(
        codigo: 'PESSOA_JA_ASSOCIADA',
        mensagem: 'Já associada.',
        estadoHttp: 409,
      );
      await abrirE13(tester);

      await preencher(tester, papel: 'Familiar');
      await enviar(tester);

      expect(
        find.text('Esta pessoa já está associada à ficha.'),
        findsOneWidget,
      );
    });

    testWidgets('erro de validação do servidor aparece no campo (RNF-028)', (
      tester,
    ) async {
      app.convites.erroConvidar = const ProblemaApi(
        codigo: 'VALIDACAO_INVALIDA',
        mensagem: 'Dados inválidos.',
        campos: [ErroCampo(campo: 'email', regra: 'Formato inválido.')],
        estadoHttp: 400,
      );
      await abrirE13(tester);

      await preencher(tester, papel: 'Familiar');
      await enviar(tester);

      expect(find.text('E-mail da pessoa: formato inválido.'), findsOneWidget);
    });

    testWidgets('sem ligação: mostra o aviso e fica no formulário', (
      tester,
    ) async {
      app.convites.erroConvidar = const SemLigacaoErro();
      await abrirE13(tester);

      await preencher(tester, papel: 'Familiar');
      await enviar(tester);

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(find.byType(ConvidarPessoaPage), findsOneWidget);
    });

    testWidgets('mostra a explicação do envio do e-mail (RF-135, RF-136)', (
      tester,
    ) async {
      await abrirE13(tester);
      expect(find.text(ConvidarPessoaPage.notaEnvio), findsOneWidget);
    });

    testWidgets('o Familiar não vê «Convidar pessoa» (RF-048)', (tester) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Convidar pessoa'), findsNothing);
    });

    testWidgets('a ficha arquivada não tem «Convidar pessoa» (RF-120)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Convidar pessoa'), findsNothing);
    });

    testWidgets('alvos de toque de pelo menos 48 x 48 dp (RNF-025)', (
      tester,
    ) async {
      final semantica = tester.ensureSemantics();
      await abrirE13(tester);
      await preencher(tester, papel: 'Cuidador');
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });

    testWidgets('letra a 150% sem erros de layout (RNF-026)', (tester) async {
      await tester.pumpWidget(app.widget(escalaLetra: 1.5));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Convidar pessoa'));
      await tester.pumpAndSettle();
      await preencher(tester, papel: 'Cuidador');
      expect(tester.takeException(), isNull);
      expect(find.text(rotuloPrincipal), findsOneWidget);
    });
  });
}
