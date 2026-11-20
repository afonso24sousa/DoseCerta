import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_tratamentos.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.medicacaoFicha('f-1'));
    app.tratamentos
      ..juntar(tratamentoExemplo())
      ..juntar(
        tratamentoExemplo(
          id: 't-2',
          medicamento: 'Amoxicilina',
          dose: '1 g',
          dataFim: null,
          horarios: const ['09:00'],
          estado: EstadoTratamento.terminado,
        ),
      );
  });

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  group('E15 Medicação da ficha', () {
    testWidgets(
      'o Cuidador vê os tratamentos em curso e os terminados e pode registar (RF-054, RF-057)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);

        expect(find.text('Em curso'), findsOneWidget);
        expect(find.text('Terminados'), findsOneWidget);
        expect(find.text('Metformina 500 mg'), findsOneWidget);
        expect(find.textContaining('Moderado · 08:00 e 20:00'), findsOneWidget);
        expect(find.textContaining('07/10/2026 a 05/12/2026'), findsOneWidget);
        expect(find.text('Amoxicilina 1 g'), findsOneWidget);
        expect(
          find.textContaining('desde 07/10/2026, contínuo'),
          findsOneWidget,
        );
        expect(find.text('Registar tratamento'), findsOneWidget);

        await tester.tap(find.text('Registar tratamento'));
        await tester.pumpAndSettle();
        expect(app.local, AppRouter.novoTratamento('f-1'));
      },
    );

    testWidgets('tocar num tratamento abre o detalhe (E17)', (tester) async {
      app.fichas.juntar(fichaExemplo());
      await abrir(tester);

      await tester.tap(find.text('Metformina 500 mg'));
      await tester.pumpAndSettle();

      expect(app.local, AppRouter.tratamento('f-1', 't-1'));
    });

    testWidgets('o Familiar só consulta (RF-048)', (tester) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await abrir(tester);

      expect(find.text('Metformina 500 mg'), findsOneWidget);
      expect(find.text('Amoxicilina 1 g'), findsOneWidget);
      expect(find.text('Registar tratamento'), findsNothing);
    });

    testWidgets(
      'numa ficha arquivada aparece o aviso e não se regista (RF-120)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
        await abrir(tester);

        expect(
          find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
          findsOneWidget,
        );
        expect(find.text('Metformina 500 mg'), findsOneWidget);
        expect(find.text('Registar tratamento'), findsNothing);
      },
    );

    testWidgets('sem tratamentos mostra as secções vazias', (tester) async {
      app.fichas.juntar(fichaExemplo());
      app.tratamentos.tratamentos.clear();
      await abrir(tester);

      expect(find.text('Não há tratamentos em curso.'), findsOneWidget);
      expect(find.text('Não há tratamentos terminados.'), findsOneWidget);
    });

    testWidgets('um erro na lista mostra a mensagem e deixa repetir', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      app.tratamentos.erroListar = const SemLigacaoErro();
      await abrir(tester);

      expect(find.text(const SemLigacaoErro().mensagem), findsOneWidget);

      app.tratamentos.erroListar = null;
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      expect(find.text('Metformina 500 mg'), findsOneWidget);
    });
  });
}
