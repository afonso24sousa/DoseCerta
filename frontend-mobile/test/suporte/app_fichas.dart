import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/shared/abridor_documentos.dart';
import 'package:dosecerta/shared/selecionador_ficheiros.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart';
import 'package:go_router/go_router.dart';

import 'falsos.dart';
import 'falsos_fichas.dart';
import 'falsos_pessoas.dart';
import 'falsos_tratamentos.dart';

/// Aplicação de teste com as rotas reais do ramo Fichas, incluindo E14 (sem a barra inferior), a começar em
/// [inicio].
class AppFichas {
  AppFichas({String inicio = AppRouter.fichas, this.outras = const []})
    : router = GoRouter(
        initialLocation: inicio,
        routes: [AppRouter.rotasFichas(), AppRouter.rotaConvites()],
      );

  final GoRouter router;

  /// Substituições a mais (por exemplo, o selecionador de ficheiros).
  final List<Override> outras;
  final FichasRepositorioFalso fichas = FichasRepositorioFalso();
  final DominiosRepositorioFalso dominios = DominiosRepositorioFalso();
  final DocumentosRepositorioFalso documentos = DocumentosRepositorioFalso();
  final FonteFicheirosFalsa fonte = FonteFicheirosFalsa();
  final AbridorDocumentosFalso abridor = AbridorDocumentosFalso();
  final TratamentosRepositorioFalso tratamentos = TratamentosRepositorioFalso();
  final ConvitesRepositorioFalso convites = ConvitesRepositorioFalso();
  final PessoasRepositorioFalso pessoas = PessoasRepositorioFalso();

  /// Identificador do Utilizador da sessão de teste.
  static const String utilizadorId = 'u-eu';

  /// Caminho atual.
  String get local => router.routerDelegate.currentConfiguration.uri.path;

  /// Widget da aplicação, com a letra do sistema à [escalaLetra].
  Widget widget({double escalaLetra = 1}) {
    return ProviderScope(
      overrides: [
        fichasRepositorioProvider.overrideWithValue(fichas),
        dominiosRepositorioProvider.overrideWithValue(dominios),
        documentosRepositorioProvider.overrideWithValue(documentos),
        tratamentosRepositorioProvider.overrideWithValue(tratamentos),
        convitesRepositorioProvider.overrideWithValue(convites),
        pessoasRepositorioProvider.overrideWithValue(pessoas),
        sessaoInicialProvider.overrideWithValue(
          SessaoGuardada(
            token: 't',
            expiraEm: DateTime(2026, 11, 10),
            utilizadorId: utilizadorId,
          ),
        ),
        abridorDocumentosProvider.overrideWithValue(abridor),
        selecionadorFicheirosProvider.overrideWithValue(
          SelecionadorFicheiros(fonte),
        ),
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
