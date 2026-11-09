import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/problema_api.dart';
import '../../models/utilizador_dto.dart';
import '../../shared/validador_palavra_passe.dart';

/// Estado do ecrã E03 Conta. Os erros dos dados e da palavra-passe são independentes, porque são duas ações.
class ContaEstado {
  /// Cria o estado.
  const ContaEstado({
    this.conta,
    this.aCarregar = false,
    this.erroCarregar,
    this.errosDados = const {},
    this.erroDados,
    this.aGuardarDados = false,
    this.errosPalavraPasse = const {},
    this.erroPalavraPasse,
    this.aAlterarPalavraPasse = false,
    this.erroTerminar,
    this.aTerminar = false,
  });

  /// Dados da conta (OP-04), sem palavra-passe (RF-013).
  final UtilizadorDto? conta;

  /// Indica que OP-04 está em curso.
  final bool aCarregar;

  /// Erro ao carregar a conta.
  final String? erroCarregar;

  /// Erros por campo de «Guardar dados» (`nome`, `email`).
  final Map<String, String> errosDados;

  /// Erro geral de «Guardar dados».
  final String? erroDados;

  /// Indica que OP-05 está em curso.
  final bool aGuardarDados;

  /// Erros por campo de «Alterar palavra-passe» (`palavraPasseAtual`, `palavraPasseNova`).
  final Map<String, String> errosPalavraPasse;

  /// Erro geral de «Alterar palavra-passe».
  final String? erroPalavraPasse;

  /// Indica que OP-06 está em curso.
  final bool aAlterarPalavraPasse;

  /// Erro ao terminar sessão (por exemplo, sem ligação).
  final String? erroTerminar;

  /// Indica que OP-03 está em curso.
  final bool aTerminar;

  /// Indica se alguma ação está em curso.
  bool get ocupado =>
      aCarregar || aGuardarDados || aAlterarPalavraPasse || aTerminar;

  /// Copia o estado, alterando os valores indicados. Os erros não indicados são limpos.
  ContaEstado copiar({
    UtilizadorDto? conta,
    bool aCarregar = false,
    String? erroCarregar,
    Map<String, String> errosDados = const {},
    String? erroDados,
    bool aGuardarDados = false,
    Map<String, String> errosPalavraPasse = const {},
    String? erroPalavraPasse,
    bool aAlterarPalavraPasse = false,
    String? erroTerminar,
    bool aTerminar = false,
  }) {
    return ContaEstado(
      conta: conta ?? this.conta,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      errosDados: errosDados,
      erroDados: erroDados,
      aGuardarDados: aGuardarDados,
      errosPalavraPasse: errosPalavraPasse,
      erroPalavraPasse: erroPalavraPasse,
      aAlterarPalavraPasse: aAlterarPalavraPasse,
      erroTerminar: erroTerminar,
      aTerminar: aTerminar,
    );
  }
}

/// Controlador do ecrã E03 Conta (OP-03 a OP-06; RF-011, RF-013, RF-014, RF-015, RF-016, RNF-003, RNF-028).
class ContaController extends Notifier<ContaEstado> {
  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'nome': 'Nome',
    'email': 'E-mail',
    'palavraPasseAtual': 'Palavra-passe atual',
    'palavraPasseNova': 'Nova palavra-passe',
  };

  /// Mensagens deste ecrã que diferem do catálogo (protótipo, E03).
  static const Map<String, String> mensagensDoEcra = {
    'EMAIL_JA_REGISTADO': 'Este e-mail pertence a outra conta.',
  };

  @override
  ContaEstado build() => const ContaEstado();

  /// OP-04: carrega os dados da conta.
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true);
    try {
      final conta = await ref.read(contaRepositorioProvider).obter();
      if (ref.mounted) {
        state = state.copiar(conta: conta);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroCarregar: MensagensErro.traduzir(erro));
      }
    }
  }

  /// OP-05: guarda o nome e o e-mail. Devolve os dados guardados, ou `null` se houver erros.
  Future<UtilizadorDto?> guardarDados(String nome, String email) async {
    final erros = <String, String>{};
    if (nome.trim().isEmpty) {
      erros['nome'] = MensagensErro.comRotulo(
        rotulos['nome']!,
        'É obrigatório.',
      );
    }
    if (email.trim().isEmpty) {
      erros['email'] = MensagensErro.comRotulo(
        rotulos['email']!,
        'É obrigatório.',
      );
    }
    if (erros.isNotEmpty) {
      state = state.copiar(errosDados: erros);
      return null;
    }
    state = state.copiar(aGuardarDados: true);
    try {
      final conta = await ref
          .read(contaRepositorioProvider)
          .atualizar(nome.trim(), email.trim());
      if (ref.mounted) {
        state = state.copiar(conta: conta);
      }
      return conta;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        state = problema.codigo == 'EMAIL_JA_REGISTADO'
            ? state.copiar(
                errosDados: {'email': mensagensDoEcra['EMAIL_JA_REGISTADO']!},
              )
            : _comErros(problema, (erros, geral) {
                return state.copiar(errosDados: erros, erroDados: geral);
              });
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroDados: MensagensErro.traduzir(erro));
      }
    }
    return null;
  }

  /// OP-06: altera a palavra-passe. Devolve `true` se foi alterada.
  Future<bool> alterarPalavraPasse(String atual, String nova) async {
    final erros = <String, String>{};
    if (atual.isEmpty) {
      erros['palavraPasseAtual'] = MensagensErro.comRotulo(
        rotulos['palavraPasseAtual']!,
        'É obrigatória.',
      );
    }
    final regra = ValidadorPalavraPasse.validar(nova);
    if (regra != null) {
      erros['palavraPasseNova'] = MensagensErro.comRotulo(
        rotulos['palavraPasseNova']!,
        regra,
      );
    }
    if (erros.isNotEmpty) {
      state = state.copiar(errosPalavraPasse: erros);
      return false;
    }
    state = state.copiar(aAlterarPalavraPasse: true);
    try {
      await ref.read(contaRepositorioProvider).alterarPalavraPasse(atual, nova);
      if (ref.mounted) {
        state = state.copiar();
      }
      return true;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        state = _comErros(problema, (erros, geral) {
          return state.copiar(
            errosPalavraPasse: erros,
            erroPalavraPasse: geral,
          );
        });
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroPalavraPasse: MensagensErro.traduzir(erro));
      }
    }
    return false;
  }

  /// OP-03: termina a sessão; as rotas levam o Utilizador ao início de sessão (RF-016).
  Future<void> terminarSessao() async {
    state = state.copiar(aTerminar: true);
    try {
      await ref.read(sessaoControllerProvider.notifier).terminarSessao();
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroTerminar: MensagensErro.traduzir(erro));
      }
    }
  }

  ContaEstado _comErros(
    ProblemaApi problema,
    ContaEstado Function(Map<String, String> erros, String? geral) criar,
  ) {
    final erros = MensagensErro.mensagensDeCampo(problema, rotulos);
    return criar(
      erros,
      erros.isEmpty
          ? MensagensErro.traduzir(problema, especificas: mensagensDoEcra)
          : null,
    );
  }
}

/// Fornecedor do controlador de E03.
final contaControllerProvider =
    NotifierProvider.autoDispose<ContaController, ContaEstado>(
      ContaController.new,
    );
