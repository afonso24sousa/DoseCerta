import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/ficha_dto.dart';
import '../models/papel.dart';
import 'provedores.dart';

/// Ficha aberta e o que o papel do Utilizador permite nela. É só apresentação: o servidor decide cada pedido
/// (RNF-004; matriz de permissões).
class FichaAcesso {
  /// Cria o acesso à [ficha].
  const FichaAcesso(this.ficha);

  /// Ficha (OP-10).
  final FichaDto ficha;

  /// Papel do Utilizador na ficha.
  Papel get papel => ficha.meuPapel;

  /// Indica se o Utilizador é o Cuidador principal.
  bool get souPrincipal => ficha.souPrincipal;

  /// Indica se a ficha está ativa.
  bool get ativa => ficha.ativa;

  /// Editar, carregar documentos e as restantes escritas: só o Cuidador, numa ficha ativa (RF-029, RF-047, RF-120).
  bool get podeEscrever => ficha.souCuidador && ficha.ativa;

  /// Registar (medicação, compromissos, ocorrências e notas): igual a [podeEscrever].
  bool get podeRegistar => podeEscrever;

  /// Arquivar, reativar e eliminar: só o Cuidador, em qualquer estado da ficha (RF-120 a RF-123).
  bool get podeGerirEstado => ficha.souCuidador;
}

/// Fornece, por `fichaId`, a ficha e o papel do Utilizador nela, para as rotas `/fichas/:fichaId` (modelo de
/// classes, secção 6.1). Quem não está associado recebe `404` do servidor (RNF-005), que chega como erro.
class FichaAcessoProvider extends AsyncNotifier<FichaAcesso> {
  /// Cria o fornecedor da ficha [fichaId].
  FichaAcessoProvider(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  @override
  Future<FichaAcesso> build() => carregar();

  /// OP-10: obtém a ficha.
  Future<FichaAcesso> carregar() async {
    final ficha = await ref.read(fichasRepositorioProvider).obter(fichaId);
    return FichaAcesso(ficha);
  }

  /// Volta a pedir a ficha (por exemplo depois de arquivar ou reativar).
  Future<void> recarregar() async {
    state = await AsyncValue.guard(carregar);
  }

  /// Substitui a ficha pela devolvida numa edição (OP-11), sem novo pedido.
  void atualizar(FichaDto ficha) {
    state = AsyncData(FichaAcesso(ficha));
  }
}

/// Fornecedor da ficha aberta, por `fichaId`.
final fichaAcessoProvider = AsyncNotifierProvider.autoDispose
    .family<FichaAcessoProvider, FichaAcesso, String>(FichaAcessoProvider.new);
