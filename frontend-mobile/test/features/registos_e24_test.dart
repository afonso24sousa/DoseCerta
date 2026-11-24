import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/features/registos/novo_registo_controller.dart';
import 'package:dosecerta/features/registos/novo_registo_page.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/models/tipo_registo.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

/// Campo de texto com o rótulo [rotulo].
Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

/// Texto atual do campo com o rótulo [rotulo].
String textoDe(WidgetTester tester, String rotulo) =>
    tester.widget<TextField>(campo(rotulo)).controller!.text;

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.registosFicha('f-1'));
    app.fichas.juntar(fichaExemplo());
  });

  /// Abre E24 a partir de E23, como o Utilizador.
  Future<void> abrirNovo(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
    await tester.tap(find.text('Novo registo'));
    await tester.pumpAndSettle();
  }

  Future<void> guardar(WidgetTester tester) async {
    await tester.dragUntilVisible(
      find.text('Guardar'),
      find.byType(ListView),
      const Offset(0, -200),
    );
    await tester.tap(find.text('Guardar'));
    await tester.pumpAndSettle();
  }

  group('NovoRegistoController.validar', () {
    test('a descrição é obrigatória e tem no máximo 2000 caracteres (RF-090, RF-091)', () {
      expect(NovoRegistoController.validar(descricao: '   ').keys, [
        'descricao',
      ]);
      expect(
        NovoRegistoController.validar(descricao: 'a' * 2001)['descricao'],
        contains('2000'),
      );
      expect(NovoRegistoController.validar(descricao: 'a' * 2000), isEmpty);
    });

    test(
      'na ocorrência, a data e a hora têm de ser válidas (RF-090, RNF-028)',
      () {
        expect(
          NovoRegistoController.validar(
            descricao: 'Queda.',
            data: '31/02/2026',
            hora: '25:00',
          ).keys,
          ['data', 'hora'],
        );
        expect(
          NovoRegistoController.validar(
            descricao: 'Queda.',
            data: '07/10/2026',
            hora: '15:05',
          ),
          isEmpty,
        );
      },
    );
  });

  group('E24 Nova ocorrência ou nota', () {
    testWidgets(
      'propõe a data e a hora atuais e regista a ocorrência com o desvio de Lisboa (RF-090)',
      (tester) async {
        await abrirNovo(tester);

        expect(find.text('Ocorrência'), findsOneWidget);
        expect(find.text('Nota para os Cuidadores'), findsOneWidget);
        expect(find.text('Data e hora em que aconteceu'), findsOneWidget);
        expect(find.text(NovoRegistoPage.avisoDefinitivo), findsOneWidget);
        expect(textoDe(tester, 'Data'), '09/11/2026');
        expect(textoDe(tester, 'Hora'), '12:00');

        await tester.enterText(
          campo('Descrição'),
          '  Queda ligeira sem lesões. ',
        );
        await tester.enterText(campo('Hora'), '11:40');
        await guardar(tester);

        final enviada = app.registos.ocorrenciasEnviadas.single;
        expect(enviada.descricao, 'Queda ligeira sem lesões.');
        expect(enviada.ocorridaEm, '2026-11-09T11:40:00+00:00');
        expect(app.local, AppRouter.registosFicha('f-1'));
        expect(find.text('Ocorrência registada.'), findsOneWidget);
        expect(find.text('Queda ligeira sem lesões.'), findsOneWidget);
        expect(find.text('Aconteceu em 09/11/2026 11:40'), findsOneWidget);
      },
    );

    testWidgets(
      'regista uma nota só com o texto e volta ao separador Notas (RF-091)',
      (tester) async {
        await abrirNovo(tester);

        await tester.tap(find.text('Nota para os Cuidadores'));
        await tester.pumpAndSettle();
        expect(find.text('Data e hora em que aconteceu'), findsNothing);

        await tester.enterText(campo('Descrição'), 'Reforçar a hidratação.');
        await guardar(tester);

        expect(
          app.registos.notasEnviadas.single.descricao,
          'Reforçar a hidratação.',
        );
        expect(app.registos.ocorrenciasEnviadas, isEmpty);
        expect(find.text('Nota registada.'), findsOneWidget);
        expect(find.text('Reforçar a hidratação.'), findsOneWidget);
        expect(find.textContaining('Aconteceu em'), findsNothing);
      },
    );

    testWidgets('aberto no separador Notas, começa com a nota escolhida', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      await tester.tap(find.text('Notas'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Novo registo'));
      await tester.pumpAndSettle();

      expect(find.text(NovoRegistoPage.avisoDefinitivo), findsOneWidget);
      expect(find.text('Data e hora em que aconteceu'), findsNothing);
    });

    testWidgets(
      'os campos em falta indicam o campo e a correção, sem apagar os outros dados (RNF-028)',
      (tester) async {
        await abrirNovo(tester);

        await tester.enterText(campo('Hora'), '9h');
        await guardar(tester);

        expect(find.textContaining('Descrição: é obrigatória'), findsOneWidget);
        expect(find.textContaining('Hora: indique a hora'), findsOneWidget);
        expect(textoDe(tester, 'Hora'), '9h');
        expect(textoDe(tester, 'Data'), '09/11/2026');
        expect(app.registos.ocorrenciasEnviadas, isEmpty);
      },
    );

    testWidgets(
      'o erro de campo do servidor aparece no campo e mantém o texto escrito (RNF-028)',
      (tester) async {
        app.registos.erroRegistar = const ProblemaApi(
          codigo: 'VALIDACAO_INVALIDA',
          mensagem: 'x',
          estadoHttp: 400,
          campos: [
            ErroCampo(campo: 'ocorridaEm', regra: 'Data e hora inválidas.'),
          ],
        );
        await abrirNovo(tester);

        await tester.enterText(campo('Descrição'), 'Queda.');
        await guardar(tester);

        expect(
          find.text('Data e hora em que aconteceu: data e hora inválidas.'),
          findsOneWidget,
        );
        expect(textoDe(tester, 'Descrição'), 'Queda.');
        expect(find.text(NovoRegistoPage.avisoDefinitivo), findsOneWidget);
      },
    );

    testWidgets(
      'a recusa por ficha arquivada mostra a mensagem respetiva (RF-120)',
      (tester) async {
        app.registos.erroRegistar = const ProblemaApi(
          codigo: 'FICHA_ARQUIVADA',
          mensagem: 'x',
          estadoHttp: 409,
        );
        await abrirNovo(tester);

        await tester.enterText(campo('Descrição'), 'Queda.');
        await guardar(tester);

        expect(
          find.textContaining('Ficha arquivada: só consulta'),
          findsOneWidget,
        );
        expect(textoDe(tester, 'Descrição'), 'Queda.');
      },
    );

    testWidgets(
      'o Familiar que abre o ecrã diretamente não tem formulário (RF-048)',
      (tester) async {
        app = AppFichas(
          inicio: AppRouter.novoRegisto('f-1', tipo: TipoRegisto.ocorrencia),
        );
        app.fichas.juntar(
          fichaExemplo(papel: Papel.familiar, principal: false),
        );
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.textContaining('o Familiar só consulta'), findsOneWidget);
        expect(find.text('Guardar'), findsNothing);
      },
    );

    testWidgets(
      'numa ficha arquivada aberta diretamente não há formulário (RF-120)',
      (tester) async {
        app = AppFichas(inicio: AppRouter.novoRegisto('f-1'));
        app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(
          find.textContaining('Ficha arquivada: só consulta'),
          findsOneWidget,
        );
        expect(find.text('Guardar'), findsNothing);
      },
    );
  });
}
