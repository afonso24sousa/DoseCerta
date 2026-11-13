import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../data/auth_repositorio.dart';
import '../data/conta_repositorio.dart';
import 'api_client.dart';
import 'auth_interceptor.dart';
import 'configuracao_api.dart';
import 'relogio.dart';
import 'sessao_controller.dart';
import 'sessao_store.dart';

/// Configuração da API, validada no arranque por `InicializadorApp`.
final configuracaoApiProvider = Provider<ConfiguracaoApi>(
  (ref) => ConfiguracaoApi.doAmbiente(),
);

/// Sessão lida do dispositivo no arranque (substituída em `main`).
final sessaoInicialProvider = Provider<SessaoGuardada?>((ref) => null);

/// Relógio da aplicação.
final relogioProvider = Provider<Relogio>((ref) => const RelogioSistema());

/// Armazenamento seguro da sessão.
final sessaoStoreProvider = Provider<SessaoStore>(
  (ref) => const SessaoStore(FlutterSecureStorage()),
);

/// Estado de sessão.
final sessaoControllerProvider =
    NotifierProvider<SessaoController, SessaoEstado>(SessaoController.new);

/// Cliente HTTP, com o token da sessão atual e o fim de sessão perante `401`.
final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(
    configuracao: ref.watch(configuracaoApiProvider),
    authInterceptor: AuthInterceptor(
      obterToken: () => ref.read(sessaoControllerProvider).token,
      aoRecusarSessao: () =>
          ref.read(sessaoControllerProvider.notifier).sessaoExpirada(),
    ),
  ),
);

/// Repositório de autenticação (OP-01 a OP-03).
final authRepositorioProvider = Provider<AuthRepositorio>(
  (ref) => AuthRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório da conta (OP-04 a OP-06).
final contaRepositorioProvider = Provider<ContaRepositorio>(
  (ref) => ContaRepositorio(ref.watch(apiClientProvider)),
);
