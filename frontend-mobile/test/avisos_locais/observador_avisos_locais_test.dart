import 'package:dosecerta/avisos_locais/observador_avisos_locais.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/falsos.dart';
import '../suporte/falsos_tomas.dart';

void main() {
  final agora = DateTime.utc(2026, 11, 9, 12);
  late PermissaoNotificacoesFalsa permissao;

  setUp(() => permissao = PermissaoNotificacoesFalsa());

  Widget app({bool comSessao = true, double escalaLetra = 1}) {
    return ProviderScope(
      overrides: [
        relogioProvider.overrideWithValue(RelogioFixo(agora)),
        sessaoInicialProvider.overrideWithValue(
          comSessao
              ? SessaoGuardada(
                  token: 't',
                  expiraEm: agora.add(const Duration(hours: 8)),
                  utilizadorId: 'u-eu',
                )
              : null,
        ),
        sessaoStoreProvider.overrideWithValue(SessaoStoreEmMemoria()),
        calendarioRepositorioProvider.overrideWithValue(
          CalendarioRepositorioFalso(),
        ),
        tomasSincronizadasStoreProvider.overrideWithValue(
          TomasSincronizadasStoreEmMemoria(),
        ),
        agendadorAvisosProvider.overrideWithValue(AgendadorAvisosFalso()),
        permissaoNotificacoesProvider.overrideWithValue(permissao),
      ],
      child: MaterialApp(
        theme: TemaDoseCerta.claro(),
        builder: (context, filho) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(escalaLetra)),
          child: ObservadorAvisosLocais(child: filho!),
        ),
        home: Scaffold(
          appBar: AppBar(title: const Text('Hoje')),
          body: const Text('Conteúdo'),
        ),
      ),
    );
  }

  testWidgets(
    'sem notificações autorizadas, o Cuidador é informado ao abrir a aplicação (RF-062)',
    (tester) async {
      permissao.notificacoes = false;

      await tester.pumpWidget(app());
      await tester.pumpAndSettle();

      expect(find.text(textoNotificacoesDesativadas), findsOneWidget);
      expect(find.text('Conteúdo'), findsOneWidget);
      expect(permissao.pedidos, 0);
    },
  );

  testWidgets(
    '«Ativar avisos» pede a autorização só nesse toque e o aviso desaparece',
    (tester) async {
      permissao.notificacoes = false;
      await tester.pumpWidget(app());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Ativar avisos'));
      await tester.pumpAndSettle();

      expect(permissao.pedidos, 1);
      expect(find.text(textoNotificacoesDesativadas), findsNothing);
    },
  );

  testWidgets('o aviso pode ser fechado', (tester) async {
    permissao.notificacoes = false;
    final semantica = tester.ensureSemantics();
    await tester.pumpWidget(app());
    await tester.pumpAndSettle();
    expect(find.bySemanticsLabel('Fechar o aviso'), findsOneWidget);
    semantica.dispose();

    await tester.tap(find.byIcon(Icons.close));
    await tester.pumpAndSettle();

    expect(find.text(textoNotificacoesDesativadas), findsNothing);
    expect(permissao.pedidos, 0);
  });

  testWidgets(
    'com notificações, mas sem alarmes exatos, avisa do atraso possível (RNF-041)',
    (tester) async {
      permissao.alarmesExatos = false;

      await tester.pumpWidget(app());
      await tester.pumpAndSettle();

      expect(find.text(textoAlarmesExatos), findsOneWidget);
      expect(find.text(textoNotificacoesDesativadas), findsNothing);
    },
  );

  testWidgets('com tudo autorizado, não há aviso', (tester) async {
    await tester.pumpWidget(app());
    await tester.pumpAndSettle();

    expect(find.text('Ativar avisos'), findsNothing);
  });

  testWidgets('sem sessão (E01 e E02), não há aviso', (tester) async {
    permissao.notificacoes = false;

    await tester.pumpWidget(app(comSessao: false));
    await tester.pumpAndSettle();

    expect(find.text(textoNotificacoesDesativadas), findsNothing);
  });

  testWidgets(
    'a 150% de letra, sem erros de layout e com áreas de toque de 48 dp (RNF-025, RNF-026)',
    (tester) async {
      permissao.notificacoes = false;
      final semantica = tester.ensureSemantics();

      await tester.pumpWidget(app(escalaLetra: 1.5));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    },
  );
}
