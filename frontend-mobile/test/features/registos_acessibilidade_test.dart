import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/tipo_registo.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_registos.dart';

/// Ecrãs da F009 (E23 e E24) num telemóvel de 360 x 740 dp, com a letra do sistema a 100% e a 150%, nos estados com
/// mais texto: sem erros de layout (RNF-026) e com alvos de toque de pelo menos 48 x 48 dp (RNF-025).
void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  void telemovel(WidgetTester tester) {
    tester.view
      ..physicalSize = const Size(360 * 3, 740 * 3)
      ..devicePixelRatio = 3;
    addTearDown(tester.view.reset);
  }

  AppFichas comRegistos(AppFichas app) {
    app.registos.ocorrencias.addAll([
      registoExemplo(
        descricao:
            'Queda ligeira na casa de banho, sem lesões visíveis. Ficou em '
            'observação durante a tarde e queixou-se de dores no joelho.',
        autor: 'Ana Fictícia dos Santos Pereira',
      ),
      registoExemplo(id: 'r-2', autor: 'Rui Fictício'),
    ]);
    app.registos.notas.add(registoExemplo(id: 'r-3', tipo: TipoRegisto.nota));
    return app;
  }

  final cenarios = <String, AppFichas Function()>{
    'E23 do Cuidador': () => comRegistos(
      AppFichas(inicio: AppRouter.registosFicha('f-1'))
        ..fichas.juntar(fichaExemplo()),
    ),
    'E23 do Familiar': () => comRegistos(
      AppFichas(inicio: AppRouter.registosFicha('f-1'))
        ..fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false)),
    ),
    'E23 arquivada': () => comRegistos(
      AppFichas(inicio: AppRouter.registosFicha('f-1'))
        ..fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada)),
    ),
    'E24 ocorrência': () =>
        AppFichas(inicio: AppRouter.novoRegisto('f-1'))
          ..fichas.juntar(fichaExemplo()),
    'E24 nota': () =>
        AppFichas(inicio: AppRouter.novoRegisto('f-1', tipo: TipoRegisto.nota))
          ..fichas.juntar(fichaExemplo()),
  };

  for (final cenario in cenarios.entries) {
    for (final escala in [1.0, 1.5]) {
      testWidgets(
        '${cenario.key}, letra a ${(escala * 100).round()}%: sem erros de layout e alvos de 48 x 48 dp',
        (tester) async {
          telemovel(tester);
          final semantica = tester.ensureSemantics();
          await tester.pumpWidget(cenario.value().widget(escalaLetra: escala));
          await tester.pumpAndSettle();
          expect(tester.takeException(), isNull);
          await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
          semantica.dispose();
        },
      );
    }
  }

  testWidgets('E24 com os erros de campo, a 150% num telemóvel', (
    tester,
  ) async {
    telemovel(tester);
    final semantica = tester.ensureSemantics();
    final app = AppFichas(inicio: AppRouter.novoRegisto('f-1'))
      ..fichas.juntar(fichaExemplo());
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    final guardar = find.text('Guardar');
    await tester.dragUntilVisible(
      guardar,
      find.byType(ListView),
      const Offset(0, -200),
    );
    await tester.pumpAndSettle();
    await tester.tap(guardar);
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(app.registos.ocorrenciasEnviadas, isEmpty);
    await tester.dragUntilVisible(
      find.textContaining('Descrição: é obrigatória'),
      find.byType(ListView),
      const Offset(0, 200),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });
}
