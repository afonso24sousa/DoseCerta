import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/pessoas/pessoas_ficha_controller.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_pessoas.dart';

/// Abre o menu da pessoa [nome], escolhe a [acao] e confirma na folha inferior.
Future<void> escolherEConfirmar(
  WidgetTester tester,
  String nome,
  AcaoPessoa acao,
) async {
  await tester.tap(find.byTooltip('Ações para $nome'));
  await tester.pumpAndSettle();
  await tester.tap(find.text(acao.rotulo));
  await tester.pumpAndSettle();
  await tester.tap(find.widgetWithText(FilledButton, acao.botaoConfirmar));
  await tester.pumpAndSettle();
}

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.pessoasFicha('f-1'));
    app.fichas.juntar(fichaExemplo());
    app.pessoas.pessoas.addAll([
      pessoaExemplo(
        id: AppFichas.utilizadorId,
        nome: 'Eu Fictício',
        principal: true,
      ),
      pessoaExemplo(id: 'u-2', nome: 'Rui Fictício'),
      pessoaExemplo(id: 'u-3', nome: 'Sofia Fictícia', papel: Papel.familiar),
    ]);
  });

  group('AcaoPessoa.disponiveis', () {
    test('Familiar: promover e remover (RF-049, RF-131)', () {
      expect(
        AcaoPessoa.disponiveis(
          pessoaExemplo(papel: Papel.familiar),
          fichaPropria: false,
        ),
        [AcaoPessoa.promover, AcaoPessoa.remover],
      );
    });

    test(
      'Cuidador: despromover, remover e tornar principal (RF-130 a RF-132)',
      () {
        expect(AcaoPessoa.disponiveis(pessoaExemplo(), fichaPropria: false), [
          AcaoPessoa.despromover,
          AcaoPessoa.remover,
          AcaoPessoa.tornarPrincipal,
        ]);
      },
    );

    test(
      'Cuidador principal de ficha de terceiro: o servidor decide (RF-133)',
      () {
        expect(
          AcaoPessoa.disponiveis(
            pessoaExemplo(principal: true),
            fichaPropria: false,
          ),
          [AcaoPessoa.despromover, AcaoPessoa.remover],
        );
      },
    );

    test('ficha própria: o titular não tem ações e ninguém passa a principal (RF-033, RF-034)', () {
      expect(
        AcaoPessoa.disponiveis(
          pessoaExemplo(principal: true),
          fichaPropria: true,
        ),
        isEmpty,
      );
      expect(AcaoPessoa.disponiveis(pessoaExemplo(), fichaPropria: true), [
        AcaoPessoa.despromover,
        AcaoPessoa.remover,
      ]);
    });
  });

  group('E12 Pessoas da ficha', () {
    testWidgets('mostra as pessoas com o papel e o Cuidador principal', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Eu Fictício'), findsOneWidget);
      expect(find.text('Cuidador principal'), findsOneWidget);
      expect(find.text('Cuidador'), findsOneWidget);
      expect(find.text('Familiar'), findsOneWidget);
      expect(app.pessoas.listagens, 1);
    });

    testWidgets('o Cuidador principal provisório é assinalado (RF-025)', (
      tester,
    ) async {
      app.pessoas.pessoas[0] = pessoaExemplo(
        id: AppFichas.utilizadorId,
        nome: 'Eu Fictício',
        principal: true,
        provisorio: true,
      );
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Cuidador principal (provisório)'), findsOneWidget);
      expect(
        find.textContaining('convite para Cuidador principal pendente'),
        findsOneWidget,
      );
    });

    testWidgets('o menu do Familiar tem promover e remover', (tester) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Ações para Sofia Fictícia'));
      await tester.pumpAndSettle();

      expect(find.text('Promover a Cuidador'), findsOneWidget);
      expect(find.text('Remover'), findsOneWidget);
      expect(find.text('Despromover a Familiar'), findsNothing);
      expect(find.text('Tornar principal'), findsNothing);
    });

    testWidgets('promover com confirmação atualiza a lista (RF-049)', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await escolherEConfirmar(tester, 'Sofia Fictícia', AcaoPessoa.promover);

      expect(app.pessoas.operacoes, ['promover u-3']);
      expect(app.pessoas.listagens, 2);
      expect(find.text('Familiar'), findsNothing);
      expect(find.text('Cuidador'), findsNWidgets(2));
    });

    testWidgets('tornar principal transfere o papel (RF-132)', (tester) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await escolherEConfirmar(
        tester,
        'Rui Fictício',
        AcaoPessoa.tornarPrincipal,
      );

      expect(app.pessoas.operacoes, ['transferir u-2']);
      final linhaRui = find.ancestor(
        of: find.text('Rui Fictício'),
        matching: find.byType(ListTile),
      );
      expect(
        find.descendant(
          of: linhaRui,
          matching: find.text('Cuidador principal'),
        ),
        findsOneWidget,
      );
    });

    testWidgets('cancelar a confirmação não faz o pedido', (tester) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Ações para Rui Fictício'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Remover'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(app.pessoas.operacoes, isEmpty);
      expect(find.text('Rui Fictício'), findsOneWidget);
    });

    testWidgets(
      'recusa do servidor: mostra a explicação e a lista fica como estava (RF-133)',
      (tester) async {
        app.pessoas.erroAcao = const ProblemaApi(
          codigo: 'CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO',
          mensagem: 'Sem substituto.',
          estadoHttp: 409,
        );
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        await escolherEConfirmar(tester, 'Eu Fictício', AcaoPessoa.despromover);

        expect(
          find.text(
            'Indique outro Cuidador para assumir o papel antes de despromover '
            'ou remover o Cuidador principal.',
          ),
          findsOneWidget,
        );
        expect(find.text('Cuidador principal'), findsOneWidget);
        expect(app.pessoas.listagens, 1);
      },
    );

    testWidgets('a ficha sem Cuidador é recusada com a mensagem (RF-134)', (
      tester,
    ) async {
      app.pessoas.erroAcao = const ProblemaApi(
        codigo: 'FICHA_SEM_CUIDADOR',
        mensagem: 'Sem Cuidador.',
        estadoHttp: 409,
      );
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await escolherEConfirmar(tester, 'Rui Fictício', AcaoPessoa.remover);

      expect(
        find.text('A ficha tem de ficar com pelo menos um Cuidador.'),
        findsOneWidget,
      );
      expect(find.text('Rui Fictício'), findsOneWidget);
    });

    testWidgets('sem ligação: mostra o aviso e mantém a lista', (tester) async {
      app.pessoas.erroAcao = const SemLigacaoErro();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await escolherEConfirmar(tester, 'Rui Fictício', AcaoPessoa.remover);

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(find.text('Rui Fictício'), findsOneWidget);
    });

    testWidgets('o Familiar só consulta: não vê ações (RF-048)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Rui Fictício'), findsOneWidget);
      expect(find.byType(PopupMenuButton<AcaoPessoa>), findsNothing);
    });

    testWidgets('ficha arquivada: só consulta (RF-120)', (tester) async {
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Ficha arquivada: só consulta.'),
        findsOneWidget,
      );
      expect(find.byType(PopupMenuButton<AcaoPessoa>), findsNothing);
    });

    testWidgets(
      'ficha própria: o titular não tem menu e ninguém passa a principal (RF-033, RF-034)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(propria: true));
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.byTooltip('Ações para Eu Fictício'), findsNothing);

        await tester.tap(find.byTooltip('Ações para Rui Fictício'));
        await tester.pumpAndSettle();
        expect(find.text('Tornar principal'), findsNothing);
        expect(find.text('Despromover a Familiar'), findsOneWidget);
      },
    );

    testWidgets('remover o próprio acesso volta à lista de fichas (RF-131)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(principal: false));
      app.pessoas.pessoas
        ..[0] = pessoaExemplo(id: AppFichas.utilizadorId, nome: 'Eu Fictício')
        ..[1] = pessoaExemplo(id: 'u-2', nome: 'Rui Fictício', principal: true);
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      final listagensFichas = app.fichas.listagens;

      await escolherEConfirmar(tester, 'Eu Fictício', AcaoPessoa.remover);

      expect(app.pessoas.operacoes, ['remover ${AppFichas.utilizadorId}']);
      expect(app.local, AppRouter.fichas);
      expect(app.fichas.listagens, greaterThan(listagensFichas));
      expect(find.text('Deixou de ter acesso à ficha.'), findsOneWidget);
    });

    testWidgets('alvos de toque de pelo menos 48 x 48 dp (RNF-025)', (
      tester,
    ) async {
      final semantica = tester.ensureSemantics();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });

    testWidgets('letra a 150% sem erros de layout (RNF-026)', (tester) async {
      await tester.pumpWidget(app.widget(escalaLetra: 1.5));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.byType(ListTile), findsNWidgets(3));
    });
  });
}
