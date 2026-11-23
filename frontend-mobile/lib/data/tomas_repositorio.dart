import '../core/api_client.dart';
import '../models/toma_dto.dart';

/// Tomas e confirmação (OP-32 e OP-33). Sem regras de negócio: a janela de confirmação, o estado resultante e as
/// recusas (RF-064, RF-067, RF-068, RF-105) são decididos pelo servidor.
class TomasRepositorio {
  /// Cria o repositório sobre o [api].
  const TomasRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-32: `GET /fichas/{fichaId}/tomas?de=AAAA-MM-DD&ate=AAAA-MM-DD`, com no máximo 31 dias (RF-063).
  Future<List<TomaDto>> listar(String fichaId, String de, String ate) async {
    final corpo = await api.get(
      '/fichas/$fichaId/tomas',
      parametros: {'de': de, 'ate': ate},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(TomaDto.fromJson)
        .toList(growable: false);
  }

  /// OP-33: `POST /tomas/{tomaId}/confirmacao`, sem corpo; devolve a toma com o novo estado (RF-064, RF-065).
  Future<TomaDto> confirmar(String tomaId) async {
    final corpo = await api.post('/tomas/$tomaId/confirmacao');
    return TomaDto.fromJson(corpo! as Map<String, dynamic>);
  }
}
