import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/convite_dto.dart';
import '../../models/problema_api.dart';
import '../fichas/lista_fichas_controller.dart';

/// Estado do ecrã E14 Convites recebidos.
class ConvitesRecebidosEstado {
  /// Cria o estado.
  const ConvitesRecebidosEstado({
    this.convites = const [],
    this.aCarregar = false,
    this.carregada = false,
    this.emResposta = const {},
    this.erro,
  });

  /// Convites pendentes dirigidos ao Utilizador (OP-20).
  final List<ConviteDto> convites;

  /// Indica que OP-20 está em curso.
  final bool aCarregar;

  /// Indica que a lista já foi obtida pelo menos uma vez.
  final bool carregada;

  /// Convites com uma resposta (aceitar ou recusar) em curso, para não responder duas vezes.
  final Set<String> emResposta;

  /// Erro ao obter a lista ou ao responder a um convite.
  final String? erro;

  /// Copia o estado, alterando os valores indicados. O erro não indicado é limpo.
  ConvitesRecebidosEstado copiar({
    List<ConviteDto>? convites,
    bool aCarregar = false,
    bool? carregada,
    Set<String>? emResposta,
    String? erro,
  }) {
    return ConvitesRecebidosEstado(
      convites: convites ?? this.convites,
      aCarregar: aCarregar,
      carregada: carregada ?? this.carregada,
      emResposta: emResposta ?? this.emResposta,
      erro: erro,
    );
  }
}

/// Controlador do ecrã E14 Convites recebidos (OP-20 a OP-22; RF-026, RF-027, RF-042, RF-044 a RF-046, RF-135).
/// O ecrã E05 usa o mesmo estado para indicar quantos convites estão pendentes (RF-042).
class ConvitesRecebidosController extends Notifier<ConvitesRecebidosEstado> {
  /// Códigos com que o servidor recusa responder a um convite que deixou de estar pendente para o Utilizador:
  /// o convite sai da lista, porque já não pode ser aceite nem recusado.
  static const Set<String> codigosConviteIndisponivel = {
    'CONVITE_NAO_ENCONTRADO',
    'CONVITE_JA_RESPONDIDO',
    'PESSOA_JA_ASSOCIADA',
  };

  /// Mensagens deste ecrã que substituem as do catálogo.
  static const Map<String, String> mensagensEspecificas = {
    'PESSOA_JA_ASSOCIADA':
        'Já está associado a esta ficha, por isso o convite não foi aceite.',
  };

  @override
  ConvitesRecebidosEstado build() => const ConvitesRecebidosEstado();

  /// OP-20: obtém os convites pendentes, incluindo os criados antes do registo (RF-044) e os de e-mail não
  /// entregue (RF-135).
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true);
    try {
      final convites = await ref
          .read(convitesRepositorioProvider)
          .listarRecebidos();
      if (ref.mounted) {
        state = state.copiar(convites: convites, carregada: true);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erro: MensagensErro.traduzir(erro));
      }
    }
  }

  /// OP-21: aceita o convite; o Utilizador fica associado à ficha com o papel oferecido (RF-045) ou, no convite
  /// para Cuidador principal, passa a principal (RF-026). A lista de fichas (E05) volta a ser pedida, para
  /// mostrar a nova ficha. Devolve `true` se o convite foi aceite.
  Future<bool> aceitar(ConviteDto convite) async {
    final aceite = await _responder(
      convite,
      () => ref.read(convitesRepositorioProvider).aceitar(convite.id),
    );
    if (aceite && ref.mounted) {
      await ref.read(listaFichasControllerProvider.notifier).carregar();
    }
    return aceite;
  }

  /// OP-22: recusa o convite, sem criar associação (RF-046, RF-027). Devolve `true` se o convite foi recusado.
  Future<bool> recusar(ConviteDto convite) {
    return _responder(
      convite,
      () => ref.read(convitesRepositorioProvider).recusar(convite.id),
    );
  }

  Future<bool> _responder(
    ConviteDto convite,
    Future<Object?> Function() pedido,
  ) async {
    if (state.emResposta.contains(convite.id)) {
      return false;
    }
    state = state.copiar(
      emResposta: {...state.emResposta, convite.id},
      erro: state.erro,
    );
    try {
      await pedido();
      if (ref.mounted) {
        state = state.copiar(
          convites: _sem(convite),
          emResposta: _respondido(convite),
        );
      }
      return true;
    } on Exception catch (erro) {
      if (ref.mounted) {
        final indisponivel =
            erro is ProblemaApi &&
            codigosConviteIndisponivel.contains(erro.codigo);
        state = state.copiar(
          convites: indisponivel ? _sem(convite) : null,
          emResposta: _respondido(convite),
          erro: MensagensErro.traduzir(erro, especificas: mensagensEspecificas),
        );
      }
      return false;
    }
  }

  List<ConviteDto> _sem(ConviteDto convite) =>
      state.convites.where((c) => c.id != convite.id).toList(growable: false);

  Set<String> _respondido(ConviteDto convite) =>
      state.emResposta.difference({convite.id});
}

/// Fornecedor do controlador de E14. Não é descartado ao sair do ecrã, porque o E05 mostra o número de convites
/// pendentes a partir do mesmo estado.
final convitesRecebidosControllerProvider =
    NotifierProvider<ConvitesRecebidosController, ConvitesRecebidosEstado>(
      ConvitesRecebidosController.new,
    );
