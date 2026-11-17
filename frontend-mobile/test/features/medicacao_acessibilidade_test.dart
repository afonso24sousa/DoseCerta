import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_tratamentos.dart';

/// Ecrãs da F005 num telemóvel de 360 x 740 dp, com a letra do sistema a 100% e a 150%: sem erros de layout
/// (RNF-026) e com alvos de toque de pelo menos 48 x 48 dp (RNF-025).
void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  void telemovel(WidgetTester tester) {
    tester.view
      ..physicalSize = const Size(360 * 3, 740 * 3)
      ..devicePixelRatio = 3;
    addTearDown(tester.view.reset);
  }

  AppFichas comTratamentos(String inicio, {EstadoFicha? estado, Papel? papel}) {
    return AppFichas(inicio: inicio)
      ..fichas.juntar(
        fichaExemplo(
          estado: estado ?? EstadoFicha.ativa,
          papel: papel ?? Papel.cuidador,
          principal: papel != Papel.familiar,
        ),
      )
      ..tratamentos.juntar(
        tratamentoExemplo(
          medicamento: 'Metformina de libertação prolongada',
          horarios: const ['08:00', '14:00', '20:00'],
          receitaDocumentoId: 'd-r',
        ),
      )
      ..tratamentos.juntar(
        tratamentoExemplo(
          id: 't-2',
          medicamento: 'Amoxicilina',
          dataFim: null,
          estado: EstadoTratamento.terminado,
        ),
      );
  }

  final cenarios = <String, AppFichas Function()>{
    'E15 do Cuidador': () => comTratamentos(AppRouter.medicacaoFicha('f-1')),
    'E15 arquivada': () => comTratamentos(
      AppRouter.medicacaoFicha('f-1'),
      estado: EstadoFicha.arquivada,
    ),
    'E15 do Familiar': () =>
        comTratamentos(AppRouter.medicacaoFicha('f-1'), papel: Papel.familiar),
    'E16 registar': () => comTratamentos(AppRouter.novoTratamento('f-1')),
    'E16 editar': () =>
        comTratamentos(AppRouter.editarTratamento('f-1', 't-1')),
    'E17 do Cuidador': () => comTratamentos(AppRouter.tratamento('f-1', 't-1')),
    'E17 arquivada': () => comTratamentos(
      AppRouter.tratamento('f-1', 't-1'),
      estado: EstadoFicha.arquivada,
    ),
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

  testWidgets('E16 com os erros de campo e horas, a 150% num telemóvel', (
    tester,
  ) async {
    telemovel(tester);
    final app = comTratamentos(AppRouter.novoTratamento('f-1'));
    final semantica = tester.ensureSemantics();
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    Future<void> tocar(Finder alvo) async {
      FocusManager.instance.primaryFocus?.unfocus();
      await tester.pumpAndSettle();
      await tester.ensureVisible(alvo);
      await tester.pumpAndSettle();
      await tester.tap(alvo);
      await tester.pumpAndSettle();
    }

    await tester.enterText(
      find.widgetWithText(TextField, 'Hora a acrescentar'),
      '08:00',
    );
    await tocar(find.text('Adicionar hora'));
    await tocar(find.widgetWithText(FilledButton, 'Registar tratamento'));

    expect(find.text('Medicamento: é obrigatório.'), findsOneWidget);
    expect(tester.takeException(), isNull);
    // De volta ao topo: um alvo cortado pela barra superior a meio do scroll mediria menos de 48 dp.
    await tester.fling(
      find.byType(SingleChildScrollView),
      const Offset(0, 3000),
      5000,
    );
    await tester.pumpAndSettle();
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });

  testWidgets('confirmação de terminar a 150% num telemóvel', (tester) async {
    telemovel(tester);
    final app = comTratamentos(AppRouter.tratamento('f-1', 't-1'));
    final semantica = tester.ensureSemantics();
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    // O ListView só constrói o que está perto do ecrã: desce até o botão existir.
    final terminar = find.text('Terminar tratamento');
    await tester.scrollUntilVisible(
      terminar,
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.pumpAndSettle();
    await tester.tap(terminar);
    await tester.pumpAndSettle();

    expect(find.text('Terminar o tratamento?'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });
}
