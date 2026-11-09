import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/autenticacao/inicio_sessao_page.dart';
import '../features/autenticacao/registo_page.dart';
import '../features/conta/conta_page.dart';
import '../features/navegacao/estrutura_navegacao.dart';
import '../features/navegacao/pagina_provisoria.dart';
import '../models/conta_criada_dto.dart';
import 'provedores.dart';
import 'sessao_controller.dart';

/// Rotas (`go_router`) e redirecionamento conforme a sessão (RF-017): sem sessão, só E01 e E02 abrem e tudo o
/// resto leva a E02; com sessão, E01 e E02 levam a E04.
class AppRouter {
  const AppRouter._();

  /// E01 Registo.
  static const String registo = '/registo';

  /// E02 Início de sessão.
  static const String entrar = '/entrar';

  /// E04 Lista de tarefas diária (provisória).
  static const String hoje = '/hoje';

  /// E05 Fichas (provisória).
  static const String fichas = '/fichas';

  /// E21 Agenda (provisória).
  static const String agenda = '/agenda';

  /// E25 Notificações (provisória).
  static const String notificacoes = '/notificacoes';

  /// E03 Conta.
  static const String conta = '/conta';

  /// Rotas que abrem sem sessão.
  static const Set<String> rotasPublicas = {registo, entrar};

  /// Decide o redirecionamento para a rota [local] no [estado] de sessão; `null` mantém a rota.
  static String? redirecionar(SessaoEstado estado, String local) {
    final publica = rotasPublicas.contains(local);
    if (!estado.autenticada && !publica) {
      return entrar;
    }
    if (estado.autenticada && publica) {
      return hoje;
    }
    return null;
  }

  /// Cria as rotas; [ref] dá acesso ao estado de sessão.
  static GoRouter criar(Ref ref) {
    final sessaoIniciada = ValueNotifier<bool>(
      ref.read(sessaoControllerProvider).autenticada,
    );
    ref.listen(
      sessaoControllerProvider,
      (_, estado) => sessaoIniciada.value = estado.autenticada,
    );
    final router = GoRouter(
      initialLocation: hoje,
      refreshListenable: sessaoIniciada,
      redirect: (context, estado) => redirecionar(
        ref.read(sessaoControllerProvider),
        estado.matchedLocation,
      ),
      routes: [
        GoRoute(
          path: registo,
          builder: (context, estado) => const RegistoPage(),
        ),
        GoRoute(
          path: entrar,
          builder: (context, estado) => InicioSessaoPage(
            contaCriada: estado.extra is ContaCriadaDto
                ? estado.extra! as ContaCriadaDto
                : null,
          ),
        ),
        StatefulShellRoute.indexedStack(
          builder: (context, estado, shell) => EstruturaNavegacao(shell: shell),
          branches: [
            _ramo(
              hoje,
              const PaginaProvisoria(
                titulo: 'Lista de tarefas diária',
                ecra: 'E04',
              ),
            ),
            _ramo(
              fichas,
              const PaginaProvisoria(titulo: 'Fichas', ecra: 'E05'),
            ),
            _ramo(
              agenda,
              const PaginaProvisoria(titulo: 'Agenda', ecra: 'E21'),
            ),
            _ramo(
              notificacoes,
              const PaginaProvisoria(titulo: 'Notificações', ecra: 'E25'),
            ),
            _ramo(conta, const ContaPage()),
          ],
        ),
      ],
    );
    ref.onDispose(() {
      router.dispose();
      sessaoIniciada.dispose();
    });
    return router;
  }

  static StatefulShellBranch _ramo(String caminho, Widget pagina) {
    return StatefulShellBranch(
      routes: [GoRoute(path: caminho, builder: (context, estado) => pagina)],
    );
  }
}

/// Rotas da aplicação.
final appRouterProvider = Provider<GoRouter>(AppRouter.criar);
