import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/app_router.dart';
import 'core/inicializador_app.dart';
import 'shared/tema_dosecerta.dart';

/// Ponto de entrada da aplicação DoseCerta.
Future<void> main() async {
  final substituicoes = await const InicializadorApp().iniciar();
  runApp(ProviderScope(overrides: substituicoes, child: const DoseCertaApp()));
}

/// Aplicação DoseCerta: tema, língua (português europeu, RNF-020) e rotas.
class DoseCertaApp extends ConsumerWidget {
  /// Cria a aplicação.
  const DoseCertaApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'DoseCerta',
      theme: TemaDoseCerta.claro(),
      locale: const Locale('pt', 'PT'),
      supportedLocales: const [Locale('pt', 'PT')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      routerConfig: ref.watch(appRouterProvider),
      debugShowCheckedModeBanner: false,
    );
  }
}
