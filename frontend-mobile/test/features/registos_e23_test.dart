import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/tipo_registo.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_registos.dart';

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.registosFicha('f-1'));
    app.registos.ocorrencias.addAll([
      registoExemplo(),
      registoExemplo(
        id: 'r-2',
        descricao: 'Recusou o almoço.',
        autor: 'Rui Fictício',
        ocorridaEm: '2026-10-06T17:30:00+01:00',
        criadaEm: '2026-10-06T18:10:00+01:00',
      ),
    ]);
    app.registos.notas.add(
      registoExemplo(
        id: 'r-3',
        tipo: TipoRegisto.nota,
        descricao: 'Reforçar a hidratação durante a tarde.',
      ),
    );
  });

  group('E23 Ocorrências e notas', () {
    testWidgets(
      'o Cuidador vê as ocorrências pela ordem do servidor, com quando aconteceu, o autor e o registo (RF-092, RF-094)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.text('Ocorrências'), findsOneWidget);
        expect(find.text('Notas'), findsOneWidget);
        expect(
          tester.getTopLeft(find.text('Queda ligeira sem lesões.')).dy,
          lessThan(tester.getTopLeft(find.text('Recusou o almoço.')).dy),
        );
        expect(find.text('Aconteceu em 07/10/2026 14:50'), findsOneWidget);
        expect(
          find.text('Registada em 07/10/2026 15:20 por Ana Fictícia'),
          findsOneWidget,
        );
        expect(
          find.text('Registada em 06/10/2026 18:10 por Rui Fictício'),
          findsOneWidget,
        );
        expect(
          find.text('Reforçar a hidratação durante a tarde.'),
          findsNothing,
        );
        expect(app.registos.consultas, ['Ocorrencia', 'Nota']);
      },
    );

    testWidgets(
      'no separador Notas o Cuidador vê as notas, sem data de ocorrência (RF-091, RF-092)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        await tester.tap(find.text('Notas'));
        await tester.pumpAndSettle();

        expect(
          find.text('Reforçar a hidratação durante a tarde.'),
          findsOneWidget,
        );
        expect(
          find.text('Registada em 07/10/2026 15:20 por Ana Fictícia'),
          findsOneWidget,
        );
        expect(find.textContaining('Aconteceu em'), findsNothing);
        expect(find.text('Queda ligeira sem lesões.'), findsNothing);
      },
    );

    testWidgets(
      'o Familiar só vê as ocorrências, não pede as notas e não tem opção de registo (RF-048, RF-095)',
      (tester) async {
        app.fichas.juntar(
          fichaExemplo(papel: Papel.familiar, principal: false),
        );
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.text('Queda ligeira sem lesões.'), findsOneWidget);
        expect(find.text('Notas'), findsNothing);
        expect(find.text('Novo registo'), findsNothing);
        expect(app.registos.consultas, ['Ocorrencia']);
      },
    );

    testWidgets('numa ficha arquivada só é possível consultar (RF-120)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Ficha arquivada: só consulta'),
        findsOneWidget,
      );
      expect(find.text('Queda ligeira sem lesões.'), findsOneWidget);
      expect(find.text('Notas'), findsOneWidget);
      expect(find.text('Novo registo'), findsNothing);
    });

    testWidgets(
      'não há opção para alterar nem eliminar registos (RF-090, RF-091)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.byType(PopupMenuButton<Object?>), findsNothing);
        expect(find.byIcon(Icons.edit_outlined), findsNothing);
        expect(find.byIcon(Icons.delete_outline), findsNothing);
        expect(find.textContaining('Editar'), findsNothing);
        expect(find.textContaining('Eliminar'), findsNothing);
      },
    );

    testWidgets(
      'ao puxar para baixo aparece o registo feito entretanto por outro Cuidador (RF-093)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();
        app.registos.ocorrencias.insert(
          0,
          registoExemplo(
            id: 'r-9',
            descricao: 'Febre de 38 graus.',
            autor: 'Rui Fictício',
          ),
        );

        await tester.fling(find.byType(ListView), const Offset(0, 400), 1000);
        await tester.pumpAndSettle();

        expect(find.text('Febre de 38 graus.'), findsOneWidget);
        expect(app.registos.consultas, [
          'Ocorrencia',
          'Nota',
          'Ocorrencia',
          'Nota',
        ]);
      },
    );

    testWidgets('sem registos, indica que ainda não há ocorrências', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      app.registos.ocorrencias.clear();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(
        find.text('Ainda não há ocorrências registadas nesta ficha.'),
        findsOneWidget,
      );
    });

    testWidgets('sem ligação mostra a mensagem e permite tentar novamente', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      app.registos.erroListar = const SemLigacaoErro();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text(const SemLigacaoErro().mensagem), findsOneWidget);

      app.registos.erroListar = null;
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      expect(find.text('Queda ligeira sem lesões.'), findsOneWidget);
      expect(find.text('Tentar novamente'), findsNothing);
    });
  });
}
