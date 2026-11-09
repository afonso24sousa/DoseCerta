import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

void main() {
  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.ficha('f-1'));
  });

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  group('E08 Ficha', () {
    testWidgets(
      'o Cuidador principal vê os dados, as necessidades e as ações (RF-028, RF-029)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);

        expect(find.text('Maria Fictícia Santos'), findsOneWidget);
        expect(find.text('Ativa'), findsOneWidget);
        expect(find.text('Nascimento: 17/05/1941'), findsOneWidget);
        expect(
          find.text('Contacto de emergência: +351 910 000 001'),
          findsOneWidget,
        );
        expect(
          find.text('Autonomia/mobilidade: Autonomia parcial'),
          findsOneWidget,
        );
        expect(find.text('Condições de saúde: Diabetes'), findsOneWidget);
        expect(find.text('Alergias: Medicamentos'), findsOneWidget);
        expect(find.text('O meu papel: Cuidador principal'), findsOneWidget);
        expect(find.text('Arquivar ou eliminar'), findsOneWidget);

        await tocar(tester, find.text('Editar'));
        expect(app.local, AppRouter.editarFicha('f-1'));
      },
    );

    testWidgets(
      'o Familiar só consulta: sem Editar nem gestão do estado (RF-048)',
      (tester) async {
        app.fichas.juntar(
          fichaExemplo(papel: Papel.familiar, principal: false),
        );
        await abrir(tester);

        expect(find.text('O meu papel: Familiar'), findsOneWidget);
        expect(find.text('Editar'), findsNothing);
        expect(find.text('Arquivar ou eliminar'), findsNothing);
        expect(find.text('Documentos'), findsOneWidget);
      },
    );

    testWidgets(
      'ficha arquivada: aviso de só consulta, sem Editar, com Reativar ou eliminar (RF-120)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
        await abrir(tester);

        expect(
          find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
          findsOneWidget,
        );
        expect(find.text('Arquivada'), findsOneWidget);
        expect(find.text('Editar'), findsNothing);
        await tocar(tester, find.text('Reativar ou eliminar'));
        expect(app.local, AppRouter.estadoFicha('f-1'));
      },
    );

    testWidgets('ficha própria indicada como tal (F003)', (tester) async {
      app.fichas.juntar(fichaExemplo(propria: true));
      await abrir(tester);
      expect(find.text('Tipo de ficha: Ficha própria'), findsOneWidget);
    });

    testWidgets('sem associação (404): mensagem e regresso à lista (RNF-005)', (
      tester,
    ) async {
      app.fichas.erroObter = const ProblemaApi(
        codigo: 'FICHA_NAO_ENCONTRADA',
        mensagem: 'x',
        estadoHttp: 404,
      );
      await abrir(tester);

      expect(
        find.text(MensagensErro.catalogo['FICHA_NAO_ENCONTRADA']!),
        findsOneWidget,
      );
      await tocar(tester, find.text('Voltar às fichas'));
      expect(app.local, AppRouter.fichas);
    });

    testWidgets('sem ligação: tentar de novo', (tester) async {
      app.fichas
        ..juntar(fichaExemplo())
        ..erroObter = const SemLigacaoErro();
      await abrir(tester);
      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);

      app.fichas.erroObter = null;
      await tocar(tester, find.text('Tentar novamente'));
      expect(find.text('Nascimento: 17/05/1941'), findsOneWidget);
    });

    testWidgets('o menu da ficha abre os documentos e as pessoas', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      await abrir(tester);

      await tocar(tester, find.widgetWithText(ChoiceChip, 'Documentos'));
      expect(app.local, AppRouter.documentosFicha('f-1'));

      app.router.go(AppRouter.ficha('f-1'));
      await tester.pumpAndSettle();
      await tocar(tester, find.widgetWithText(ChoiceChip, 'Pessoas'));
      expect(app.local, AppRouter.pessoasFicha('f-1'));
      expect(find.text('Página provisória'), findsOneWidget);
    });

    testWidgets(
      'alvos de toque de 48 x 48 dp e letra a 150% (RNF-025, RNF-026)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        final semantica = tester.ensureSemantics();
        await abrir(tester);
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();

        await tester.pumpWidget(app.widget(escalaLetra: 1.5));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
      },
    );
  });
}
