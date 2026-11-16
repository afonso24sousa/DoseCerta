import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/pessoas/convites_recebidos_page.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_pessoas.dart';

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.convites);
    app.convites.recebidos
      ..add(conviteExemplo())
      ..add(
        conviteExemplo(
          id: 'c-2',
          nomeFicha: 'João Fictício Lopes',
          papel: Papel.familiar,
          convidadoPor: 'Rui Fictício',
        ),
      );
  });

  group('E14 Convites recebidos', () {
    testWidgets('mostra a ficha, o papel e quem convidou (RF-042)', (
      tester,
    ) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Maria Fictícia Santos'), findsOneWidget);
      expect(
        find.text('Cuidador | convidado por Ana Fictícia'),
        findsOneWidget,
      );
      expect(
        find.text('Familiar | convidado por Rui Fictício'),
        findsOneWidget,
      );
      expect(find.text('Aceitar'), findsNWidgets(2));
      expect(find.text('Recusar'), findsNWidgets(2));
      // Sem convite para Cuidador principal, a nota não aparece.
      expect(find.text(ConvitesRecebidosPage.notaPrincipal), findsNothing);
    });

    testWidgets(
      'o convite para Cuidador principal mostra o papel e a nota (RF-026, RF-027)',
      (tester) async {
        app.convites.recebidos.add(
          conviteExemplo(
            id: 'c-3',
            nomeFicha: 'Antónia Fictícia',
            paraPrincipal: true,
            convidadoPor: 'Sofia Fictícia',
          ),
        );
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(
          find.text('Cuidador principal | convidado por Sofia Fictícia'),
          findsOneWidget,
        );
        expect(find.text(ConvitesRecebidosPage.notaPrincipal), findsOneWidget);
      },
    );

    testWidgets(
      'aceitar tira o convite da lista e volta a pedir as fichas (RF-045)',
      (tester) async {
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();
        final listagensAntes = app.fichas.listagens;

        await tester.tap(find.text('Aceitar').first);
        await tester.pumpAndSettle();

        expect(app.convites.respostas, ['aceitar c-1']);
        expect(find.text('Maria Fictícia Santos'), findsNothing);
        expect(find.text('João Fictício Lopes'), findsOneWidget);
        expect(
          find.text(
            'Convite aceite. A ficha Maria Fictícia Santos está nas suas '
            'fichas.',
          ),
          findsOneWidget,
        );
        expect(app.fichas.listagens, listagensAntes + 1);
      },
    );

    testWidgets('recusar tira o convite da lista (RF-046)', (tester) async {
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Recusar').last);
      await tester.pumpAndSettle();

      expect(app.convites.respostas, ['recusar c-2']);
      expect(find.text('João Fictício Lopes'), findsNothing);
      expect(find.text('Convite recusado.'), findsOneWidget);
    });

    testWidgets('convite já respondido: mostra a mensagem e tira-o da lista', (
      tester,
    ) async {
      app.convites.erroResponder = const ProblemaApi(
        codigo: 'CONVITE_JA_RESPONDIDO',
        mensagem: 'O convite já foi respondido.',
        estadoHttp: 409,
      );
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Aceitar').first);
      await tester.pumpAndSettle();

      expect(
        find.text('Este convite já foi aceite ou recusado.'),
        findsOneWidget,
      );
      expect(find.text('Maria Fictícia Santos'), findsNothing);
      expect(find.text('João Fictício Lopes'), findsOneWidget);
    });

    testWidgets('pessoa já associada: mostra a mensagem própria do ecrã', (
      tester,
    ) async {
      app.convites.erroResponder = const ProblemaApi(
        codigo: 'PESSOA_JA_ASSOCIADA',
        mensagem: 'Já associado.',
        estadoHttp: 409,
      );
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Aceitar').first);
      await tester.pumpAndSettle();

      expect(
        find.text(
          'Já está associado a esta ficha, por isso o convite não foi aceite.',
        ),
        findsOneWidget,
      );
    });

    testWidgets('sem ligação ao responder: mantém o convite na lista', (
      tester,
    ) async {
      app.convites.erroResponder = const SemLigacaoErro();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Recusar').first);
      await tester.pumpAndSettle();

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(find.text('Maria Fictícia Santos'), findsOneWidget);
      expect(find.text('Convite recusado.'), findsNothing);
    });

    testWidgets('sem convites pendentes mostra a indicação', (tester) async {
      app.convites.recebidos.clear();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();

      expect(find.text('Não tem convites pendentes.'), findsOneWidget);
    });

    testWidgets(
      'sem ligação ao carregar mostra o erro e permite tentar de novo',
      (tester) async {
        app.convites.erroListar = const SemLigacaoErro();
        await tester.pumpWidget(app.widget());
        await tester.pumpAndSettle();

        expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);

        app.convites.erroListar = null;
        await tester.tap(find.text('Tentar novamente'));
        await tester.pumpAndSettle();
        expect(find.text('Maria Fictícia Santos'), findsOneWidget);
      },
    );

    testWidgets('alvos de toque de pelo menos 48 x 48 dp (RNF-025)', (
      tester,
    ) async {
      final semantica = tester.ensureSemantics();
      await tester.pumpWidget(app.widget());
      await tester.pumpAndSettle();
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });

    testWidgets('letra a 150% sem erros de layout (RNF-026)', (tester) async {
      await tester.pumpWidget(app.widget(escalaLetra: 1.5));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.byType(Card), findsNWidgets(2));
    });
  });

  group('E05 e E14', () {
    testWidgets(
      'E05 indica quantos convites estão pendentes e abre E14 (RF-042)',
      (tester) async {
        final e05 = AppFichas();
        e05.fichas.juntar(fichaExemplo());
        e05.convites.recebidos
          ..add(conviteExemplo())
          ..add(conviteExemplo(id: 'c-2'));
        await tester.pumpWidget(e05.widget());
        await tester.pumpAndSettle();

        await tester.scrollUntilVisible(
          find.text('Convites recebidos (2)'),
          100,
        );
        await tester.tap(find.text('Convites recebidos (2)'));
        await tester.pumpAndSettle();
        // E14 é aberto com `push`: o caminho de base continua a ser o de E05, por isso verifica-se o ecrã.
        expect(find.byType(ConvitesRecebidosPage), findsOneWidget);

        await tester.tap(find.text('Recusar').first);
        await tester.pumpAndSettle();
        await tester.tap(find.byType(BackButton));
        await tester.pumpAndSettle();

        expect(find.byType(ConvitesRecebidosPage), findsNothing);
        expect(e05.local, AppRouter.fichas);
        expect(find.text('Convites recebidos (1)'), findsOneWidget);
      },
    );
  });
}
