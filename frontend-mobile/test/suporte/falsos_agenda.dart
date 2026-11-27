import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/agenda_repositorio.dart';
import 'package:dosecerta/data/compromissos_repositorio.dart';
import 'package:dosecerta/models/compromisso_dto.dart';
import 'package:dosecerta/models/dados_compromisso_dto.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/item_agenda_dto.dart';
import 'package:dosecerta/models/tipo_item_agenda.dart';
import 'package:dosecerta/models/vista_agenda.dart';

/// Compromisso de exemplo (dados fictícios da documentação da API, OP-41).
CompromissoDto compromissoExemplo({
  String id = 'c-1',
  String fichaId = 'f-1',
  String dataHora = '2026-11-24T10:30:00+00:00',
  int antecedencia = 60,
}) {
  return CompromissoDto(
    id: id,
    fichaId: fichaId,
    dataHora: dataHora,
    local: 'Centro de Saúde Fictício',
    motivo: 'Consulta de rotina',
    antecedenciaAvisoMinutos: antecedencia,
  );
}

/// Toma e compromisso da agenda de exemplo (OP-45).
List<ItemAgendaDto> agendaExemplo() => const [
  ItemAgendaDto(
    id: 't-1',
    tipo: TipoItemAgenda.toma,
    fichaId: 'f-1',
    nomeFicha: 'Maria Fictícia Santos',
    titulo: 'Metformina 500 mg',
    inicio: '2026-11-24T08:00:00+00:00',
    estado: EstadoToma.pendente,
    confirmavel: true,
  ),
  ItemAgendaDto(
    id: 'c-1',
    tipo: TipoItemAgenda.compromisso,
    fichaId: 'f-1',
    nomeFicha: 'Maria Fictícia Santos',
    titulo: 'Consulta de rotina',
    inicio: '2026-11-24T10:30:00+00:00',
  ),
];

/// Repositório da agenda falso: guarda os pedidos e devolve [itens] ou lança [erro].
class AgendaRepositorioFalso implements AgendaRepositorio {
  List<ItemAgendaDto> itens = agendaExemplo();
  Object? erro;

  /// Pedidos, como `vista data`.
  final List<String> pedidos = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<ItemAgendaDto>> consultar(VistaAgenda vista, String data) async {
    pedidos.add('${vista.valor} $data');
    final e = erro;
    if (e != null) {
      throw e;
    }
    return List.of(itens);
  }
}

/// Repositório de compromissos falso.
class CompromissosRepositorioFalso implements CompromissosRepositorio {
  List<CompromissoDto> compromissos = [compromissoExemplo()];
  Object? erroGuardar;
  DadosCompromissoDto? registado;
  DadosCompromissoDto? editado;
  final List<String> removidos = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<CompromissoDto> registar(
    String fichaId,
    DadosCompromissoDto dados,
  ) async {
    final e = erroGuardar;
    if (e != null) {
      throw e;
    }
    registado = dados;
    return compromissoExemplo(fichaId: fichaId, dataHora: dados.dataHora);
  }

  @override
  Future<List<CompromissoDto>> listar(
    String fichaId, {
    String? de,
    String? ate,
  }) async => List.of(compromissos);

  @override
  Future<CompromissoDto> editar(
    String compromissoId,
    DadosCompromissoDto dados,
  ) async {
    final e = erroGuardar;
    if (e != null) {
      throw e;
    }
    editado = dados;
    return compromissoExemplo(id: compromissoId, dataHora: dados.dataHora);
  }

  @override
  Future<void> remover(String compromissoId) async =>
      removidos.add(compromissoId);
}
