import 'dart:async';

import 'package:dosecerta/avisos_locais/aviso_local.dart';
import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/medicacao/lista_tomas.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/pessoa_ref_dto.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_tomas.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  // O relógio do AppFichas está a 09/11/2026 às 12:00: o período por omissão é de 08/11 a 14/11.
  late TomasRepositorioFalso tomas;
  late AgendadorAvisosFalso agendador;
  late AppFichas app;

  AppFichas criar(String inicio) {
    return AppFichas(
      inicio: inicio,
      outras: [
        tomasRepositorioProvider.overrideWithValue(tomas),
        agendadorAvisosProvider.overrideWithValue(agendador),
        permissaoNotificacoesProvider.overrideWithValue(
          PermissaoNotificacoesFalsa(),
        ),
        calendarioRepositorioProvider.overrideWithValue(
          CalendarioRepositorioFalso(),
        ),
        tomasSincronizadasStoreProvider.overrideWithValue(
          TomasSincronizadasStoreEmMemoria(),
        ),
      ],
    );
  }

  setUp(() {
    tomas = TomasRepositorioFalso()
      ..tomas = [
        tomaExemplo(id: 't-pendente', previstaEm: '2026-11-09T20:00:00+00:00'),
        tomaExemplo(
          id: 't-atraso',
          medicamento: 'Insulina',
          dose: '10 UI',
          grau: GrauImportancia.grave,
          previstaEm: '2026-11-08T08:00:00+00:00',
          estado: EstadoToma.confirmadaComAtraso,
          confirmadaPor: const PessoaRefDto(id: 'u-2', nome: 'Rui Fictício'),
          confirmadaEm: '2026-11-08T09:15:00+00:00',
        ),
        tomaExemplo(
          id: 't-nao',
          medicamento: 'Amoxicilina',
          dose: '1 g',
          grau: GrauImportancia.leve,
          previstaEm: '2026-11-08T20:00:00+00:00',
          estado: EstadoToma.naoConfirmada,
        ),
      ];
    agendador = AgendadorAvisosFalso();
    app = criar(AppRouter.calendarioTomas('f-1'));
  });

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
  }

  Finder cartao(String medicamento) => find.ancestor(
    of: find.textContaining(medicamento),
    matching: find.byType(Card),
  );

  group('E15 «Calendário de tomas» (I069)', () {
    testWidgets(
      '?vista=calendario abre o separador, de ontem a hoje + 5 dias, agrupado por dia (RF-063)',
      (tester) async {
        await abrir(tester);

        expect(find.text('Calendário de tomas'), findsOneWidget);
        expect(find.text('Tomas de 08/11/2026 a 14/11/2026'), findsOneWidget);
        expect(tomas.periodos, ['2026-11-08..2026-11-14']);
        expect(find.text('08/11/2026'), findsOneWidget);
        expect(find.text('09/11/2026'), findsOneWidget);
        // Por ordem da hora prevista, com a hora de Lisboa tal como vem da API.
        final insulina = tester.getTopLeft(find.text('08:00  Insulina')).dy;
        final amoxicilina = tester
            .getTopLeft(find.text('20:00  Amoxicilina'))
            .dy;
        final metformina = tester.getTopLeft(find.text('20:00  Metformina')).dy;
        expect(insulina < amoxicilina && amoxicilina < metformina, isTrue);
        // Os tratamentos e «Registar tratamento» ficam no outro separador.
        expect(find.text('Registar tratamento'), findsNothing);
      },
    );

    testWidgets(
      'cada toma mostra a dose, o grau, o estado com texto e ícone e quem confirmou (RF-065, RNF-027)',
      (tester) async {
        await abrir(tester);

        final atraso = cartao('Insulina');
        expect(
          find.descendant(of: atraso, matching: find.text('10 UI · Grave')),
          findsOneWidget,
        );
        expect(
          find.descendant(
            of: atraso,
            matching: find.text('Confirmada com atraso'),
          ),
          findsOneWidget,
        );
        expect(
          find.descendant(of: atraso, matching: find.byIcon(Icons.history)),
          findsOneWidget,
        );
        expect(
          find.descendant(
            of: atraso,
            matching: find.text(
              'Confirmada por Rui Fictício em 08/11/2026 às 09:15',
            ),
          ),
          findsOneWidget,
        );
        expect(
          find.descendant(
            of: cartao('Amoxicilina'),
            matching: find.byIcon(Icons.cancel),
          ),
          findsOneWidget,
        );
        expect(
          find.descendant(
            of: cartao('Metformina'),
            matching: find.byIcon(Icons.schedule),
          ),
          findsOneWidget,
        );
        // Só as tomas pendentes têm «Confirmar toma».
        expect(find.text('Confirmar toma'), findsOneWidget);
      },
    );

    testWidgets(
      'confirmar com um toque mostra o novo estado sem ler a lista de novo e cancela o aviso (RF-064, RNF-011)',
      (tester) async {
        await agendador.agendar(
          AvisoLocal.daToma(tomaSincronizadaExemplo(tomaId: 't-pendente')),
        );
        await abrir(tester);

        await tocar(tester, find.text('Confirmar toma'));
        await tester.pumpAndSettle();

        expect(tomas.confirmadas, ['t-pendente']);
        expect(tomas.periodos, hasLength(1));
        expect(find.text('Toma confirmada.'), findsOneWidget);
        expect(find.text('Confirmar toma'), findsNothing);
        expect(
          find.descendant(
            of: cartao('Metformina'),
            matching: find.text(
              'Confirmada por Ana Fictícia em 09/11/2026 às 12:00',
            ),
          ),
          findsOneWidget,
        );
        expect(agendador.agendados, isEmpty);
      },
    );

    testWidgets(
      'enquanto a confirmação está em curso, o botão fica desativado',
      (tester) async {
        final resposta = Completer<void>();
        tomas.espera = resposta.future;
        await abrir(tester);

        await tocar(tester, find.text('Confirmar toma'));
        await tester.pump();

        final botao = tester.widget<FilledButton>(
          find.ancestor(
            of: find.text('Confirmar toma'),
            matching: find.byType(FilledButton),
          ),
        );
        expect(botao.onPressed, isNull);

        resposta.complete();
        await tester.pumpAndSettle();
        expect(find.text('Toma confirmada.'), findsOneWidget);
        expect(tomas.confirmadas, ['t-pendente']);
      },
    );

    testWidgets('com atraso, a mensagem diz que foi com atraso (RF-067)', (
      tester,
    ) async {
      tomas.estadoConfirmado = EstadoToma.confirmadaComAtraso;
      await abrir(tester);

      await tocar(tester, find.text('Confirmar toma'));
      await tester.pumpAndSettle();

      expect(find.text('Toma confirmada com atraso.'), findsOneWidget);
    });

    final recusas = <String, (ProblemaApi, String)>{
      'antes do dia previsto': (
        const ProblemaApi(
          codigo: 'TOMA_FORA_DA_JANELA',
          mensagem: 'x',
          estadoHttp: 409,
        ),
        'A toma só pode ser confirmada a partir das 00:00 do dia previsto.',
      ),
      'não confirmada (RF-068)': (
        const ProblemaApi(
          codigo: 'TOMA_NAO_CONFIRMAVEL',
          mensagem: 'x',
          estadoHttp: 409,
        ),
        'A toma ficou não confirmada e já não pode ser confirmada.',
      ),
      'já confirmada por outro Cuidador (RF-105)': (
        const ProblemaApi(
          codigo: 'TOMA_JA_CONFIRMADA',
          mensagem: 'A toma já foi confirmada por Rui Fictício em 09/11/2026 às 11:58.',
          estadoHttp: 409,
        ),
        'A toma já foi confirmada por Rui Fictício em 09/11/2026 às 11:58.',
      ),
      'papel sem permissão': (
        const ProblemaApi(
          codigo: 'ACESSO_PROIBIDO',
          mensagem: 'x',
          estadoHttp: 403,
        ),
        MensagensErro.catalogo['ACESSO_PROIBIDO']!,
      ),
    };
    for (final recusa in recusas.entries) {
      testWidgets(
        'recusa ${recusa.key}: mostra a mensagem e lê a lista de novo, com o estado atual',
        (tester) async {
          await abrir(tester);
          tomas.erroConfirmar = recusa.value.$1;

          await tocar(tester, find.text('Confirmar toma'));
          await tester.pumpAndSettle();

          expect(find.text(recusa.value.$2), findsOneWidget);
          expect(tomas.periodos, hasLength(2));
          expect(find.text('Toma confirmada.'), findsNothing);
        },
      );
    }

    testWidgets('sem ligação, mostra a mensagem e mantém a lista (RNF-033)', (
      tester,
    ) async {
      await abrir(tester);
      tomas.erroConfirmar = const SemLigacaoErro();

      await tocar(tester, find.text('Confirmar toma'));
      await tester.pumpAndSettle();

      expect(find.text(const SemLigacaoErro().mensagem), findsOneWidget);
      expect(tomas.periodos, hasLength(1));
      expect(find.text('Confirmar toma'), findsOneWidget);
    });

    testWidgets('«Período anterior» e «Período seguinte» andam 7 dias', (
      tester,
    ) async {
      await abrir(tester);

      await tester.tap(find.text('Período seguinte'));
      await tester.pumpAndSettle();
      expect(find.text('Tomas de 15/11/2026 a 21/11/2026'), findsOneWidget);

      await tester.tap(find.text('Período anterior'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Período anterior'));
      await tester.pumpAndSettle();

      expect(find.text('Tomas de 01/11/2026 a 07/11/2026'), findsOneWidget);
      expect(tomas.periodos, [
        '2026-11-08..2026-11-14',
        '2026-11-15..2026-11-21',
        '2026-11-08..2026-11-14',
        '2026-11-01..2026-11-07',
      ]);
    });

    testWidgets('sem tomas no período', (tester) async {
      tomas.tomas = [];
      await abrir(tester);

      expect(
        find.text('Não há tomas previstas neste período.'),
        findsOneWidget,
      );
    });

    testWidgets('um erro na leitura deixa tentar de novo', (tester) async {
      tomas.erroListar = const SemLigacaoErro();
      await abrir(tester);
      expect(find.text(const SemLigacaoErro().mensagem), findsOneWidget);

      tomas.erroListar = null;
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      expect(find.text('20:00  Metformina'), findsOneWidget);
    });

    testWidgets('o Familiar consulta sem opção de confirmar (RF-048)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await abrir(tester);

      expect(find.text(ListaTomas.textoFamiliar), findsOneWidget);
      expect(find.text('20:00  Metformina'), findsOneWidget);
      expect(find.text('Confirmar toma'), findsNothing);
    });

    testWidgets(
      'numa ficha arquivada ainda se confirmam as tomas por confirmar (RF-120)',
      (tester) async {
        app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
        await abrir(tester);

        expect(find.text(ListaTomas.textoArquivada), findsOneWidget);
        expect(
          find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
          findsNothing,
        );
        expect(find.text('Confirmar toma'), findsOneWidget);
      },
    );

    testWidgets('os separadores mudam a vista pela rota', (tester) async {
      app = criar(AppRouter.medicacaoFicha('f-1'));
      await abrir(tester);
      expect(find.text('Registar tratamento'), findsOneWidget);
      expect(tomas.periodos, isEmpty);

      await tester.tap(find.text('Calendário de tomas'));
      await tester.pumpAndSettle();
      expect(find.text('Tomas de 08/11/2026 a 14/11/2026'), findsOneWidget);

      await tester.tap(find.text('Tratamentos'));
      await tester.pumpAndSettle();
      expect(find.text('Registar tratamento'), findsOneWidget);
    });
  });

  group('Acessibilidade do calendário (RNF-025, RNF-026)', () {
    for (final papel in [Papel.cuidador, Papel.familiar]) {
      for (final escala in [1.0, 1.5]) {
        testWidgets(
          '${papel.name}, letra a ${(escala * 100).round()}%: sem erros de layout e alvos de 48 x 48 dp',
          (tester) async {
            tester.view
              ..physicalSize = const Size(360 * 3, 740 * 3)
              ..devicePixelRatio = 3;
            addTearDown(tester.view.reset);
            app.fichas.juntar(
              fichaExemplo(papel: papel, principal: papel == Papel.cuidador),
            );
            final semantica = tester.ensureSemantics();

            await tester.pumpWidget(app.widget(escalaLetra: escala));
            await tester.pumpAndSettle();

            expect(tester.takeException(), isNull);
            await expectLater(
              tester,
              meetsGuideline(androidTapTargetGuideline),
            );
            semantica.dispose();
          },
        );
      }
    }
  });
}
