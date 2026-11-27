import '../core/api_client.dart';
import '../models/nova_nota_dto.dart';
import '../models/nova_ocorrencia_dto.dart';
import '../models/registo_dto.dart';
import '../models/tipo_registo.dart';

/// Ocorrências e notas partilhadas da ficha (OP-46 a OP-49). Sem regras de negócio: as recusas (por exemplo o
/// Familiar, RF-091 e RF-095, ou a ficha arquivada, RF-120) chegam do servidor como `ProblemaApi`. Não tem
/// alteração nem eliminação, porque a API não as oferece (RF-090, RF-091).
class RegistosRepositorio {
  /// Cria o repositório sobre o [api].
  const RegistosRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-46: `POST /fichas/{fichaId}/ocorrencias` (RF-090).
  Future<RegistoDto> registarOcorrencia(
    String fichaId,
    NovaOcorrenciaDto dados,
  ) async {
    final corpo = await api.post(
      '/fichas/$fichaId/ocorrencias',
      corpo: dados.toJson(),
    );
    return RegistoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-47: `POST /fichas/{fichaId}/notas` (RF-091).
  Future<RegistoDto> registarNota(String fichaId, NovaNotaDto dados) async {
    final corpo = await api.post(
      '/fichas/$fichaId/notas',
      corpo: dados.toJson(),
    );
    return RegistoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-48: `GET /fichas/{fichaId}/registos?tipo=Ocorrencia`, com [de] e [ate] (`AAAA-MM-DD`) opcionais.
  Future<List<RegistoDto>> listarOcorrencias(
    String fichaId, {
    String? de,
    String? ate,
  }) {
    return _listar(fichaId, TipoRegisto.ocorrencia, de, ate);
  }

  /// OP-49: `GET /fichas/{fichaId}/registos?tipo=Nota`, com [de] e [ate] (`AAAA-MM-DD`) opcionais.
  Future<List<RegistoDto>> listarNotas(
    String fichaId, {
    String? de,
    String? ate,
  }) {
    return _listar(fichaId, TipoRegisto.nota, de, ate);
  }

  Future<List<RegistoDto>> _listar(
    String fichaId,
    TipoRegisto tipo,
    String? de,
    String? ate,
  ) async {
    final corpo = await api.get(
      '/fichas/$fichaId/registos',
      parametros: {'tipo': tipo.valor, 'de': ?de, 'ate': ?ate},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(RegistoDto.fromJson)
        .toList(growable: false);
  }
}
