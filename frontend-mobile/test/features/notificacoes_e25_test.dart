import 'dart:async';

import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/navegacao/estrutura_navegacao.dart';
import 'package:dosecerta/features/notificacoes/notificacoes_page.dart';
import 'package:dosecerta/models/estado_envio_email.dart';
import 'package:dosecerta/models/tipo_notificacao.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import '../suporte/falsos.dart';
import '../suporte/falsos_notificacoes.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  late NotificacoesRepositorioFalso repositorio;

  setUp(() {
    repositorio = NotificacoesRepositorioFalso()
      ..notificacoes = [
        notificacaoExemplo(
          id: 'n-1',
          tipo: TipoNotificacao.conviteRecebido,
          mensagem: 'Convite para a ficha de João Fictício Lopes.',
          nomeFicha: 'João Fictício Lopes',
          medicamento: null,
          previstaEm: null,
          criadaEm: '2026-10-06T09:00:00+01:00',
        ),
        notificacaoExemplo(
          id: 'n-2',
          tipo: TipoNotificacao.tomaGraveFamiliares,
          mensagem: 'A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.',
          medicamento: 'Insulina',
          previstaEm: '2026-10-07T10:00:00+01:00',
          criadaEm: '2026-10-07T11:00:00+01:00',
          email: EstadoEnvioEmail.naoEntregue,
        ),
        notificacaoExemplo(
          id: 'n-3',
          tipo: TipoNotificacao.novaOcorrencia,
          mensagem: 'Ana Fictícia registou uma ocorrência na ficha de Maria Fictícia Santos.',
          medicamento: null,
          previstaEm: null,
          criadaEm: '2026-10-07T08:30:00+01:00',
          email: EstadoEnvioEmail.pendente,
        ),
      ];
  });

  Widget envolver(Widget filho, {double escalaLetra = 1}) {
    return ProviderScope(
      overrides: [
        notificacoesRepositorioProvider.overrideWithValue(repositorio),
        relogioProvider.overrideWithValue(
          RelogioFixo(DateTime(2026, 11, 21, 12)),
        ),
      ],
      child: MaterialApp(
        theme: TemaDoseCerta.claro(),
        locale: const Locale('pt', 'PT'),
        supportedLocales: const [Locale('pt', 'PT')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        builder: (context, f) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(escalaLetra)),
          child: f!,
        ),
        home: filho,
      ),
    );
  }

  Future<void> abrir(WidgetTester tester, {double escalaLetra = 1}) async {
    // Ecrã alto, para a lista inteira ficar construída sem deslizar.
    tester.view.physicalSize = const Size(1080, 4800);
    tester.view.devicePixelRatio = 2.5;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      envolver(const NotificacoesPage(), escalaLetra: escalaLetra),
    );
    await tester.pumpAndSettle();
  }

  Future<void> filtrar(WidgetTester tester, String de, String ate) async {
    await tester.enterText(find.widgetWithText(TextField, 'De'), de);
    await tester.enterText(find.widgetWithText(TextField, 'Até'), ate);
    await tester.ensureVisible(find.text('Filtrar'));
    await tester.tap(find.text('Filtrar'));
    await tester.pumpAndSettle();
  }

  group('E25 Notificações', () {
    testWidgets(
      'mostra as notificações da mais recente para a mais antiga, com tipo, ficha, data e hora (RF-140, RNF-029)',
      (tester) async {
        await abrir(tester);

        expect(repositorio.pedidos, 1);
        final grave = tester.getTopLeft(
          find.text('Toma Grave em atraso prolongado'),
        );
        final ocorrencia = tester.getTopLeft(find.text('Nova ocorrência'));
        final convite = tester.getTopLeft(find.text('Convite recebido'));
        expect(grave.dy, lessThan(ocorrencia.dy));
        expect(ocorrencia.dy, lessThan(convite.dy));

        expect(
          find.text('07/10/2026 11:00 · Maria Fictícia Santos'),
          findsOneWidget,
        );
        expect(
          find.text('06/10/2026 09:00 · João Fictício Lopes'),
          findsOneWidget,
        );
        expect(
          find.text('Convite para a ficha de João Fictício Lopes.'),
          findsOneWidget,
        );
        expect(
          find.text('Atualizado ao abrir ou atualizar o ecrã.'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'nas tomas mostra o medicamento e a hora prevista; o e-mail não entregue fica identificado (RF-113, RF-119)',
      (tester) async {
        await abrir(tester);

        expect(
          find.text('Medicamento: Insulina · prevista às 10:00'),
          findsOneWidget,
        );
        expect(find.textContaining('Medicamento:'), findsOneWidget);
        expect(find.text('E-mail não entregue'), findsOneWidget);
        expect(find.text('E-mail pendente'), findsOneWidget);
        expect(find.text('E-mail enviado'), findsOneWidget);
      },
    );

    testWidgets('cada tipo tem texto e ícone próprios, não só cor', (
      tester,
    ) async {
      repositorio.notificacoes = [
        for (final (i, tipo) in TipoNotificacao.values.indexed)
          notificacaoExemplo(
            id: 'n-$i',
            tipo: tipo,
            criadaEm: '2026-10-0${i + 1}T09:00:00+01:00',
          ),
      ];
      await abrir(tester);

      for (final (tipo, icone) in [
        (TipoNotificacao.conviteRecebido, Icons.mail_outline),
        (TipoNotificacao.tomaEmAtraso, Icons.medication_outlined),
        (TipoNotificacao.tomaGraveFamiliares, Icons.warning_amber_outlined),
        (TipoNotificacao.novaOcorrencia, Icons.report_outlined),
        (TipoNotificacao.proximoCompromisso, Icons.event_outlined),
      ]) {
        expect(find.text(tipo.rotulo), findsOneWidget);
        expect(find.byIcon(icone), findsOneWidget);
      }
    });

    testWidgets('sem notificações indica que não há notificações', (
      tester,
    ) async {
      repositorio.notificacoes = [];
      await abrir(tester);

      expect(find.text('Não há notificações.'), findsOneWidget);
    });

    testWidgets('sem ligação mostra o erro e permite tentar de novo', (
      tester,
    ) async {
      repositorio.erro = const SemLigacaoErro();
      await abrir(tester);

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(find.text('Convite recebido'), findsNothing);

      repositorio.erro = null;
      final antes = repositorio.pedidos;
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      expect(repositorio.pedidos, greaterThan(antes));
      expect(find.text('Convite recebido'), findsOneWidget);
    });

    testWidgets('puxar para baixo pede o histórico de novo (sem push)', (
      tester,
    ) async {
      await abrir(tester);
      repositorio.notificacoes = [
        notificacaoExemplo(
          id: 'n-9',
          tipo: TipoNotificacao.proximoCompromisso,
          mensagem: 'Compromisso de Maria Fictícia Santos: Consulta de rotina.',
          medicamento: null,
          previstaEm: null,
        ),
      ];

      // O mesmo que puxar a lista para baixo.
      unawaited(
        tester
            .state<RefreshIndicatorState>(find.byType(RefreshIndicator))
            .show(),
      );
      await tester.pumpAndSettle();

      expect(repositorio.pedidos, 2);
      expect(find.text('Compromisso próximo'), findsOneWidget);
      expect(find.text('Convite recebido'), findsNothing);
    });

    testWidgets('o filtro envia de e ate no formato da API', (tester) async {
      await abrir(tester);
      repositorio.notificacoes = [];

      await filtrar(tester, '01/10/2026', '31/10/2026');

      expect(repositorio.ultimoDe, '2026-10-01');
      expect(repositorio.ultimoAte, '2026-10-31');
      expect(find.text('Não há notificações neste período.'), findsOneWidget);

      await tester.tap(find.text('Limpar filtro'));
      await tester.pumpAndSettle();
      expect(repositorio.ultimoDe, isNull);
      expect(repositorio.ultimoAte, isNull);
    });

    testWidgets(
      'Até anterior a De ou data inválida dá erro no campo, sem pedido à API (RNF-028)',
      (tester) async {
        await abrir(tester);

        await filtrar(tester, '31/10/2026', '01/10/2026');
        expect(
          find.text('Até: tem de ser igual ou posterior à data De.'),
          findsOneWidget,
        );
        expect(repositorio.pedidos, 1);
        expect(find.text('31/10/2026'), findsOneWidget);

        await filtrar(tester, '31/02/2026', '');
        expect(
          find.text('De: indique uma data válida no formato dd/mm/aaaa.'),
          findsOneWidget,
        );
        expect(repositorio.pedidos, 1);
      },
    );

    testWidgets('voltar ao separador Notificações pede o histórico de novo', (
      tester,
    ) async {
      final router = GoRouter(
        initialLocation: '/notificacoes',
        routes: [
          StatefulShellRoute.indexedStack(
            builder: (context, estado, shell) =>
                EstruturaNavegacao(shell: shell),
            branches: [
              for (final caminho in [
                '/hoje',
                '/fichas',
                '/agenda',
                '/notificacoes',
                '/conta',
              ])
                StatefulShellBranch(
                  routes: [
                    GoRoute(
                      path: caminho,
                      builder: (context, estado) => caminho == '/notificacoes'
                          ? const NotificacoesPage()
                          : const SizedBox.shrink(),
                    ),
                  ],
                ),
            ],
          ),
        ],
      );
      addTearDown(router.dispose);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            notificacoesRepositorioProvider.overrideWithValue(repositorio),
          ],
          child: MaterialApp.router(
            theme: TemaDoseCerta.claro(),
            locale: const Locale('pt', 'PT'),
            supportedLocales: const [Locale('pt', 'PT')],
            localizationsDelegates: GlobalMaterialLocalizations.delegates,
            routerConfig: router,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(repositorio.pedidos, 1);

      router.go('/conta');
      await tester.pumpAndSettle();
      router.go('/notificacoes');
      await tester.pumpAndSettle();

      expect(repositorio.pedidos, 2);
    });

    testWidgets(
      'a 150% de letra não há erros de layout e os alvos de toque têm 48 x 48 dp (RNF-025)',
      (tester) async {
        final semantica = tester.ensureSemantics();
        await abrir(tester, escalaLetra: 1.5);

        expect(tester.takeException(), isNull);
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();
      },
    );
  });
}
