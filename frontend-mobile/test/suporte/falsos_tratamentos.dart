import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/tratamentos_repositorio.dart';
import 'package:dosecerta/models/dados_tratamento_dto.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/ficheiro_escolhido.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/tratamento_dto.dart';

/// Tratamento de exemplo (dados fictícios da documentação da API, OP-27).
TratamentoDto tratamentoExemplo({
  String id = 't-1',
  String fichaId = 'f-1',
  String medicamento = 'Metformina',
  String dose = '500 mg',
  GrauImportancia grau = GrauImportancia.moderado,
  String dataInicio = '2026-10-07',
  String? dataFim = '2026-12-05',
  List<String> horarios = const ['08:00', '20:00'],
  EstadoTratamento estado = EstadoTratamento.emCurso,
  String? receitaDocumentoId,
}) {
  return TratamentoDto(
    id: id,
    fichaId: fichaId,
    medicamento: medicamento,
    dose: dose,
    grauImportancia: grau,
    dataInicio: dataInicio,
    dataFim: dataFim,
    horarios: horarios,
    estado: estado,
    receitaDocumentoId: receitaDocumentoId,
  );
}

/// Repositório de tratamentos falso: guarda os pedidos e devolve ou lança o que o teste indicar.
class TratamentosRepositorioFalso implements TratamentosRepositorio {
  /// Tratamentos por identificador.
  final Map<String, TratamentoDto> tratamentos = {};

  Object? erroListar;
  Object? erroConsultar;
  Object? erroGuardar;
  Object? erroTerminar;

  DadosTratamentoDto? ultimosDados;
  FicheiroEscolhido? ultimaReceita;
  int registos = 0;
  int atualizacoes = 0;
  int terminos = 0;
  int consultas = 0;

  /// Junta [tratamento] aos tratamentos da ficha.
  void juntar(TratamentoDto tratamento) =>
      tratamentos[tratamento.id] = tratamento;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<TratamentoDto> registar(
    String fichaId,
    DadosTratamentoDto dados, {
    FicheiroEscolhido? receita,
  }) async {
    registos++;
    ultimosDados = dados;
    ultimaReceita = receita;
    if (erroGuardar != null) {
      throw erroGuardar!;
    }
    final novo = TratamentoDto(
      id: 't-novo',
      fichaId: fichaId,
      medicamento: dados.medicamento!,
      dose: dados.dose!,
      grauImportancia: dados.grauImportancia!,
      dataInicio: dados.dataInicio!,
      dataFim: dados.dataFim,
      horarios: dados.horarios!,
      estado: EstadoTratamento.emCurso,
      receitaDocumentoId: receita == null ? null : 'd-receita',
    );
    juntar(novo);
    return novo;
  }

  @override
  Future<List<TratamentoDto>> listar(
    String fichaId, {
    EstadoTratamento? estado,
  }) async {
    if (erroListar != null) {
      throw erroListar!;
    }
    return tratamentos.values
        .where((t) => t.fichaId == fichaId)
        .where((t) => estado == null || t.estado == estado)
        .toList();
  }

  @override
  Future<TratamentoDto> consultar(String tratamentoId) async {
    consultas++;
    if (erroConsultar != null) {
      throw erroConsultar!;
    }
    return tratamentos[tratamentoId] ?? tratamentoExemplo(id: tratamentoId);
  }

  @override
  Future<TratamentoDto> atualizar(
    String tratamentoId,
    DadosTratamentoDto dados, {
    FicheiroEscolhido? receita,
  }) async {
    atualizacoes++;
    ultimosDados = dados;
    ultimaReceita = receita;
    if (erroGuardar != null) {
      throw erroGuardar!;
    }
    final atual = tratamentos[tratamentoId]!;
    final atualizado = TratamentoDto(
      id: atual.id,
      fichaId: atual.fichaId,
      medicamento: dados.medicamento ?? atual.medicamento,
      dose: dados.dose ?? atual.dose,
      grauImportancia: dados.grauImportancia ?? atual.grauImportancia,
      dataInicio: atual.dataInicio,
      dataFim: dados.dataFim ?? atual.dataFim,
      horarios: dados.horarios ?? atual.horarios,
      estado: atual.estado,
      receitaDocumentoId: atual.receitaDocumentoId,
    );
    juntar(atualizado);
    return atualizado;
  }

  @override
  Future<TratamentoDto> terminar(String tratamentoId) async {
    terminos++;
    if (erroTerminar != null) {
      throw erroTerminar!;
    }
    final atual = tratamentos[tratamentoId]!;
    final terminado = tratamentoExemplo(
      id: atual.id,
      fichaId: atual.fichaId,
      medicamento: atual.medicamento,
      dose: atual.dose,
      grau: atual.grauImportancia,
      dataInicio: atual.dataInicio,
      dataFim: atual.dataFim,
      horarios: atual.horarios,
      estado: EstadoTratamento.terminado,
      receitaDocumentoId: atual.receitaDocumentoId,
    );
    juntar(terminado);
    return terminado;
  }
}
