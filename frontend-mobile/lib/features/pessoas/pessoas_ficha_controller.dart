import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/papel.dart';
import '../../models/pessoa_ficha_dto.dart';
import '../fichas/lista_fichas_controller.dart';

/// Ação do Cuidador sobre uma pessoa da ficha (protótipo v02, E12), com o texto do menu e da confirmação.
enum AcaoPessoa {
  /// OP-23 (RF-049).
  promover('Promover a Cuidador', 'Promover'),

  /// OP-24 (RF-130).
  despromover('Despromover a Familiar', 'Despromover'),

  /// OP-25 (RF-131).
  remover('Remover', 'Remover'),

  /// OP-26 (RF-132).
  tornarPrincipal('Tornar principal', 'Tornar principal');

  const AcaoPessoa(this.rotulo, this.botaoConfirmar);

  /// Texto da ação no menu da pessoa.
  final String rotulo;

  /// Texto do botão que confirma a ação.
  final String botaoConfirmar;

  /// Explicação do efeito, mostrada na confirmação.
  String efeito(String nome) => switch (this) {
    promover =>
      '$nome passa a Cuidador e fica com acesso completo à ficha (RF-047).',
    despromover => '$nome passa a Familiar e fica só com consulta.',
    remover => '$nome deixa de ter acesso à ficha.',
    tornarPrincipal =>
      '$nome passa a Cuidador principal. O Cuidador principal atual continua '
          'Cuidador.',
  };

  /// Ações que o ecrã oferece para a [pessoa]. É só apresentação: o servidor decide cada pedido e recusa, por
  /// exemplo, despromover ou remover o Cuidador principal sem substituto (RF-133) ou deixar a ficha sem Cuidador
  /// (RF-134). Na [fichaPropria] o titular, que é sempre o Cuidador principal, não tem ações e ninguém pode
  /// tornar-se principal (RF-033, RF-034; matriz de permissões, RP-06).
  static List<AcaoPessoa> disponiveis(
    PessoaFichaDto pessoa, {
    required bool fichaPropria,
  }) {
    if (pessoa.papel == Papel.familiar) {
      return const [promover, remover];
    }
    if (pessoa.principal) {
      return fichaPropria ? const [] : const [despromover, remover];
    }
    return fichaPropria
        ? const [despromover, remover]
        : const [despromover, remover, tornarPrincipal];
  }
}

/// Resultado de uma ação sobre uma pessoa.
enum ResultadoAcaoPessoa {
  /// A ação foi feita e a lista atualizada.
  feita,

  /// O Utilizador removeu o seu próprio acesso: deixou de ver a ficha (RF-131; matriz, saída por iniciativa própria).
  saiuDaFicha,

  /// O servidor recusou ou não houve ligação; o erro está no estado e a lista ficou como estava.
  falhou,
}

/// Estado do ecrã E12 Pessoas da ficha.
class PessoasFichaEstado {
  /// Cria o estado.
  const PessoasFichaEstado({
    this.pessoas = const [],
    this.aCarregar = false,
    this.carregada = false,
    this.aAlterar = false,
    this.erroLista,
    this.erroAcao,
  });

  /// Pessoas associadas à ficha (OP-18).
  final List<PessoaFichaDto> pessoas;

  /// Indica que OP-18 está em curso.
  final bool aCarregar;

  /// Indica que a lista já foi obtida pelo menos uma vez.
  final bool carregada;

  /// Indica que uma ação (OP-23 a OP-26) está em curso.
  final bool aAlterar;

  /// Erro ao obter a lista.
  final String? erroLista;

  /// Erro da última ação, com a explicação da recusa.
  final String? erroAcao;

  /// Copia o estado, alterando os valores indicados. Os erros não indicados são limpos.
  PessoasFichaEstado copiar({
    List<PessoaFichaDto>? pessoas,
    bool aCarregar = false,
    bool? carregada,
    bool aAlterar = false,
    String? erroLista,
    String? erroAcao,
  }) {
    return PessoasFichaEstado(
      pessoas: pessoas ?? this.pessoas,
      aCarregar: aCarregar,
      carregada: carregada ?? this.carregada,
      aAlterar: aAlterar,
      erroLista: erroLista,
      erroAcao: erroAcao,
    );
  }
}

/// Controlador do ecrã E12 Pessoas da ficha (OP-18, OP-23 a OP-26; RF-032 a RF-034, RF-047 a RF-049, RF-130 a
/// RF-134).
class PessoasFichaController extends Notifier<PessoasFichaEstado> {
  /// Cria o controlador da ficha [fichaId].
  PessoasFichaController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  @override
  PessoasFichaEstado build() => const PessoasFichaEstado();

  /// OP-18: obtém as pessoas associadas, com o papel e o Cuidador principal (RF-025, RF-047, RF-048).
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true, erroAcao: state.erroAcao);
    try {
      final pessoas = await ref
          .read(pessoasRepositorioProvider)
          .listar(fichaId);
      if (ref.mounted) {
        state = state.copiar(
          pessoas: pessoas,
          carregada: true,
          erroAcao: state.erroAcao,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(
          erroLista: MensagensErro.traduzir(erro),
          erroAcao: state.erroAcao,
        );
      }
    }
  }

  /// Executa a [acao] sobre a [pessoa], já confirmada pelo Utilizador. Feita a ação, volta a pedir a lista e o
  /// acesso à ficha, porque o papel do próprio Utilizador pode ter mudado; se ele removeu o seu próprio acesso,
  /// pede de novo a lista de fichas. Se o servidor recusar, mostra a explicação e a lista fica como estava.
  Future<ResultadoAcaoPessoa> executar(
    AcaoPessoa acao,
    PessoaFichaDto pessoa,
  ) async {
    if (state.aAlterar) {
      return ResultadoAcaoPessoa.falhou;
    }
    state = state.copiar(aAlterar: true);
    final repositorio = ref.read(pessoasRepositorioProvider);
    final id = pessoa.utilizadorId;
    try {
      await switch (acao) {
        AcaoPessoa.promover => repositorio.promover(fichaId, id),
        AcaoPessoa.despromover => repositorio.despromover(fichaId, id),
        AcaoPessoa.remover => repositorio.remover(fichaId, id),
        AcaoPessoa.tornarPrincipal => repositorio.transferirPrincipal(
          fichaId,
          id,
        ),
      };
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroAcao: MensagensErro.traduzir(erro));
      }
      return ResultadoAcaoPessoa.falhou;
    }
    if (!ref.mounted) {
      return ResultadoAcaoPessoa.feita;
    }
    final eu = ref.read(sessaoControllerProvider).utilizadorId;
    if (acao == AcaoPessoa.remover && id == eu) {
      state = state.copiar();
      await ref.read(listaFichasControllerProvider.notifier).carregar();
      return ResultadoAcaoPessoa.saiuDaFicha;
    }
    state = state.copiar();
    await Future.wait([
      carregar(),
      ref.read(fichaAcessoProvider(fichaId).notifier).recarregar(),
    ]);
    return ResultadoAcaoPessoa.feita;
  }
}

/// Fornecedor do controlador de E12, por ficha.
final pessoasFichaControllerProvider = NotifierProvider.autoDispose
    .family<PessoasFichaController, PessoasFichaEstado, String>(
      PessoasFichaController.new,
    );
