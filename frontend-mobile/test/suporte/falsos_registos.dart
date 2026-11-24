import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/registos_repositorio.dart';
import 'package:dosecerta/models/nova_nota_dto.dart';
import 'package:dosecerta/models/nova_ocorrencia_dto.dart';
import 'package:dosecerta/models/pessoa_ref_dto.dart';
import 'package:dosecerta/models/registo_dto.dart';
import 'package:dosecerta/models/tipo_registo.dart';

/// Registo de exemplo (dados fictícios da documentação da API, OP-48 e OP-49).
RegistoDto registoExemplo({
  String id = 'r-1',
  TipoRegisto tipo = TipoRegisto.ocorrencia,
  String descricao = 'Queda ligeira sem lesões.',
  String autor = 'Ana Fictícia',
  String? ocorridaEm = '2026-10-07T14:50:00+01:00',
  String criadaEm = '2026-10-07T15:20:00+01:00',
}) {
  return RegistoDto(
    id: id,
    fichaId: 'f-1',
    tipo: tipo,
    descricao: descricao,
    autor: PessoaRefDto(id: 'u-$autor', nome: autor),
    ocorridaEm: tipo == TipoRegisto.nota ? null : ocorridaEm,
    criadaEm: criadaEm,
  );
}

/// Repositório das ocorrências e notas falso: guarda os pedidos e devolve ou lança o que o teste indicar. Os
/// registos criados entram no início da lista, como a mais recente (OP-48, OP-49).
class RegistosRepositorioFalso implements RegistosRepositorio {
  /// Ocorrências da ficha (OP-48).
  final List<RegistoDto> ocorrencias = [];

  /// Notas da ficha (OP-49).
  final List<RegistoDto> notas = [];

  Object? erroListar;
  Object? erroRegistar;

  /// Pedidos de consulta feitos, pelo tipo (`Ocorrencia` ou `Nota`).
  final List<String> consultas = [];

  final List<NovaOcorrenciaDto> ocorrenciasEnviadas = [];
  final List<NovaNotaDto> notasEnviadas = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<RegistoDto> registarOcorrencia(
    String fichaId,
    NovaOcorrenciaDto dados,
  ) async {
    ocorrenciasEnviadas.add(dados);
    if (erroRegistar != null) {
      throw erroRegistar!;
    }
    final registo = registoExemplo(
      id: 'r-novo',
      descricao: dados.descricao,
      autor: 'Eu Fictício',
      ocorridaEm: dados.ocorridaEm,
      criadaEm: '2026-11-09T12:00:00+00:00',
    );
    ocorrencias.insert(0, registo);
    return registo;
  }

  @override
  Future<RegistoDto> registarNota(String fichaId, NovaNotaDto dados) async {
    notasEnviadas.add(dados);
    if (erroRegistar != null) {
      throw erroRegistar!;
    }
    final registo = registoExemplo(
      id: 'r-nova',
      tipo: TipoRegisto.nota,
      descricao: dados.descricao,
      autor: 'Eu Fictício',
      criadaEm: '2026-11-09T12:00:00+00:00',
    );
    notas.insert(0, registo);
    return registo;
  }

  @override
  Future<List<RegistoDto>> listarOcorrencias(
    String fichaId, {
    String? de,
    String? ate,
  }) => _listar(TipoRegisto.ocorrencia, ocorrencias);

  @override
  Future<List<RegistoDto>> listarNotas(
    String fichaId, {
    String? de,
    String? ate,
  }) => _listar(TipoRegisto.nota, notas);

  Future<List<RegistoDto>> _listar(
    TipoRegisto tipo,
    List<RegistoDto> registos,
  ) async {
    consultas.add(tipo.valor);
    if (erroListar != null) {
      throw erroListar!;
    }
    return List.of(registos);
  }
}
