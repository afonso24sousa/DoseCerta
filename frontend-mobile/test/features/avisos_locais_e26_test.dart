import 'package:dosecerta/avisos_locais/avisos_locais_controller.dart';
import 'package:dosecerta/avisos_locais/tomas_sincronizadas_store.dart';
import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/lista_diaria/avisos_locais_page.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_lista_diaria.dart';
import '../suporte/falsos_tomas.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  // O relógio do AppFichas está a 09/11/2026 às 12:00 (Lisboa, inverno: UTC).
  late CalendarioRepositorioFalso calendario;
  late PermissaoNotificacoesFalsa permissao;
  late TomasSincronizadasStoreEmMemoria store;
  late AgendadorAvisosFalso agendador;
  late AppFichas app;

  setUp(() {
    calendario = CalendarioRepositorioFalso()
      ..tomas = [
        tomaSincronizadaExemplo(
          tomaId: 't-passada',
          previstaEm: '2026-11-09T08:00:00+00:00',
        ),
        tomaSincronizadaExemplo(),
        tomaSincronizadaExemplo(
          tomaId: 't-amanha',
          previstaEm: '2026-11-10T08:00:00+00:00',
        ),
      ];
    permissao = PermissaoNotificacoesFalsa();
    store = TomasSincronizadasStoreEmMemoria();
    agendador = AgendadorAvisosFalso();
    app = AppFichas(
      inicio: AppRouter.avisosLocais,
      outras: [
        listaDiariaRepositorioProvider.overrideWithValue(
          ListaDiariaRepositorioFalso(),
        ),
        agendadorAvisosProvider.overrideWithValue(agendador),
        permissaoNotificacoesProvider.overrideWithValue(permissao),
        calendarioRepositorioProvider.overrideWithValue(calendario),
        tomasSincronizadasStoreProvider.overrideWithValue(store),
      ],
    );
  });

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  testWidgets(
    'mostra a última sincronização, as tomas guardadas, os avisos agendados e as autorizações (RF-061, RF-062)',
    (tester) async {
      await abrir(tester);

      expect(
        find.text('Último calendário sincronizado: 09/11/2026 12:00'),
        findsOneWidget,
      );
      expect(
        find.text('Tomas sincronizadas neste dispositivo: 3'),
        findsOneWidget,
      );
      expect(
        find.text('Avisos agendados neste dispositivo: 2'),
        findsOneWidget,
      );
      expect(agendador.agendados, hasLength(2));
      expect(find.text('Notificações do sistema: autorizadas'), findsOneWidget);
      expect(find.text('Alarmes exatos: permitidos'), findsOneWidget);
      expect(
        find.text(AvisosLocaisPage.notificacoesNaoAutorizadas),
        findsNothing,
      );
    },
  );

  testWidgets('«Sincronizar agora» pede a OP-34 e atualiza o ecrã (RF-061)', (
    tester,
  ) async {
    await abrir(tester);
    final pedidos = calendario.pedidos;
    calendario.tomas = [tomaSincronizadaExemplo()];

    await tester.tap(find.text('Sincronizar agora'));
    await tester.pumpAndSettle();

    expect(calendario.pedidos, pedidos + 1);
    expect(find.text('Calendário sincronizado.'), findsOneWidget);
    expect(
      find.text('Tomas sincronizadas neste dispositivo: 1'),
      findsOneWidget,
    );
    expect(find.text('Avisos agendados neste dispositivo: 1'), findsOneWidget);
  });

  testWidgets(
    'sem ligação mantém as tomas da última sincronização e avisa (RF-061, RNF-033)',
    (tester) async {
      store.guardadas = TomasSincronizadas(
        utilizadorId: AppFichas.utilizadorId,
        tomas: [tomaSincronizadaExemplo()],
        ultimaSincronizacao: DateTime.utc(2026, 11, 9, 7, 55),
      );
      calendario.erro = const SemLigacaoErro();
      await abrir(tester);

      expect(find.text(AvisosLocaisPage.semLigacao), findsOneWidget);
      expect(
        find.text('Último calendário sincronizado: 09/11/2026 07:55'),
        findsOneWidget,
      );
      expect(
        find.text('Tomas sincronizadas neste dispositivo: 1'),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'sem notificações autorizadas avisa e pede a autorização com um toque (RF-062)',
    (tester) async {
      permissao.notificacoes = false;
      await abrir(tester);

      expect(
        find.text('Notificações do sistema: não autorizadas'),
        findsOneWidget,
      );
      expect(
        find.text(AvisosLocaisPage.notificacoesNaoAutorizadas),
        findsOneWidget,
      );
      expect(permissao.pedidos, 0);

      await tester.tap(find.text('Pedir autorização'));
      await tester.pumpAndSettle();

      expect(permissao.pedidos, 1);
      expect(find.text('Notificações do sistema: autorizadas'), findsOneWidget);
      expect(
        find.text(AvisosLocaisPage.notificacoesNaoAutorizadas),
        findsNothing,
      );
    },
  );

  testWidgets('sem alarmes exatos avisa e pede a autorização (RNF-041)', (
    tester,
  ) async {
    permissao.alarmesExatos = false;
    await abrir(tester);

    expect(find.text('Alarmes exatos: não permitidos'), findsOneWidget);
    expect(find.text(AvisosLocaisPage.alarmesNaoPermitidos), findsOneWidget);

    await tester.tap(find.text('Pedir autorização'));
    await tester.pumpAndSettle();

    expect(permissao.pedidosAlarmes, 1);
    expect(find.text('Alarmes exatos: permitidos'), findsOneWidget);
  });

  testWidgets('antes da primeira sincronização indica que ainda não houve', (
    tester,
  ) async {
    calendario.erro = Exception('recusado');
    await abrir(tester);

    expect(
      find.text('Último calendário sincronizado: ainda não sincronizado'),
      findsOneWidget,
    );
    expect(find.text(AvisosLocaisPage.falhou), findsOneWidget);
  });

  testWidgets(
    'uma toma confirmada sai logo das contagens, sem esperar pela sincronização seguinte',
    (tester) async {
      await abrir(tester);
      expect(
        find.text('Tomas sincronizadas neste dispositivo: 3'),
        findsOneWidget,
      );

      final contentor = ProviderScope.containerOf(
        tester.element(find.byType(AvisosLocaisPage)),
      );
      await contentor
          .read(avisosLocaisControllerProvider.notifier)
          .cancelarAviso('t-amanha');
      await tester.pumpAndSettle();

      expect(
        find.text('Tomas sincronizadas neste dispositivo: 2'),
        findsOneWidget,
      );
      expect(
        find.text('Avisos agendados neste dispositivo: 1'),
        findsOneWidget,
      );
      expect(store.guardadas!.ultimaSincronizacao, DateTime(2026, 11, 9, 12));
    },
  );
}
