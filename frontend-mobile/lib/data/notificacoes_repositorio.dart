import '../core/api_client.dart';
import '../models/notificacao_dto.dart';

/// Histórico de notificações do Utilizador autenticado (OP-51). Sem regras de negócio: forma o pedido, chama a API
/// e devolve os DTO.
class NotificacoesRepositorio {
  /// Cria o repositório sobre o [api].
  const NotificacoesRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-51: `GET /notificacoes`, com [de] e [ate] opcionais (`AAAA-MM-DD`, hora de Lisboa, aplicados a `criadaEm`).
  Future<List<NotificacaoDto>> listar({String? de, String? ate}) async {
    final corpo = await api.get(
      '/notificacoes',
      parametros: {'de': ?de, 'ate': ?ate},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(NotificacaoDto.fromJson)
        .toList(growable: false);
  }
}
