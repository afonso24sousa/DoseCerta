import 'grau_importancia.dart';

/// Corpo de OP-27 (registar, todos os campos menos [dataFim] são obrigatórios) e de OP-30 (atualizar, só os campos
/// alterados). Os campos `null` não são enviados; em OP-30 o servidor mantém o valor deles (RF-055).
class DadosTratamentoDto {
  /// Cria os dados.
  const DadosTratamentoDto({
    this.medicamento,
    this.dose,
    this.grauImportancia,
    this.dataInicio,
    this.dataFim,
    this.horarios,
  });

  /// Nome do medicamento.
  final String? medicamento;

  /// Dose.
  final String? dose;

  /// Grau de importância.
  final GrauImportancia? grauImportancia;

  /// Data de início (`AAAA-MM-DD`); só no registo.
  final String? dataInicio;

  /// Data de fim (`AAAA-MM-DD`).
  final String? dataFim;

  /// Horas de toma (`HH:mm`).
  final List<String>? horarios;

  /// Indica se não há nada para enviar.
  bool get vazio =>
      medicamento == null &&
      dose == null &&
      grauImportancia == null &&
      dataInicio == null &&
      dataFim == null &&
      horarios == null;

  /// JSON do pedido, sem os campos `null`.
  Map<String, dynamic> toJson() => {
    'medicamento': ?medicamento,
    'dose': ?dose,
    'grauImportancia': ?grauImportancia?.valor,
    'dataInicio': ?dataInicio,
    'dataFim': ?dataFim,
    'horarios': ?horarios,
  };
}
