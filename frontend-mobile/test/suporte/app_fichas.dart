import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart';
import 'package:go_router/go_router.dart';

import 'falsos.dart';
import 'falsos_fichas.dart';

/// Aplicação de teste com as rotas reais do ramo Fichas (sem a barra inferior), a começar em [inicio].
class AppFichas {
  AppFichas({String inicio = AppRouter.fichas, this.outras = const []})
    : router = GoRouter(
        initialLocation: inicio,
        routes: [AppRouter.rotasFichas()],
      );

  final GoRouter router;

  /// Substituições a mais (por exemplo, o selecionador de ficheiros).
  final List<Override> outras;
  final FichasRepositorioFalso fichas = FichasRepositorioFalso();
  final DominiosRepositorioFalso dominios = DominiosRepositorioFalso();
  final DocumentosRepositorioFalso documentos = DocumentosRepositorioFalso();

  /// Caminho atual.
  String get local => router.routerDelegate.currentConfiguration.uri.path;

  /// Widget da aplicação, com a letra do sistema à [escalaLetra].
  Widget widget({double escalaLetra = 1}) {
    return ProviderScope(
      overrides: [
        fichasRepositorioProvider.overrideWithValue(fichas),
        dominiosRepositorioProvider.overrideWithValue(dominios),
        documentosRepositorioProvider.overrideWithValue(documentos),
        relogioProvider.overrideWithValue(
          RelogioFixo(DateTime(2026, 11, 9, 12)),
        ),
        ...outras,
      ],
      child: MaterialApp.router(
        theme: TemaDoseCerta.claro(),
        locale: const Locale('pt', 'PT'),
        supportedLocales: const [Locale('pt', 'PT')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        routerConfig: router,
        builder: (context, filho) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(escalaLetra)),
          child: filho!,
        ),
      ),
    );
  }
}
