import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/features/medicacao/tratamento_controller.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_tratamentos.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.tratamento('f-1', 't-1'));
    app.tratamentos.juntar(tratamentoExemplo(receitaDocumentoId: 'd-r'));
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

  group('E17 Detalhe do tratamento', () {
    testWidgets('mostra os dados e o estado (RF-054)', (tester) async {
      app.fichas.juntar(fichaExemplo());
      await abrir(tester);

      expect(find.text('Metformina 500 mg'), findsOneWidget);
      expect(find.text('Estado: Em curso'), findsOneWidget);
      expect(find.text('Moderado'), findsOneWidget);
      expect(find.text('08:00 e 20:00'), findsOneWidget);
      expect(find.text('07/10/2026 a 05/12/2026'), findsOneWidget);
      expect(find.text('Editar'), findsOneWidget);
      expect(find.text('Terminar tratamento'), findsOneWidget);
    });

    testWidgets(
      'terminar pede confirmação e mostra o estado novo (RF-056, RF-057)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);

        await tocar(tester, find.text('Terminar tratamento'));
        expect(find.text('Terminar o tratamento?'), findsOneWidget);
        await tocar(tester, find.text('Cancelar'));
        expect(app.tratamentos.terminos, 0);

        await tocar(tester, find.text('Terminar tratamento'));
        await tocar(tester, find.text('Sim, terminar o tratamento'));

        expect(app.tratamentos.terminos, 1);
        expect(find.text('Estado: Terminado'), findsOneWidget);
        expect(find.text(TratamentoController.terminado), findsOneWidget);
        expect(find.text('Editar'), findsNothing);
        expect(find.text('Terminar tratamento'), findsNothing);
      },
    );

    testWidgets('o Familiar só consulta (RF-048)', (tester) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await abrir(tester);

      expect(find.text('Metformina 500 mg'), findsOneWidget);
      expect(find.text('Abrir a receita'), findsOneWidget);
      expect(find.text('Editar'), findsNothing);
      expect(find.text('Terminar tratamento'), findsNothing);
    });

    testWidgets(
      'numa ficha arquivada não se edita, mas pode-se terminar (OP-31)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
        await abrir(tester);

        expect(find.textContaining('Ficha arquivada'), findsOneWidget);
        expect(find.text('Editar'), findsNothing);
        expect(find.text('Terminar tratamento'), findsOneWidget);
      },
    );

    testWidgets('um tratamento terminado fica só para consulta (RF-057)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      app.tratamentos.juntar(
        tratamentoExemplo(estado: EstadoTratamento.terminado),
      );
      await abrir(tester);

      expect(find.text('Estado: Terminado'), findsOneWidget);
      expect(find.text('Editar'), findsNothing);
      expect(find.text('Terminar tratamento'), findsNothing);
    });

    testWidgets(
      'já terminado por outro Cuidador: mostra a mensagem e o estado atual',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);
        app.tratamentos
          ..erroTerminar = const ProblemaApi(
            codigo: 'TRATAMENTO_JA_TERMINADO',
            mensagem: '',
            estadoHttp: 409,
          )
          ..juntar(tratamentoExemplo(estado: EstadoTratamento.terminado));

        await tocar(tester, find.text('Terminar tratamento'));
        await tocar(tester, find.text('Sim, terminar o tratamento'));

        expect(find.text('O tratamento já está terminado.'), findsOneWidget);
        expect(find.text('Estado: Terminado'), findsOneWidget);
        expect(find.text('Terminar tratamento'), findsNothing);
      },
    );

    testWidgets('abre a receita no visualizador do dispositivo', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      await abrir(tester);

      await tocar(tester, find.text('Abrir a receita'));

      expect(app.documentos.descargas, 1);
      expect(app.abridor.abertos.single.$1, 'receita.pdf');
      expect(app.abridor.abertos.single.$2, 'application/pdf');
    });

    testWidgets(
      'uma falha ao obter a receita aparece no lugar dela, com repetição (RF-124)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        app.documentos.erroDescarregar = const ProblemaApi(
          codigo: 'DOCUMENTO_INDISPONIVEL',
          mensagem: '',
          estadoHttp: 503,
        );
        await abrir(tester);

        await tocar(tester, find.text('Abrir a receita'));
        expect(
          find.text(TratamentoController.receitaIndisponivel),
          findsOneWidget,
        );

        app.documentos.erroDescarregar = null;
        await tocar(tester, find.text('Tentar novamente'));
        expect(app.abridor.abertos, hasLength(1));
      },
    );

    testWidgets('«Editar» abre o formulário de edição (E16)', (tester) async {
      app.fichas.juntar(fichaExemplo());
      await abrir(tester);

      await tocar(tester, find.text('Editar'));

      expect(app.local, AppRouter.editarTratamento('f-1', 't-1'));
    });
  });
}
