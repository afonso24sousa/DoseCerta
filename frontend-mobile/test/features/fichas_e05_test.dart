import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas();
    app.fichas
      ..juntar(fichaExemplo())
      ..juntar(
        fichaExemplo(
          id: 'f-2',
          nome: 'João Fictício Lopes',
          papel: Papel.familiar,
          principal: false,
          propria: true,
        ),
      )
      ..juntar(fichaExemplo(id: 'f-3', nome: 'Ana Fictícia', propria: true))
      ..juntar(
        fichaExemplo(
          id: 'f-4',
          nome: 'Rui Arquivado',
          estado: EstadoFicha.arquivada,
        ),
      );
  });

  group('E05 Fichas', () {
    testWidgets('mostra as fichas ativas com o papel em cada uma (RF-028)', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Maria Fictícia Santos'), findsOneWidget);
      expect(find.text('Cuidador principal'), findsNWidgets(2));
      // Ficha própria de outra pessoa em que o Utilizador é Familiar: não é a «própria» dele (D5).
      expect(find.text('João Fictício Lopes'), findsOneWidget);
      expect(find.text('Familiar'), findsOneWidget);
      expect(find.text('Ana Fictícia (própria)'), findsOneWidget);
      expect(find.text('Rui Arquivado'), findsNothing);
      expect(app.fichas.listagens, 1);
    });

    testWidgets('o separador Arquivadas mostra só as arquivadas (RF-120)', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Arquivadas'));
      await tester.pumpAndSettle();

      expect(find.text('Rui Arquivado'), findsOneWidget);
      expect(find.text('Maria Fictícia Santos'), findsNothing);
      expect(app.fichas.listagens, 1);
    });

    testWidgets('sem ligação mostra o erro e permite tentar de novo', (
      tester,
    ) async {
      app.fichas.erroListar = const SemLigacaoErro();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);

      app.fichas.erroListar = null;
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();
      expect(find.text(SemLigacaoErro.mensagemPadrao), findsNothing);
      expect(find.text('Maria Fictícia Santos'), findsOneWidget);
    });

    testWidgets('abre a ficha e a nova ficha', (tester) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Maria Fictícia Santos'));
      await tester.pumpAndSettle();
      expect(app.local, AppRouter.ficha('f-1'));

      app.router.go(AppRouter.fichas);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Nova ficha'));
      await tester.pumpAndSettle();
      expect(app.local, AppRouter.novaFicha);
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
      expect(find.byType(ListTile), findsWidgets);
    });
  });
}
