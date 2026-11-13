import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/features/fichas/estado_ficha_page.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.ficha('f-1'));
  });

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  Future<void> abrirE11(WidgetTester tester, String botao) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
    await tocar(tester, find.text(botao));
    expect(app.local, AppRouter.estadoFicha('f-1'));
  }

  Future<void> confirmar(WidgetTester tester, String acao) async {
    await tocar(tester, find.widgetWithText(FilledButton, acao).last);
  }

  group('E11 Arquivar, reativar ou eliminar ficha', () {
    testWidgets(
      'arquivar com confirmação: a ficha fica só de consulta (RF-120)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrirE11(tester, 'Arquivar ou eliminar');

        expect(find.text(EstadoFichaPage.textoArquivo), findsOneWidget);
        expect(find.text('Reativar'), findsNothing);

        await tocar(tester, find.widgetWithText(FilledButton, 'Arquivar'));
        await tocar(tester, find.text('Cancelar'));
        expect(app.fichas.operacoes, isEmpty);

        await tocar(tester, find.widgetWithText(FilledButton, 'Arquivar'));
        await confirmar(tester, 'Arquivar');

        expect(app.fichas.operacoes, ['arquivar f-1']);
        expect(app.local, AppRouter.ficha('f-1'));
        expect(find.text('Ficha arquivada.'), findsOneWidget);
        expect(
          find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
          findsOneWidget,
        );
        expect(find.text('Editar'), findsNothing);
      },
    );

    testWidgets('reativar uma ficha arquivada (RF-121)', (tester) async {
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await abrirE11(tester, 'Reativar ou eliminar');

      expect(find.widgetWithText(FilledButton, 'Arquivar'), findsNothing);
      await tocar(tester, find.widgetWithText(FilledButton, 'Reativar'));
      await confirmar(tester, 'Reativar');

      expect(app.fichas.operacoes, ['reativar f-1']);
      expect(app.local, AppRouter.ficha('f-1'));
      expect(find.text('Editar'), findsOneWidget);
    });

    testWidgets('eliminar uma ficha sem registos leva à lista (RF-123)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      await abrirE11(tester, 'Arquivar ou eliminar');

      await tocar(tester, find.widgetWithText(OutlinedButton, 'Eliminar'));
      await confirmar(tester, 'Eliminar');

      expect(app.fichas.operacoes, ['eliminar f-1']);
      expect(app.local, AppRouter.fichas);
      expect(find.text('Ficha eliminada.'), findsOneWidget);
      expect(find.text('Maria Fictícia Santos'), findsNothing);
    });

    testWidgets(
      'eliminar uma ficha com histórico é recusado e propõe arquivar (RF-123)',
      (tester) async {
        app.fichas
          ..juntar(fichaExemplo())
          ..erroEstado = const ProblemaApi(
            codigo: 'FICHA_COM_HISTORICO',
            mensagem: 'x',
            estadoHttp: 409,
          );
        await abrirE11(tester, 'Arquivar ou eliminar');

        await tocar(tester, find.widgetWithText(OutlinedButton, 'Eliminar'));
        await confirmar(tester, 'Eliminar');

        expect(
          find.text(
            'A ficha tem medicação ou histórico e não pode ser eliminada. Arquive-a.',
          ),
          findsOneWidget,
        );
        expect(app.local, AppRouter.estadoFicha('f-1'));
      },
    );

    testWidgets('o Familiar não gere o estado da ficha (RF-120 CA2)', (
      tester,
    ) async {
      app = AppFichas(inicio: AppRouter.estadoFicha('f-1'));
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.text(MensagensErro.catalogo['ACESSO_PROIBIDO']!),
        findsOneWidget,
      );
      expect(find.text('Eliminar'), findsNothing);
    });

    testWidgets(
      'alvos de toque de 48 x 48 dp e letra a 150% (RNF-025, RNF-026)',
      (tester) async {
        app = AppFichas(inicio: AppRouter.estadoFicha('f-1'));
        app.fichas.juntar(fichaExemplo());
        final semantica = tester.ensureSemantics();
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();

        await tester.pumpWidget(app.widget(escalaLetra: 1.5));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
      },
    );
  });
}
