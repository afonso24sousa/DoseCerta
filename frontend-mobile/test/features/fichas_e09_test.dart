import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/features/fichas/editar_ficha_controller.dart';
import 'package:dosecerta/models/categoria_documento.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/validador_ficha.dart';
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

  Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    // Sem foco num campo, para o ecrã não voltar a deslocar-se para ele depois de mostrar o alvo.
    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle();
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  Future<void> abrirEdicao(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
    await tocar(tester, find.text('Editar'));
    expect(app.local, AppRouter.editarFicha('f-1'));
  }

  group('E09 Editar ficha', () {
    testWidgets(
      'abre com os dados atuais, guarda e volta à ficha atualizada (RF-029)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrirEdicao(tester);

        expect(find.text('Maria Fictícia Santos'), findsOneWidget);
        expect(find.text('17/05/1941'), findsOneWidget);
        expect(find.text('Autonomia parcial'), findsOneWidget);

        await tester.enterText(
          campo('Contacto de emergência'),
          '+351 910 000 002',
        );
        await tocar(tester, find.text('Hipertensão arterial'));
        await tocar(tester, find.text('Guardar'));

        final dados = app.fichas.ultimaEdicao!;
        expect(dados.contactoEmergencia, '+351 910 000 002');
        expect(dados.dataNascimento, '1941-05-17');
        expect(dados.condicoesSaudeIds, [1, 2]);
        expect(dados.toJsonEditar().containsKey('propria'), isFalse);
        expect(app.fichas.ultimosDocumentos, isEmpty);
        expect(app.local, AppRouter.ficha('f-1'));
        expect(
          find.text('Contacto de emergência: +351 910 000 002'),
          findsOneWidget,
        );
        expect(find.text('Alterações guardadas.'), findsOneWidget);
      },
    );

    testWidgets('validação prévia mantém os dados e não envia (RNF-028)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      await abrirEdicao(tester);

      await tester.enterText(campo('Nome completo'), '');
      await tester.enterText(campo('Data de nascimento'), '31/02/1941');
      await tocar(tester, find.text('Guardar'));

      expect(find.text('Nome completo: é obrigatório.'), findsOneWidget);
      expect(find.text(ValidadorFicha.erroData), findsOneWidget);
      expect(find.text('31/02/1941'), findsOneWidget);
      expect(app.fichas.ultimaEdicao, isNull);
    });

    testWidgets(
      'acrescenta um documento: escolhe a categoria antes da origem (D3)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrirEdicao(tester);

        await tocar(
          tester,
          find.text('Carregar: Documento a acrescentar (opcional)'),
        );
        await tester.tap(find.text('Relatório clínico'));
        await tester.pumpAndSettle();
        expect(find.text('Carregar: Relatório clínico'), findsOneWidget);
        await tester.tap(find.text('Câmara'));
        await tester.pumpAndSettle();

        expect(find.text('receita.pdf'), findsOneWidget);
        await tocar(tester, find.text('Guardar'));
        final documento = app.fichas.ultimosDocumentos.single;
        expect(documento.categoria, CategoriaDocumento.relatorioClinico);
      },
    );

    testWidgets(
      'documento não guardado: nenhuma alteração guardada e os dados mantêm-se (RF-023, CA3)',
      (tester) async {
        app.fichas
          ..juntar(fichaExemplo())
          ..erroEditar = const ProblemaApi(
            codigo: 'DOCUMENTO_NAO_GUARDADO',
            mensagem: 'x',
            estadoHttp: 503,
          );
        await abrirEdicao(tester);
        await tester.enterText(campo('Nome completo'), 'Maria Editada');
        await tocar(tester, find.text('Guardar'));

        expect(
          find.text(EditarFichaController.documentoNaoGuardado),
          findsOneWidget,
        );
        expect(app.local, AppRouter.editarFicha('f-1'));
        expect(find.text('Maria Editada'), findsOneWidget);
      },
    );

    testWidgets('erro do servidor num campo aparece por baixo do campo', (
      tester,
    ) async {
      app.fichas
        ..juntar(fichaExemplo())
        ..erroEditar = const ProblemaApi(
          codigo: 'VALIDACAO_INVALIDA',
          mensagem: 'x',
          estadoHttp: 400,
          campos: [
            ErroCampo(
              campo: 'contactoEmergencia',
              regra: ValidadorFicha.regraContacto,
            ),
            ErroCampo(
              campo: 'documentos[0].categoria',
              regra: 'É obrigatória.',
            ),
          ],
        );
      await abrirEdicao(tester);
      await tocar(tester, find.text('Guardar'));

      expect(
        find.text(
          'Contacto de emergência: tem de ser um número de telefone com algarismos, espaços e o prefixo + '
          'opcional, até 30 caracteres, por exemplo +351 910 000 001.',
        ),
        findsOneWidget,
      );
      expect(
        find.text('documentos[0].categoria: é obrigatória.'),
        findsOneWidget,
      );
    });

    testWidgets('ficha arquivada: não mostra o formulário (RF-120)', (
      tester,
    ) async {
      app = AppFichas(inicio: AppRouter.editarFicha('f-1'));
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      expect(
        find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
        findsOneWidget,
      );
      expect(find.text('Guardar'), findsNothing);
    });

    testWidgets('Familiar: não mostra o formulário (RF-029, RF-048)', (
      tester,
    ) async {
      app = AppFichas(inicio: AppRouter.editarFicha('f-1'));
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      expect(
        find.text(MensagensErro.catalogo['ACESSO_PROIBIDO']!),
        findsOneWidget,
      );
      expect(find.text('Guardar'), findsNothing);
    });

    testWidgets('Cancelar volta à ficha sem guardar', (tester) async {
      app.fichas.juntar(fichaExemplo());
      await abrirEdicao(tester);
      await tocar(tester, find.text('Cancelar'));
      expect(app.local, AppRouter.ficha('f-1'));
      expect(app.fichas.ultimaEdicao, isNull);
    });

    testWidgets(
      'alvos de toque de 48 x 48 dp e letra a 150% (RNF-025, RNF-026)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        final semantica = tester.ensureSemantics();
        await abrirEdicao(tester);
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();

        final grande = AppFichas(inicio: AppRouter.editarFicha('f-1'));
        grande.fichas.juntar(fichaExemplo());
        await tester.pumpWidget(grande.widget(escalaLetra: 1.5));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
      },
    );
  });
}
