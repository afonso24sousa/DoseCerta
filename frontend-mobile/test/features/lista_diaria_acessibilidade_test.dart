import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_lista_diaria.dart';
import '../suporte/falsos_tomas.dart';

/// Ecrãs da F010 (E04) e do E26 num telemóvel de 360 x 740 dp, com a letra do sistema a 100% e a 150%, nos estados
/// com mais texto: sem erros de layout (RNF-026) e com alvos de toque de pelo menos 48 x 48 dp (RNF-025).
void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  void telemovel(WidgetTester tester) {
    tester.view
      ..physicalSize = const Size(360 * 3, 740 * 3)
      ..devicePixelRatio = 3;
    addTearDown(tester.view.reset);
  }

  AppFichas criar(String inicio, {bool semAutorizacoes = false}) {
    final lista = ListaDiariaRepositorioFalso()
      ..itens = [
        tomaDiariaExemplo(
          id: 't-ontem',
          nomeFicha: 'Maria Fictícia dos Santos Pereira',
          titulo: 'Insulina de ação rápida 10 UI',
          previstaEm: '2026-11-08T23:30:00+00:00',
        ),
        compromissoDiarioExemplo(
          titulo: 'Consulta de rotina no centro de saúde',
        ),
        tomaDiariaExemplo(
          id: 't-2',
          estado: EstadoToma.confirmadaComAtraso,
          confirmadaPor: 'Rui Fictício dos Santos',
          confirmadaEm: '2026-11-09T11:04:00+00:00',
        ),
      ];
    final permissao = PermissaoNotificacoesFalsa();
    if (semAutorizacoes) {
      permissao.notificacoes = false;
    }
    return AppFichas(
      inicio: inicio,
      outras: [
        listaDiariaRepositorioProvider.overrideWithValue(lista),
        tomasRepositorioProvider.overrideWithValue(TomasRepositorioFalso()),
        agendadorAvisosProvider.overrideWithValue(AgendadorAvisosFalso()),
        permissaoNotificacoesProvider.overrideWithValue(permissao),
        calendarioRepositorioProvider.overrideWithValue(
          CalendarioRepositorioFalso()..tomas = [tomaSincronizadaExemplo()],
        ),
        tomasSincronizadasStoreProvider.overrideWithValue(
          TomasSincronizadasStoreEmMemoria(),
        ),
      ],
    );
  }

  final cenarios = <String, AppFichas Function()>{
    'E04 com tomas e compromissos': () => criar(AppRouter.hoje),
    'E26 com tudo autorizado': () => criar(AppRouter.avisosLocais),
    'E26 sem notificações': () =>
        criar(AppRouter.avisosLocais, semAutorizacoes: true),
  };

  for (final cenario in cenarios.entries) {
    for (final escala in [1.0, 1.5]) {
      testWidgets(
        '${cenario.key}, letra a ${(escala * 100).round()}%: sem erros de layout e alvos de 48 x 48 dp',
        (tester) async {
          telemovel(tester);
          final semantica = tester.ensureSemantics();
          await tester.pumpWidget(cenario.value().widget(escalaLetra: escala));
          await tester.pumpAndSettle();
          expect(tester.takeException(), isNull);
          await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
          semantica.dispose();
        },
      );
    }
  }
}
