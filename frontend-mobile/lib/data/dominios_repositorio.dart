import '../core/api_client.dart';
import '../models/dominios_necessidades_dto.dart';

/// Listas fechadas de necessidades (OP-08; RF-021).
class DominiosRepositorio {
  /// Cria o repositório sobre o [api].
  const DominiosRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-08: `GET /dominios/necessidades`.
  Future<DominiosNecessidadesDto> obterNecessidades() async {
    final corpo = await api.get('/dominios/necessidades');
    return DominiosNecessidadesDto.fromJson(corpo! as Map<String, dynamic>);
  }
}
