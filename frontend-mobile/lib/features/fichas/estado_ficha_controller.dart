import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import 'lista_fichas_controller.dart';

/// Ação de E11 em curso.
enum AcaoEstadoFicha {
  /// Arquivar (OP-12).
  arquivar,

  /// Reativar (OP-13).
  reativar,

  /// Eliminar (OP-14).
  eliminar,
}

/// Estado do ecrã E11 Arquivar, reativar ou eliminar ficha.
class EstadoFichaEstado {
  /// Cria o estado.
  const EstadoFichaEstado({this.emCurso, this.erro});

  /// Ação em curso.
  final AcaoEstadoFicha? emCurso;

  /// Erro da última ação (por exemplo `FICHA_COM_HISTORICO`).
  final String? erro;
}

/// Controlador do ecrã E11 (OP-12, OP-13, OP-14; RF-120 a RF-123). Depois de cada ação atualiza a ficha aberta e
/// a lista de E05.
class EstadoFichaController extends Notifier<EstadoFichaEstado> {
  /// Cria o controlador da ficha [fichaId].
  EstadoFichaController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  @override
  EstadoFichaEstado build() => const EstadoFichaEstado();

  /// OP-12: arquiva a ficha (RF-120). Devolve `true` se ficou arquivada.
  Future<bool> arquivar() => _executar(
    AcaoEstadoFicha.arquivar,
    () => ref.read(fichasRepositorioProvider).arquivar(fichaId),
  );

  /// OP-13: reativa a ficha (RF-121). Devolve `true` se ficou ativa.
  Future<bool> reativar() => _executar(
    AcaoEstadoFicha.reativar,
    () => ref.read(fichasRepositorioProvider).reativar(fichaId),
  );

  /// OP-14: elimina a ficha, só aceite sem medicação, tomas, ocorrências nem notas (RF-123). Devolve `true` se foi
  /// eliminada.
  Future<bool> eliminar() => _executar(
    AcaoEstadoFicha.eliminar,
    () => ref.read(fichasRepositorioProvider).eliminar(fichaId),
    recarregarFicha: false,
  );

  Future<bool> _executar(
    AcaoEstadoFicha acao,
    Future<void> Function() pedido, {
    bool recarregarFicha = true,
  }) async {
    state = EstadoFichaEstado(emCurso: acao);
    try {
      await pedido();
      if (recarregarFicha) {
        await ref.read(fichaAcessoProvider(fichaId).notifier).recarregar();
      }
      ref.read(listaFichasControllerProvider.notifier).carregar().ignore();
      if (ref.mounted) {
        state = const EstadoFichaEstado();
      }
      return true;
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = EstadoFichaEstado(erro: MensagensErro.traduzir(erro));
      }
      return false;
    }
  }
}

/// Fornecedor do controlador de E11, por ficha.
final estadoFichaControllerProvider = NotifierProvider.autoDispose
    .family<EstadoFichaController, EstadoFichaEstado, String>(
      EstadoFichaController.new,
    );
