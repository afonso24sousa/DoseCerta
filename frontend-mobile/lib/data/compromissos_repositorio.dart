import '../core/api_client.dart';
import '../models/compromisso_dto.dart';
import '../models/dados_compromisso_dto.dart';

/// Compromissos de uma ficha (OP-41 a OP-44). Sem regras de negócio: o papel, o estado da ficha e o aviso
/// (RF-117) são decididos pelo servidor.
class CompromissosRepositorio {
  /// Cria o repositório sobre o [api].
  const CompromissosRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-41: `POST /fichas/{fichaId}/compromissos`.
  Future<CompromissoDto> registar(
    String fichaId,
    DadosCompromissoDto dados,
  ) async {
    final corpo = await api.post(
      '/fichas/$fichaId/compromissos',
      corpo: dados.toJson(),
    );
    return CompromissoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-42: `GET /fichas/{fichaId}/compromissos`, com `de` e `ate` (`AAAA-MM-DD`) opcionais, por ordem de data.
  Future<List<CompromissoDto>> listar(
    String fichaId, {
    String? de,
    String? ate,
  }) async {
    final corpo = await api.get(
      '/fichas/$fichaId/compromissos',
      parametros: {'de': ?de, 'ate': ?ate},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(CompromissoDto.fromJson)
        .toList(growable: false);
  }

  /// OP-43: `PUT /compromissos/{compromissoId}`, com todos os campos.
  Future<CompromissoDto> editar(
    String compromissoId,
    DadosCompromissoDto dados,
  ) async {
    final corpo = await api.put(
      '/compromissos/$compromissoId',
      corpo: dados.toJson(),
    );
    return CompromissoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-44: `DELETE /compromissos/{compromissoId}` (`204`).
  Future<void> remover(String compromissoId) async {
    await api.delete('/compromissos/$compromissoId');
  }
}
