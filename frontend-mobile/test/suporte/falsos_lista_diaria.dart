import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/lista_diaria_repositorio.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/item_lista_diaria_dto.dart';
import 'package:dosecerta/models/tipo_item_agenda.dart';

/// Toma da lista diária de exemplo (dados fictícios da documentação da API, OP-50).
ItemListaDiariaDto tomaDiariaExemplo({
  String id = 't-1',
  String nomeFicha = 'Maria Fictícia Santos',
  String titulo = 'Metformina 500 mg',
  String previstaEm = '2026-11-09T08:00:00+00:00',
  EstadoToma estado = EstadoToma.pendente,
  bool? confirmavel,
  String? confirmadaPor,
  String? confirmadaEm,
}) {
  return ItemListaDiariaDto(
    tipo: TipoItemAgenda.toma,
    id: id,
    fichaId: 'f-1',
    nomeFicha: nomeFicha,
    titulo: titulo,
    previstaEm: previstaEm,
    estado: estado,
    confirmadaPor: confirmadaPor,
    confirmadaEm: confirmadaEm,
    confirmavel: confirmavel ?? estado == EstadoToma.pendente,
  );
}

/// Compromisso da lista diária de exemplo (OP-50).
ItemListaDiariaDto compromissoDiarioExemplo({
  String id = 'c-1',
  String titulo = 'Consulta de rotina',
  String previstaEm = '2026-11-09T10:30:00+00:00',
}) {
  return ItemListaDiariaDto(
    tipo: TipoItemAgenda.compromisso,
    id: id,
    fichaId: 'f-1',
    nomeFicha: 'Maria Fictícia Santos',
    titulo: titulo,
    previstaEm: previstaEm,
    confirmavel: false,
  );
}

/// Repositório da lista diária falso: devolve [itens] ou lança [erro] e guarda as datas pedidas.
class ListaDiariaRepositorioFalso implements ListaDiariaRepositorio {
  /// Itens devolvidos pela OP-50.
  List<ItemListaDiariaDto> itens = [];

  /// Erro da OP-50.
  Object? erro;

  /// Datas pedidas (`null` quando o pedido não leva a data).
  final List<String?> datas = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<ItemListaDiariaDto>> consultar({String? data}) async {
    datas.add(data);
    final e = erro;
    if (e != null) {
      throw e;
    }
    return List.of(itens);
  }
}
