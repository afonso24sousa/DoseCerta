import '../core/api_client.dart';
import '../models/papel.dart';
import '../models/pessoa_ficha_dto.dart';

/// Pessoas e papéis da ficha (OP-18 e OP-23 a OP-26). Sem regras de negócio: as recusas (RF-033, RF-034,
/// RF-133, RF-134) chegam do servidor como `ProblemaApi`.
class PessoasRepositorio {
  /// Cria o repositório sobre o [api].
  const PessoasRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-18: `GET /fichas/{fichaId}/pessoas`.
  Future<List<PessoaFichaDto>> listar(String fichaId) async {
    final corpo = await api.get('/fichas/$fichaId/pessoas');
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(PessoaFichaDto.fromJson)
        .toList(growable: false);
  }

  /// OP-23: `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` com o papel Cuidador (RF-049).
  Future<PessoaFichaDto> promover(String fichaId, String utilizadorId) {
    return _alterarPapel(fichaId, utilizadorId, Papel.cuidador);
  }

  /// OP-24: `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` com o papel Familiar (RF-130).
  Future<PessoaFichaDto> despromover(String fichaId, String utilizadorId) {
    return _alterarPapel(fichaId, utilizadorId, Papel.familiar);
  }

  /// OP-25: `DELETE /fichas/{fichaId}/pessoas/{utilizadorId}` (RF-131).
  Future<void> remover(String fichaId, String utilizadorId) async {
    await api.delete('/fichas/$fichaId/pessoas/$utilizadorId');
  }

  /// OP-26: `POST /fichas/{fichaId}/cuidador-principal` (RF-132).
  Future<PessoaFichaDto> transferirPrincipal(
    String fichaId,
    String utilizadorId,
  ) async {
    final corpo = await api.post(
      '/fichas/$fichaId/cuidador-principal',
      corpo: {'utilizadorId': utilizadorId},
    );
    return PessoaFichaDto.fromJson(corpo! as Map<String, dynamic>);
  }

  Future<PessoaFichaDto> _alterarPapel(
    String fichaId,
    String utilizadorId,
    Papel papel,
  ) async {
    final corpo = await api.put(
      '/fichas/$fichaId/pessoas/$utilizadorId/papel',
      corpo: {'papel': papel.valor},
    );
    return PessoaFichaDto.fromJson(corpo! as Map<String, dynamic>);
  }
}
