import 'estado_tratamento.dart';
import 'grau_importancia.dart';

/// Tratamento de uma ficha (OP-27 a OP-31). As datas vêm da API em `AAAA-MM-DD` e os horários em `HH:mm`.
class TratamentoDto {
  /// Cria o tratamento.
  const TratamentoDto({
    required this.id,
    required this.fichaId,
    required this.medicamento,
    required this.dose,
    required this.grauImportancia,
    required this.dataInicio,
    required this.horarios,
    required this.estado,
    this.dataFim,
    this.receitaDocumentoId,
  });

  /// Lê um tratamento da API.
  factory TratamentoDto.fromJson(Map<String, dynamic> json) {
    return TratamentoDto(
      id: json['id'] as String,
      fichaId: json['fichaId'] as String,
      medicamento: json['medicamento'] as String,
      dose: json['dose'] as String,
      grauImportancia: GrauImportancia.fromJson(json['grauImportancia']),
      dataInicio: json['dataInicio'] as String,
      dataFim: json['dataFim'] as String?,
      horarios: (json['horarios'] as List<dynamic>).cast<String>(),
      estado: EstadoTratamento.fromJson(json['estado']),
      receitaDocumentoId: json['receitaDocumentoId'] as String?,
    );
  }

  /// Identificador do tratamento.
  final String id;

  /// Ficha do tratamento.
  final String fichaId;

  /// Nome do medicamento.
  final String medicamento;

  /// Dose (texto livre, por exemplo «500 mg»).
  final String dose;

  /// Grau de importância.
  final GrauImportancia grauImportancia;

  /// Data de início.
  final String dataInicio;

  /// Data de fim prevista; `null` num tratamento contínuo.
  final String? dataFim;

  /// Horas de toma por dia.
  final List<String> horarios;

  /// Estado.
  final EstadoTratamento estado;

  /// Documento da receita médica, se houver.
  final String? receitaDocumentoId;

  /// Indica se o tratamento está em curso.
  bool get emCurso => estado == EstadoTratamento.emCurso;
}
