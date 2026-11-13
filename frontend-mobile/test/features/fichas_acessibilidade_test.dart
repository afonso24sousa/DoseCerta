import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

/// Ecrãs da F002 e da F003 num telemóvel de 360 x 740 dp, com a letra do sistema a 100% e a 150%, nos estados com
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

  final cenarios = <String, AppFichas Function()>{
    'E05 com fichas ativas': () => AppFichas()
      ..fichas.juntar(fichaExemplo(nome: 'Maria Fictícia dos Santos Pereira'))
      ..fichas.juntar(
        fichaExemplo(id: 'f-2', papel: Papel.familiar, principal: false),
      ),
    'E06 com a ficha própria indisponível': () =>
        AppFichas(inicio: AppRouter.novaFicha)
          ..fichas.juntar(fichaExemplo(propria: true)),
    'E08 do Cuidador': () =>
        AppFichas(inicio: AppRouter.ficha('f-1'))
          ..fichas.juntar(fichaExemplo()),
    'E08 arquivada': () =>
        AppFichas(inicio: AppRouter.ficha('f-1'))
          ..fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada)),
    'E08 do Familiar': () => AppFichas(inicio: AppRouter.ficha('f-1'))
      ..fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false)),
    'E08 sem acesso': () => AppFichas(inicio: AppRouter.ficha('f-1'))
      ..fichas.erroObter = const ProblemaApi(
        codigo: 'FICHA_NAO_ENCONTRADA',
        mensagem: 'x',
        estadoHttp: 404,
      ),
    'E09': () =>
        AppFichas(inicio: AppRouter.editarFicha('f-1'))
          ..fichas.juntar(fichaExemplo()),
    'E10 do Cuidador': () =>
        AppFichas(inicio: AppRouter.documentosFicha('f-1'))
          ..fichas.juntar(fichaExemplo()),
    'E11 ativa': () =>
        AppFichas(inicio: AppRouter.estadoFicha('f-1'))
          ..fichas.juntar(fichaExemplo()),
    'E11 arquivada': () =>
        AppFichas(inicio: AppRouter.estadoFicha('f-1'))
          ..fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada)),
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

  testWidgets('E06 com os erros de campo, a 150% num telemóvel', (
    tester,
  ) async {
    telemovel(tester);
    final app = AppFichas(inicio: AppRouter.novaFicha);
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    final seguinte = find.text('Seguinte');
    await tester.ensureVisible(seguinte);
    await tester.pumpAndSettle();
    await tester.tap(seguinte);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Nome completo: é obrigatório.'), findsOneWidget);
  });

  testWidgets('E07 com o convite e o erro do e-mail, a 150% num telemóvel', (
    tester,
  ) async {
    telemovel(tester);
    final app = AppFichas(inicio: AppRouter.novaFicha);
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
      find.widgetWithText(TextField, 'Nome completo'),
      'Maria Fictícia Santos',
    );
    await tester.enterText(
      find.widgetWithText(TextField, 'Data de nascimento'),
      '17/05/1941',
    );
    await tester.enterText(
      find.widgetWithText(TextField, 'Contacto de emergência'),
      '+351 910 000 001',
    );
    await tocar(find.text('Seguinte'));
    await tocar(find.textContaining('Convidar outra pessoa'));
    await tocar(find.text('Criar ficha'));

    expect(
      find.text('E-mail da pessoa a convidar: é obrigatório.'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
    final semantica = tester.ensureSemantics();
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });

  testWidgets('folhas de categoria e de confirmação: alvos de 48 x 48 dp', (
    tester,
  ) async {
    telemovel(tester);
    final app = AppFichas(inicio: AppRouter.documentosFicha('f-1'))
      ..fichas.juntar(fichaExemplo());
    final semantica = tester.ensureSemantics();
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    final carregar = find.text('Carregar: Documento a acrescentar');
    await tester.ensureVisible(carregar);
    await tester.pumpAndSettle();
    await tester.tap(carregar);
    await tester.pumpAndSettle();
    expect(find.text('Categoria do documento'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));

    await tester.tap(find.text('Receita médica'));
    await tester.pumpAndSettle();
    expect(find.text('Câmara'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });

  testWidgets('confirmação de eliminar a 150% num telemóvel', (tester) async {
    telemovel(tester);
    final app = AppFichas(inicio: AppRouter.estadoFicha('f-1'))
      ..fichas.juntar(fichaExemplo());
    final semantica = tester.ensureSemantics();
    await tester.pumpWidget(app.widget(escalaLetra: 1.5));
    await tester.pumpAndSettle();

    final eliminar = find.widgetWithText(OutlinedButton, 'Eliminar');
    await tester.ensureVisible(eliminar);
    await tester.pumpAndSettle();
    await tester.tap(eliminar);
    await tester.pumpAndSettle();

    expect(find.text('Eliminar ficha'), findsWidgets);
    expect(tester.takeException(), isNull);
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    semantica.dispose();
  });
}
