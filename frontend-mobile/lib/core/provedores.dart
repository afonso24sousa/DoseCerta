import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../avisos_locais/agendador_avisos.dart';
import '../avisos_locais/agendador_flutter_local_notifications.dart';
import '../avisos_locais/avisos_locais_service.dart';
import '../avisos_locais/permissao_notificacoes.dart';
import '../avisos_locais/sincronizador_calendario.dart';
import '../avisos_locais/tomas_sincronizadas_store.dart';
import '../data/auth_repositorio.dart';
import '../data/calendario_repositorio.dart';
import '../data/conta_repositorio.dart';
import '../data/convites_repositorio.dart';
import '../data/documentos_repositorio.dart';
import '../data/dominios_repositorio.dart';
import '../data/fichas_repositorio.dart';
import '../data/lista_diaria_repositorio.dart';
import '../data/notificacoes_repositorio.dart';
import '../data/pessoas_repositorio.dart';
import '../data/registos_repositorio.dart';
import '../data/tomas_repositorio.dart';
import '../data/tratamentos_repositorio.dart';
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

/// Repositório das listas de necessidades (OP-08).
final dominiosRepositorioProvider = Provider<DominiosRepositorio>(
  (ref) => DominiosRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório das fichas (OP-07, OP-09 a OP-14).
final fichasRepositorioProvider = Provider<FichasRepositorio>(
  (ref) => FichasRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório dos documentos (OP-15 a OP-17).
final documentosRepositorioProvider = Provider<DocumentosRepositorio>(
  (ref) => DocumentosRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório dos tratamentos (OP-27 a OP-31).
final tratamentosRepositorioProvider = Provider<TratamentosRepositorio>(
  (ref) => TratamentosRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório das pessoas da ficha (OP-18, OP-23 a OP-26).
final pessoasRepositorioProvider = Provider<PessoasRepositorio>(
  (ref) => PessoasRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório das ocorrências e notas partilhadas (OP-46 a OP-49).
final registosRepositorioProvider = Provider<RegistosRepositorio>(
  (ref) => RegistosRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório dos convites (OP-19 a OP-22).
final convitesRepositorioProvider = Provider<ConvitesRepositorio>(
  (ref) => ConvitesRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório das tomas e da confirmação (OP-32 e OP-33).
final tomasRepositorioProvider = Provider<TomasRepositorio>(
  (ref) => TomasRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório da lista de tarefas diária (OP-50).
final listaDiariaRepositorioProvider = Provider<ListaDiariaRepositorio>(
  (ref) => ListaDiariaRepositorio(ref.watch(apiClientProvider)),
);

/// Repositório do calendário de tomas para o dispositivo (OP-34).
final calendarioRepositorioProvider = Provider<CalendarioRepositorio>(
  (ref) => CalendarioRepositorio(ref.watch(apiClientProvider)),
);

/// Agendador dos avisos no sistema operativo (os testes substituem-no por um falso).
final agendadorAvisosProvider = Provider<AgendadorAvisos>(
  (ref) => const AgendadorFlutterLocalNotifications(),
);

/// Autorizações das notificações e dos alarmes exatos (os testes substituem-nas por falsas).
final permissaoNotificacoesProvider = Provider<PermissaoNotificacoes>(
  (ref) => const PermissaoNotificacoesAndroid(),
);

/// Tomas sincronizadas guardadas no dispositivo.
final tomasSincronizadasStoreProvider = Provider<TomasSincronizadasStore>(
  (ref) => const TomasSincronizadasStore(),
);

/// Serviço dos avisos locais.
final avisosLocaisServiceProvider = Provider<AvisosLocaisService>(
  (ref) => AvisosLocaisService(
    ref.watch(agendadorAvisosProvider),
    ref.watch(relogioProvider),
  ),
);

/// Sincronização das tomas das 48 horas seguintes (OP-34) e dos avisos.
final sincronizadorCalendarioProvider = Provider<SincronizadorCalendario>(
  (ref) => SincronizadorCalendario(
    repositorio: ref.watch(calendarioRepositorioProvider),
    store: ref.watch(tomasSincronizadasStoreProvider),
    avisos: ref.watch(avisosLocaisServiceProvider),
    relogio: ref.watch(relogioProvider),
  ),
);

/// Repositório do histórico de notificações (OP-51).
final notificacoesRepositorioProvider = Provider<NotificacoesRepositorio>(
  (ref) => NotificacoesRepositorio(ref.watch(apiClientProvider)),
);
