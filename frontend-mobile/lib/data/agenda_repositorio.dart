import '../core/api_client.dart';
import '../models/item_agenda_dto.dart';
import '../models/vista_agenda.dart';

/// Agenda do Utilizador (OP-45): compromissos e tomas previstas das fichas ativas, por ordem de início (RF-083).
class AgendaRepositorio {
  /// Cria o repositório sobre o [api].
  const AgendaRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-45: `GET /agenda?vista=dia|semana|mes&data=AAAA-MM-DD`.
  Future<List<ItemAgendaDto>> consultar(VistaAgenda vista, String data) async {
    final corpo = await api.get(
      '/agenda',
      parametros: {'vista': vista.valor, 'data': data},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(ItemAgendaDto.fromJson)
        .toList(growable: false);
  }
}
