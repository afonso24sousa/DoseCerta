import 'estado_toma.dart';
import 'grau_importancia.dart';
import 'pessoa_ref_dto.dart';

/// Toma de um tratamento (OP-32 e OP-33).
class TomaDto {
  /// Cria a toma.
  const TomaDto({
    required this.id,
    required this.tratamentoId,
    required this.medicamento,
    required this.dose,
    required this.grauImportancia,
    required this.previstaEm,
    required this.estado,
    this.confirmadaPor,
    this.confirmadaEm,
  });

  /// Lê uma toma da API.
  factory TomaDto.fromJson(Map<String, dynamic> json) {
    final confirmadaPor = json['confirmadaPor'];
    return TomaDto(
      id: json['id'] as String,
      tratamentoId: json['tratamentoId'] as String,
      medicamento: json['medicamento'] as String,
      dose: json['dose'] as String,
      grauImportancia: GrauImportancia.fromJson(json['grauImportancia']),
      previstaEm: json['previstaEm'] as String,
      estado: EstadoToma.fromJson(json['estado']),
      confirmadaPor: confirmadaPor == null
          ? null
          : PessoaRefDto.fromJson(confirmadaPor as Map<String, dynamic>),
      confirmadaEm: json['confirmadaEm'] as String?,
    );
  }

  /// Identificador da toma.
  final String id;

  /// Identificador do tratamento que gerou a toma.
  final String tratamentoId;

  /// Medicamento.
  final String medicamento;

  /// Dose.
  final String dose;

  /// Grau de importância do tratamento.
  final GrauImportancia grauImportancia;

  /// Instante previsto, em ISO 8601 com o desvio de Lisboa.
  final String previstaEm;

  /// Estado atual.
  final EstadoToma estado;

  /// Quem confirmou, nas tomas confirmadas (RF-065).
  final PessoaRefDto? confirmadaPor;

  /// Instante da confirmação, em ISO 8601 com o desvio de Lisboa (RF-065).
  final String? confirmadaEm;
}
