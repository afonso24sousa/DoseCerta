import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/agenda/agenda_page.dart';
import '../features/agenda/form_compromisso_page.dart';
import '../features/autenticacao/inicio_sessao_page.dart';
import '../features/autenticacao/registo_page.dart';
import '../features/conta/conta_page.dart';
import '../features/fichas/documentos_ficha_page.dart';
import '../features/fichas/editar_ficha_page.dart';
import '../features/fichas/estado_ficha_page.dart';
import '../features/fichas/ficha_page.dart';
import '../features/fichas/lista_fichas_page.dart';
import '../features/fichas/nova_ficha_dados_page.dart';
import '../features/fichas/nova_ficha_documentos_page.dart';
import '../features/lista_diaria/avisos_locais_page.dart';
import '../features/lista_diaria/lista_diaria_page.dart';
import '../features/medicacao/formulario_tratamento_page.dart';
import '../features/medicacao/medicacao_ficha_page.dart';
import '../features/medicacao/tratamento_page.dart';
import '../features/medicacao/vista_medicacao.dart';
import '../features/navegacao/estrutura_navegacao.dart';
import '../features/navegacao/pagina_provisoria.dart';
import '../features/pessoas/convidar_pessoa_page.dart';
import '../features/pessoas/convites_recebidos_page.dart';
import '../features/pessoas/pessoas_ficha_page.dart';
import '../features/registos/novo_registo_page.dart';
import '../features/registos/registos_page.dart';
import '../models/conta_criada_dto.dart';
import '../models/tipo_registo.dart';
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

  /// E04 Lista de tarefas diária.
  static const String hoje = '/hoje';

  /// E26 Calendário sincronizado e avisos locais, aberto a partir do E04.
  static const String avisosLocais = '/hoje/avisos-locais';

  /// E05 Fichas.
  static const String fichas = '/fichas';

  /// E06 Nova ficha: dados e necessidades.
  static const String novaFicha = '/fichas/nova';

  /// E07 Nova ficha: documentos e Cuidador principal.
  static const String novaFichaDocumentos = '/fichas/nova/documentos';

  /// E08 Ficha.
  static String ficha(String fichaId) => '/fichas/$fichaId';

  /// E09 Editar ficha.
  static String editarFicha(String fichaId) => '/fichas/$fichaId/editar';

  /// E10 Documentos da ficha.
  static String documentosFicha(String fichaId) =>
      '/fichas/$fichaId/documentos';

  /// E11 Arquivar, reativar ou eliminar ficha.
  static String estadoFicha(String fichaId) => '/fichas/$fichaId/estado';

  /// E15 Medicação da ficha.
  static String medicacaoFicha(String fichaId) => '/fichas/$fichaId/medicacao';

  /// E15 Medicação da ficha, aberto no separador «Calendário de tomas» (I069).
  static String calendarioTomas(String fichaId) =>
      '/fichas/$fichaId/medicacao?vista=calendario';

  /// E16 Registar tratamento.
  static String novoTratamento(String fichaId) =>
      '/fichas/$fichaId/medicacao/novo';

  /// E17 Detalhe do tratamento.
  static String tratamento(String fichaId, String tratamentoId) =>
      '/fichas/$fichaId/medicacao/$tratamentoId';

  /// E16 Editar tratamento.
  static String editarTratamento(String fichaId, String tratamentoId) =>
      '/fichas/$fichaId/medicacao/$tratamentoId/editar';

  /// E12 Pessoas da ficha.
  static String pessoasFicha(String fichaId) => '/fichas/$fichaId/pessoas';

  /// E13 Convidar pessoa.
  static String convidarPessoa(String fichaId) =>
      '/fichas/$fichaId/pessoas/convidar';

  /// E23 Ocorrências e notas.
  static String registosFicha(String fichaId) => '/fichas/$fichaId/registos';

  /// E24 Nova ocorrência ou nota, com o [tipo] escolhido à partida (`?tipo=Nota`).
  static String novoRegisto(String fichaId, {TipoRegisto? tipo}) =>
      '/fichas/$fichaId/registos/novo'
      '${tipo == null ? '' : '?tipo=${tipo.valor}'}';

  /// E14 Convites recebidos.
  static const String convites = '/convites';

  /// E21 Agenda.
  static const String agenda = '/agenda';

  /// E22 Registar compromisso.
  static String novoCompromisso(String fichaId) =>
      '/fichas/$fichaId/compromissos/novo';

  /// E22 Editar compromisso.
  static String editarCompromisso(String fichaId, String compromissoId) =>
      '/fichas/$fichaId/compromissos/$compromissoId/editar';

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
        ...rotasCompromissos(),
        StatefulShellRoute.indexedStack(
          builder: (context, estado, shell) => EstruturaNavegacao(shell: shell),
          branches: [
            StatefulShellBranch(routes: [rotaHoje()]),
            StatefulShellBranch(routes: [rotasFichas(), rotaConvites()]),
            _ramo(agenda, const AgendaPage()),
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

  /// Rotas do ramo Fichas (modelo de classes, secção 8.1). `nova` vem antes de `:fichaId`, para não ser lida como
  /// um identificador de ficha.
  static GoRoute rotasFichas() {
    return GoRoute(
      path: fichas,
      builder: (context, estado) => const ListaFichasPage(),
      routes: [
        GoRoute(
          path: 'nova',
          builder: (context, estado) => const NovaFichaDadosPage(),
          routes: [
            GoRoute(
              path: 'documentos',
              builder: (context, estado) => const NovaFichaDocumentosPage(),
            ),
          ],
        ),
        GoRoute(
          path: ':fichaId',
          builder: (context, estado) =>
              FichaPage(fichaId: estado.pathParameters['fichaId']!),
          routes: [
            rotasMedicacao(),
            GoRoute(
              path: 'pessoas',
              builder: (context, estado) =>
                  PessoasFichaPage(fichaId: estado.pathParameters['fichaId']!),
              routes: [
                GoRoute(
                  path: 'convidar',
                  builder: (context, estado) => ConvidarPessoaPage(
                    fichaId: estado.pathParameters['fichaId']!,
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'registos',
              builder: (context, estado) =>
                  RegistosPage(fichaId: estado.pathParameters['fichaId']!),
              routes: [
                GoRoute(
                  path: 'novo',
                  builder: (context, estado) => NovoRegistoPage(
                    fichaId: estado.pathParameters['fichaId']!,
                    tipo: estado.uri.queryParameters['tipo'] == 'Nota'
                        ? TipoRegisto.nota
                        : TipoRegisto.ocorrencia,
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'editar',
              builder: (context, estado) =>
                  EditarFichaPage(fichaId: estado.pathParameters['fichaId']!),
            ),
            GoRoute(
              path: 'documentos',
              builder: (context, estado) => DocumentosFichaPage(
                fichaId: estado.pathParameters['fichaId']!,
              ),
            ),
            GoRoute(
              path: 'estado',
              builder: (context, estado) =>
                  EstadoFichaPage(fichaId: estado.pathParameters['fichaId']!),
            ),
          ],
        ),
      ],
    );
  }

  /// Rotas de E22 (modelo de classes, secção 8). Ficam fora da barra inferior e antes do ramo Fichas, para E21 as
  /// abrir com `push` por cima da agenda e voltar a ela com a seta de voltar.
  static List<GoRoute> rotasCompromissos() {
    return [
      GoRoute(
        path: '/fichas/:fichaId/compromissos/novo',
        builder: (context, estado) =>
            FormCompromissoPage(fichaId: estado.pathParameters['fichaId']!),
      ),
      GoRoute(
        path: '/fichas/:fichaId/compromissos/:compromissoId/editar',
        builder: (context, estado) => FormCompromissoPage(
          fichaId: estado.pathParameters['fichaId']!,
          compromissoId: estado.pathParameters['compromissoId'],
        ),
      ),
    ];
  }

  /// Rotas da medicação da ficha (E15 a E17). `novo` vem antes de `:tratamentoId`, para não ser lido como um
  /// identificador de tratamento.
  static GoRoute rotasMedicacao() {
    return GoRoute(
      path: 'medicacao',
      builder: (context, estado) => MedicacaoFichaPage(
        fichaId: estado.pathParameters['fichaId']!,
        vista: VistaMedicacao.daRota(estado.uri.queryParameters['vista']),
      ),
      routes: [
        GoRoute(
          path: 'novo',
          builder: (context, estado) => FormularioTratamentoPage(
            fichaId: estado.pathParameters['fichaId']!,
          ),
        ),
        GoRoute(
          path: ':tratamentoId',
          builder: (context, estado) => TratamentoPage(
            fichaId: estado.pathParameters['fichaId']!,
            tratamentoId: estado.pathParameters['tratamentoId']!,
          ),
          routes: [
            GoRoute(
              path: 'editar',
              builder: (context, estado) => FormularioTratamentoPage(
                fichaId: estado.pathParameters['fichaId']!,
                tratamentoId: estado.pathParameters['tratamentoId'],
              ),
            ),
          ],
        ),
      ],
    );
  }

  /// Rota do E04 Lista de tarefas diária, com o E26 por baixo.
  static GoRoute rotaHoje() {
    return GoRoute(
      path: hoje,
      builder: (context, estado) => const ListaDiariaPage(),
      routes: [
        GoRoute(
          path: 'avisos-locais',
          builder: (context, estado) => const AvisosLocaisPage(),
        ),
      ],
    );
  }

  /// Rota de E14 (modelo de classes, secção 8). Fica no ramo Fichas, porque E14 abre-se a partir de E05 (protótipo
  /// v02, página 36); E05 abre-a com `push`, para o ecrã ter a seta de voltar.
  static GoRoute rotaConvites() {
    return GoRoute(
      path: convites,
      builder: (context, estado) => const ConvitesRecebidosPage(),
    );
  }

  static StatefulShellBranch _ramo(String caminho, Widget pagina) {
    return StatefulShellBranch(
      routes: [GoRoute(path: caminho, builder: (context, estado) => pagina)],
    );
  }
}

/// Rotas da aplicação.
final appRouterProvider = Provider<GoRouter>(AppRouter.criar);
