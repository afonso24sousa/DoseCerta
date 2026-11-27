import '../core/api_client.dart';
import '../models/item_lista_diaria_dto.dart';

/// Lista de tarefas diária (OP-50). Sem regras de negócio: o servidor escolhe as fichas (ativas, em que o
/// Utilizador é Cuidador), junta as tomas pendentes de ontem e decide o que é confirmável (RF-100, RF-087).
class ListaDiariaRepositorio {
  /// Cria o repositório sobre o [api].
  const ListaDiariaRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-50: `GET /lista-diaria`, com a [data] (`AAAA-MM-DD`) opcional; sem ela, o servidor usa o dia de hoje em
  /// Lisboa (RNF-047). Um só pedido para a lista inteira (RNF-010).
  Future<List<ItemListaDiariaDto>> consultar({String? data}) async {
    final corpo = await api.get('/lista-diaria', parametros: {'data': ?data});
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(ItemListaDiariaDto.fromJson)
        .toList(growable: false);
  }
}
