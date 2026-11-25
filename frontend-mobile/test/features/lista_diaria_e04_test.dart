import 'dart:async';

import 'package:dosecerta/avisos_locais/aviso_local.dart';
import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/lista_diaria/lista_diaria_page.dart';
import 'package:dosecerta/features/medicacao/lista_tomas_controller.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_lista_diaria.dart';
import '../suporte/falsos_tomas.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  // O relógio do AppFichas está a 09/11/2026 às 12:00 (Lisboa, inverno: UTC).
  late ListaDiariaRepositorioFalso lista;
  late TomasRepositorioFalso tomas;
  late AgendadorAvisosFalso agendador;
  late CalendarioRepositorioFalso calendario;
  late AppFichas app;

  setUp(() {
    lista = ListaDiariaRepositorioFalso()
      ..itens = [
        tomaDiariaExemplo(
          id: 't-ontem',
          nomeFicha: 'João Fictício Lopes',
          titulo: 'Insulina 10 UI',
          previstaEm: '2026-11-08T23:30:00+00:00',
        ),
        tomaDiariaExemplo(id: 't-1'),
        compromissoDiarioExemplo(),
        tomaDiariaExemplo(
          id: 't-2',
          nomeFicha: 'João Fictício Lopes',
          titulo: 'Insulina 10 UI',
          previstaEm: '2026-11-09T11:00:00+00:00',
          estado: EstadoToma.confirmada,
          confirmadaPor: 'Rui Fictício',
          confirmadaEm: '2026-11-09T11:04:00+00:00',
        ),
        tomaDiariaExemplo(
          id: 't-3',
          titulo: 'Paracetamol 1 g',
          previstaEm: '2026-11-09T20:00:00+00:00',
        ),
      ];
    // A OP-33 do falso confirma as tomas desta lista.
    tomas = TomasRepositorioFalso()
      ..tomas = [
        tomaExemplo(id: 't-1', previstaEm: '2026-11-09T08:00:00+00:00'),
        tomaExemplo(id: 't-ontem', previstaEm: '2026-11-08T23:30:00+00:00'),
      ];
    agendador = AgendadorAvisosFalso();
    calendario = CalendarioRepositorioFalso();
    app = AppFichas(
      inicio: AppRouter.hoje,
      outras: [
        listaDiariaRepositorioProvider.overrideWithValue(lista),
        tomasRepositorioProvider.overrideWithValue(tomas),
        agendadorAvisosProvider.overrideWithValue(agendador),
        permissaoNotificacoesProvider.overrideWithValue(
          PermissaoNotificacoesFalsa(),
        ),
        calendarioRepositorioProvider.overrideWithValue(calendario),
        tomasSincronizadasStoreProvider.overrideWithValue(
          TomasSincronizadasStoreEmMemoria(),
        ),
      ],
    );
    app.fichas.juntar(fichaExemplo());
  });

  Future<void> abrir(WidgetTester tester) async {
    // Ecrã alto, para a lista inteira ficar construída.
    tester.view
      ..physicalSize = const Size(800, 2000)
      ..devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  /// Cartão do item com o texto [texto].
  Finder cartao(String texto) =>
      find.ancestor(of: find.text(texto), matching: find.byType(Card));

  /// Botão «Confirmar» dentro do cartão com o texto [texto].
  Finder confirmarEm(String texto) => find.descendant(
    of: cartao(texto),
    matching: find.widgetWithText(FilledButton, 'Confirmar'),
  );

  testWidgets(
    'mostra num só pedido as tomas e os compromissos de hoje, por hora e com o utente (RF-100, RF-101, RNF-010)',
    (tester) async {
      await abrir(tester);

      expect(find.text('Hoje, 09/11/2026'), findsOneWidget);
      expect(lista.datas, [null]);
      expect(
        find.text('08/11/2026 23:30 | João Fictício Lopes'),
        findsOneWidget,
      );
      expect(find.text('08:00 | Maria Fictícia Santos'), findsOneWidget);
      expect(find.text('10:30 | Maria Fictícia Santos'), findsOneWidget);
      expect(
        tester.getTopLeft(find.text('08:00 | Maria Fictícia Santos')).dy,
        lessThan(
          tester.getTopLeft(find.text('10:30 | Maria Fictícia Santos')).dy,
        ),
      );
    },
  );

  testWidgets(
    'os estados têm texto e ícone; os compromissos não têm confirmação (RNF-027, RF-087, RF-104)',
    (tester) async {
      await abrir(tester);

      expect(confirmarEm('Consulta de rotina'), findsNothing);
      expect(
        find.descendant(
          of: cartao('Consulta de rotina'),
          matching: find.text(ListaDiariaPage.compromisso),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: cartao('Consulta de rotina'),
          matching: find.byIcon(Icons.event_outlined),
        ),
        findsOneWidget,
      );
      expect(
        find.text('Confirmada por Rui Fictício em 09/11/2026 às 11:04'),
        findsOneWidget,
      );
      expect(find.byIcon(Icons.check_circle), findsOneWidget);
      expect(find.byIcon(Icons.schedule), findsWidgets);
      expect(find.widgetWithText(FilledButton, 'Confirmar'), findsNWidgets(3));
    },
  );

  testWidgets(
    'confirma com um toque, sem abrir a ficha nem ler a lista de novo, e cancela o aviso (RF-102, RNF-011, RNF-021)',
    (tester) async {
      await abrir(tester);

      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pumpAndSettle();

      expect(tomas.confirmadas, ['t-1']);
      expect(find.text('Toma confirmada.'), findsOneWidget);
      expect(
        find.text('Confirmada por Ana Fictícia em 09/11/2026 às 12:00'),
        findsOneWidget,
      );
      expect(confirmarEm('08:00 | Maria Fictícia Santos'), findsNothing);
      expect(lista.datas, [null]);
      expect(app.local, AppRouter.hoje);
      expect(agendador.cancelados, [AvisoLocal.idDaToma('t-1')]);
    },
  );

  testWidgets('a toma de ontem fica confirmada com atraso (RF-067)', (
    tester,
  ) async {
    tomas.estadoConfirmado = EstadoToma.confirmadaComAtraso;
    await abrir(tester);

    await tester.tap(confirmarEm('08/11/2026 23:30 | João Fictício Lopes'));
    await tester.pumpAndSettle();

    expect(find.text('Toma confirmada com atraso.'), findsOneWidget);
    expect(find.text('Confirmada com atraso'), findsOneWidget);
  });

  testWidgets(
    'a toma já confirmada por outro Cuidador mostra a mensagem do servidor e o estado atual (RF-105)',
    (tester) async {
      tomas.erroConfirmar = const ProblemaApi(
        codigo: 'TOMA_JA_CONFIRMADA',
        mensagem:
            'A toma já foi confirmada por Rui Fictício em 09/11/2026 às 08:05.',
        estadoHttp: 409,
      );
      await abrir(tester);
      lista.itens[1] = tomaDiariaExemplo(
        id: 't-1',
        estado: EstadoToma.confirmada,
        confirmadaPor: 'Rui Fictício',
        confirmadaEm: '2026-11-09T08:05:00+00:00',
      );

      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pumpAndSettle();

      expect(
        find.text(
          'A toma já foi confirmada por Rui Fictício em 09/11/2026 às 08:05.',
        ),
        findsOneWidget,
      );
      expect(
        find.text('Confirmada por Rui Fictício em 09/11/2026 às 08:05'),
        findsOneWidget,
      );
      expect(lista.datas, [null, null]);
    },
  );

  testWidgets('«Amanhã» pede a data seguinte no calendário de Lisboa', (
    tester,
  ) async {
    await abrir(tester);
    lista.itens = [];

    await tester.tap(find.text('Amanhã'));
    await tester.pumpAndSettle();

    expect(find.text('Amanhã, 10/11/2026'), findsOneWidget);
    expect(lista.datas, [null, '2026-11-10']);
    expect(find.text(ListaDiariaPage.semTarefas), findsOneWidget);
  });

  testWidgets(
    'puxar para baixo lê a lista de novo e sincroniza os avisos locais (RF-061, RF-104)',
    (tester) async {
      await abrir(tester);
      final pedidosAntes = calendario.pedidos;

      await tester.fling(find.byType(ListView), const Offset(0, 800), 1000);
      await tester.pumpAndSettle();

      expect(lista.datas, [null, null]);
      expect(calendario.pedidos, pedidosAntes + 1);
    },
  );

  testWidgets('sem ligação mostra a mensagem e permite tentar novamente', (
    tester,
  ) async {
    lista.erro = const SemLigacaoErro();
    await abrir(tester);

    expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);

    lista.erro = null;
    await tester.tap(find.text('Tentar novamente'));
    await tester.pumpAndSettle();

    expect(find.text('08:00 | Maria Fictícia Santos'), findsOneWidget);
  });

  testWidgets('o botão do topo abre o E26 e tocar num item abre a ficha', (
    tester,
  ) async {
    await abrir(tester);

    await tester.tap(find.byTooltip('Avisos de toma'));
    await tester.pumpAndSettle();
    expect(app.local, AppRouter.avisosLocais);
    expect(find.text('Sincronizar agora'), findsOneWidget);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Consulta de rotina'));
    await tester.pumpAndSettle();
    expect(app.local, AppRouter.ficha('f-1'));
  });

  testWidgets('«Ontem» pede a data anterior no calendário de Lisboa', (
    tester,
  ) async {
    await abrir(tester);
    lista.itens = [];

    await tester.tap(find.text('Ontem'));
    await tester.pumpAndSettle();

    expect(find.text('Ontem, 08/11/2026'), findsOneWidget);
    expect(lista.datas, [null, '2026-11-08']);
    expect(find.text(ListaDiariaPage.semTarefas), findsOneWidget);
  });

  testWidgets(
    'o «Confirmar» diz a toma e o utente ao leitor de ecrã (RNF-025)',
    (tester) async {
      await abrir(tester);

      expect(
        find.bySemanticsLabel(
          'Confirmar Metformina 500 mg de Maria Fictícia Santos',
        ),
        findsOneWidget,
      );
    },
  );

  for (final (codigo, estadoHttp) in [
    ('ACESSO_PROIBIDO', 403),
    ('TOMA_NAO_ENCONTRADA', 404),
  ]) {
    testWidgets(
      'a recusa $estadoHttp ao confirmar mostra a mensagem e lê a lista de novo',
      (tester) async {
        final recusa = ProblemaApi(
          codigo: codigo,
          mensagem: 'Recusa $estadoHttp.',
          estadoHttp: estadoHttp,
        );
        tomas.erroConfirmar = recusa;
        await abrir(tester);

        await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
        await tester.pumpAndSettle();

        expect(
          find.text(ListaTomasController.mensagemRecusa(recusa)),
          findsOneWidget,
        );
        expect(lista.datas, [null, null]);
      },
    );
  }

  testWidgets(
    'sem ligação ao confirmar mostra a mensagem e a toma continua por confirmar',
    (tester) async {
      tomas.erroConfirmar = const SemLigacaoErro();
      await abrir(tester);

      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pumpAndSettle();

      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(confirmarEm('08:00 | Maria Fictícia Santos'), findsOneWidget);
      expect(lista.datas, [null]);
    },
  );

  testWidgets('puxar para baixo depois da meia-noite passa para o novo dia', (
    tester,
  ) async {
    await abrir(tester);
    app.relogio.instante = DateTime(2026, 11, 10, 8);

    await tester.fling(find.byType(ListView), const Offset(0, 800), 1000);
    await tester.pumpAndSettle();

    expect(find.text('Hoje, 10/11/2026'), findsOneWidget);
  });

  testWidgets(
    'ao voltar à aplicação lê a lista de novo, com o dia atual (RF-104)',
    (tester) async {
      await abrir(tester);
      app.relogio.instante = DateTime(2026, 11, 10, 8);

      for (final estado in [
        AppLifecycleState.inactive,
        AppLifecycleState.hidden,
        AppLifecycleState.paused,
        AppLifecycleState.hidden,
        AppLifecycleState.inactive,
        AppLifecycleState.resumed,
      ]) {
        tester.binding.handleAppLifecycleStateChanged(estado);
      }
      await tester.pumpAndSettle();

      expect(lista.datas, [null, null]);
      expect(find.text('Hoje, 10/11/2026'), findsOneWidget);
    },
  );

  testWidgets(
    'uma confirmação que termina durante a leitura de outro dia não apaga o estado da leitura',
    (tester) async {
      final respostaConfirmar = Completer<void>();
      tomas.espera = respostaConfirmar.future;
      await abrir(tester);

      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pump();
      final leitura = Completer<void>();
      lista
        ..espera = leitura
        ..erro = const SemLigacaoErro();
      await tester.tap(find.text('Amanhã'));
      await tester.pump();

      respostaConfirmar.complete();
      await tester.pump();
      expect(find.byType(LinearProgressIndicator), findsOneWidget);
      expect(find.text(ListaDiariaPage.semTarefas), findsNothing);

      leitura.complete();
      await tester.pumpAndSettle();
      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(find.text('Tentar novamente'), findsOneWidget);
    },
  );

  testWidgets(
    'mudar de dia e voltar não volta a ativar uma confirmação em curso',
    (tester) async {
      final respostaConfirmar = Completer<void>();
      tomas.espera = respostaConfirmar.future;
      await abrir(tester);

      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pump();
      await tester.tap(find.text('Amanhã'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Hoje'));
      await tester.pumpAndSettle();

      final botao = tester.widget<FilledButton>(
        confirmarEm('08:00 | Maria Fictícia Santos'),
      );
      expect(botao.onPressed, isNull);
      respostaConfirmar.complete();
      await tester.pumpAndSettle();
    },
  );

  testWidgets(
    'uma leitura antiga que chega depois da confirmação não a desfaz',
    (tester) async {
      await abrir(tester);
      final leitura = Completer<void>();
      lista.espera = leitura;

      await tester.fling(find.byType(ListView), const Offset(0, 800), 1000);
      // O RefreshIndicator só lança a leitura depois da animação.
      for (var i = 0; i < 50 && lista.datas.length < 2; i++) {
        await tester.pump(const Duration(milliseconds: 50));
      }
      expect(lista.datas, [null, null]);
      await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      leitura.complete();
      await tester.pumpAndSettle();

      expect(confirmarEm('08:00 | Maria Fictícia Santos'), findsNothing);
      expect(
        find.descendant(
          of: cartao('08:00 | Maria Fictícia Santos'),
          matching: find.textContaining('Confirmada por'),
        ),
        findsOneWidget,
      );
    },
  );

  testWidgets('puxar para baixo apaga a mensagem da última confirmação', (
    tester,
  ) async {
    tomas.erroConfirmar = const SemLigacaoErro();
    await abrir(tester);
    await tester.tap(confirmarEm('08:00 | Maria Fictícia Santos'));
    await tester.pumpAndSettle();
    expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);

    await tester.fling(find.byType(ListView), const Offset(0, 800), 1000);
    await tester.pumpAndSettle();

    expect(find.text(SemLigacaoErro.mensagemPadrao), findsNothing);
  });
}
