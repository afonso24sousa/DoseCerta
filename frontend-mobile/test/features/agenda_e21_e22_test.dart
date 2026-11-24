import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/features/agenda/agenda_page.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import '../suporte/falsos.dart';
import '../suporte/falsos_agenda.dart';
import '../suporte/falsos_fichas.dart';

void main() {
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AgendaRepositorioFalso agenda;
  late CompromissosRepositorioFalso compromissos;
  late FichasRepositorioFalso fichas;
  late GoRouter router;

  setUp(() {
    agenda = AgendaRepositorioFalso();
    compromissos = CompromissosRepositorioFalso();
    fichas = FichasRepositorioFalso()..juntar(fichaExemplo());
  });

  Future<void> abrir(WidgetTester tester) async {
    // Ecrã alto: E22 cabe inteiro, sem deslocar a lista.
    await tester.binding.setSurfaceSize(const Size(800, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    router = GoRouter(
      initialLocation: AppRouter.agenda,
      routes: [
        ...AppRouter.rotasCompromissos(),
        GoRoute(
          path: AppRouter.agenda,
          builder: (context, estado) => const AgendaPage(),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          agendaRepositorioProvider.overrideWithValue(agenda),
          compromissosRepositorioProvider.overrideWithValue(compromissos),
          fichasRepositorioProvider.overrideWithValue(fichas),
          // Terça-feira, 24/11/2026.
          relogioProvider.overrideWithValue(
            RelogioFixo(DateTime(2026, 11, 24, 12)),
          ),
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
  }

  testWidgets('E21 mostra tomas e compromissos do dia com o utente', (
    tester,
  ) async {
    await abrir(tester);
    expect(agenda.pedidos, ['dia 2026-11-24']);
    expect(find.text('24/11/2026'), findsOneWidget);
    expect(find.text('Metformina 500 mg'), findsOneWidget);
    expect(find.text('Consulta de rotina'), findsOneWidget);
    expect(find.textContaining('08:00 · Toma · Pendente'), findsOneWidget);
    expect(find.textContaining('10:30 · Compromisso'), findsOneWidget);
    expect(find.textContaining('Maria Fictícia Santos'), findsNWidgets(2));
    // Nenhum evento oferece confirmação na agenda (RF-087, RF-088).
    expect(find.text('Confirmar'), findsNothing);
  });

  testWidgets('E21 muda de vista e de período', (tester) async {
    await abrir(tester);
    await tester.tap(find.text('Semana'));
    await tester.pumpAndSettle();
    expect(find.text('23/11/2026 a 29/11/2026'), findsOneWidget);
    await tester.tap(find.byTooltip('Período seguinte'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Mês'));
    await tester.pumpAndSettle();
    expect(find.text('dezembro de 2026'), findsOneWidget);
    expect(agenda.pedidos, [
      'dia 2026-11-24',
      'semana 2026-11-24',
      'semana 2026-12-01',
      'mes 2026-12-01',
    ]);
  });

  testWidgets('E21 sem eventos indica que não há eventos', (tester) async {
    agenda.itens = [];
    await abrir(tester);
    expect(find.text('Não há eventos neste período.'), findsOneWidget);
  });

  testWidgets('E21 sem fichas como Cuidador não oferece registar', (
    tester,
  ) async {
    fichas = FichasRepositorioFalso()
      ..juntar(fichaExemplo(papel: Papel.familiar, principal: false))
      ..juntar(fichaExemplo(id: 'f-2', estado: EstadoFicha.arquivada));
    await abrir(tester);
    expect(find.text('Registar compromisso'), findsNothing);
  });

  testWidgets('E22 valida e regista o compromisso com a hora de Lisboa', (
    tester,
  ) async {
    await abrir(tester);
    await tester.tap(find.text('Registar compromisso'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Registar compromisso'), findsOneWidget);
    await tester.tap(find.widgetWithText(FilledButton, 'Registar compromisso'));
    await tester.pumpAndSettle();
    expect(find.text('Local: é obrigatório.'), findsOneWidget);
    expect(compromissos.registado, isNull);

    await tester.enterText(
      find.widgetWithText(TextField, 'Data'),
      '25/11/2026',
    );
    await tester.enterText(find.widgetWithText(TextField, 'Hora'), '10:30');
    await tester.enterText(
      find.widgetWithText(TextField, 'Local'),
      'Centro de Saúde Fictício',
    );
    await tester.enterText(
      find.widgetWithText(TextField, 'Motivo'),
      'Consulta de rotina',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Registar compromisso'));
    await tester.pumpAndSettle();
    expect(compromissos.registado!.dataHora, '2026-11-25T10:30:00+00:00');
    expect(compromissos.registado!.antecedenciaAvisoMinutos, 1440);
    expect(find.widgetWithText(AppBar, 'Agenda'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
    // A agenda voltou a ser pedida.
    expect(agenda.pedidos.length, 2);
  });

  testWidgets('E22 mostra o erro da API junto do campo', (tester) async {
    compromissos.erroGuardar = const ProblemaApi(
      codigo: 'VALIDACAO_INVALIDA',
      mensagem: '',
      campos: [
        ErroCampo(campo: 'local', regra: 'Tem no máximo 150 caracteres.'),
      ],
    );
    await abrir(tester);
    await tester.tap(find.text('Consulta de rotina'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Editar compromisso'), findsOneWidget);
    expect(find.text('1 hora'), findsOneWidget);
    await tester.tap(find.text('Guardar'));
    await tester.pumpAndSettle();
    expect(find.text('Local: tem no máximo 150 caracteres.'), findsOneWidget);
    expect(find.widgetWithText(AppBar, 'Editar compromisso'), findsOneWidget);
  });

  testWidgets('E22 remove o compromisso depois de confirmar', (tester) async {
    await abrir(tester);
    await tester.tap(find.text('Consulta de rotina'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Remover compromisso'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Remover'));
    await tester.pumpAndSettle();
    expect(compromissos.removidos, ['c-1']);
    expect(find.widgetWithText(AppBar, 'Agenda'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
  });

  testWidgets('E22 para o Familiar só mostra os dados', (tester) async {
    fichas = FichasRepositorioFalso()
      ..juntar(fichaExemplo(papel: Papel.familiar, principal: false));
    await abrir(tester);
    await tester.tap(find.text('Consulta de rotina'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Compromisso'), findsOneWidget);
    expect(
      find.text('O Familiar só consulta os compromissos.'),
      findsOneWidget,
    );
    expect(find.text('Guardar'), findsNothing);
    expect(find.text('Remover compromisso'), findsNothing);
    final campo = tester.widget<TextField>(
      find.widgetWithText(TextField, 'Local'),
    );
    expect(campo.enabled, isFalse);
  });
}
