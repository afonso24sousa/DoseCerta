import 'package:dosecerta/features/navegacao/estrutura_navegacao.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

void main() {
  const rotulos = ['Hoje', 'Fichas', 'Agenda', 'Notificações', 'Conta'];

  Widget app({required double escalaLetra}) {
    final router = GoRouter(
      initialLocation: '/notificacoes',
      routes: [
        StatefulShellRoute.indexedStack(
          builder: (context, estado, shell) => EstruturaNavegacao(shell: shell),
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
                    builder: (context, estado) => const SizedBox.shrink(),
                  ),
                ],
              ),
          ],
        ),
      ],
    );
    return MaterialApp.router(
      theme: TemaDoseCerta.claro(),
      routerConfig: router,
      builder: (context, filho) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(escalaLetra)),
        child: filho!,
      ),
    );
  }

  group('Barra inferior (RNF-025, RNF-026)', () {
    for (final escala in [1.0, 1.2, 1.5]) {
      testWidgets(
        'a ${(escala * 100).round()}%: rótulo só na entrada selecionada, a acompanhar a letra, sem erros de layout',
        (tester) async {
          await tester.pumpWidget(app(escalaLetra: escala));
          await tester.pumpAndSettle();
          expect(tester.takeException(), isNull);

          final barra = tester.widget<NavigationBar>(
            find.byType(NavigationBar),
          );
          expect(
            barra.labelBehavior,
            NavigationDestinationLabelBehavior.onlyShowSelected,
          );

          // O rótulo acompanha a letra do sistema até ao limite do próprio NavigationBar do Material
          // (1,3; `_kMaxLabelTextScaleFactor`); a aplicação não acrescenta nenhum limite.
          final texto = tester.widget<Text>(find.text('Notificações'));
          final contexto = tester.element(find.text('Notificações'));
          expect(
            texto.textScaler ?? MediaQuery.textScalerOf(contexto),
            TextScaler.linear(escala < 1.3 ? escala : 1.3),
          );
        },
      );
    }

    testWidgets(
      'o rótulo de cada entrada fica disponível para o leitor de ecrã',
      (tester) async {
        final semantica = tester.ensureSemantics();
        await tester.pumpWidget(app(escalaLetra: 1.5));
        await tester.pumpAndSettle();
        for (final rotulo in rotulos) {
          expect(find.bySemanticsLabel(RegExp(rotulo)), findsWidgets);
        }
        semantica.dispose();
      },
    );

    testWidgets('alvos de toque de pelo menos 48 x 48 dp', (tester) async {
      final semantica = tester.ensureSemantics();
      await tester.pumpWidget(app(escalaLetra: 1.5));
      await tester.pumpAndSettle();
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });
  });
}
