import '../core/api_client.dart';
import '../models/toma_calendario_dto.dart';

/// Tomas das 48 horas seguintes para o dispositivo (OP-34; só no frontend mobile). O servidor só devolve as tomas
/// `Pendente` das fichas ativas em que o Utilizador é Cuidador (RF-061, RF-069).
class CalendarioRepositorio {
  /// Cria o repositório sobre o [api].
  const CalendarioRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-34: `GET /calendario/tomas`.
  Future<List<TomaCalendarioDto>> sincronizar() async {
    final corpo = await api.get('/calendario/tomas');
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(TomaCalendarioDto.fromJson)
        .toList(growable: false);
  }
}
