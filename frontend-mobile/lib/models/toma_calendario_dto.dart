/// Toma `Pendente` das 48 horas seguintes, sincronizada e guardada no dispositivo para o aviso local (OP-34;
/// RF-061, RF-062).
class TomaCalendarioDto {
  /// Cria a toma sincronizada.
  const TomaCalendarioDto({
    required this.tomaId,
    required this.fichaId,
    required this.nomeFicha,
    required this.medicamento,
    required this.dose,
    required this.previstaEm,
  });

  /// Lê uma toma da API ou do armazenamento local.
  factory TomaCalendarioDto.fromJson(Map<String, dynamic> json) {
    return TomaCalendarioDto(
      tomaId: json['tomaId'] as String,
      fichaId: json['fichaId'] as String,
      nomeFicha: json['nomeFicha'] as String,
      medicamento: json['medicamento'] as String,
      dose: json['dose'] as String,
      previstaEm: json['previstaEm'] as String,
    );
  }

  /// Identificador da toma.
  final String tomaId;

  /// Identificador da ficha.
  final String fichaId;

  /// Nome do utente da ficha.
  final String nomeFicha;

  /// Medicamento.
  final String medicamento;

  /// Dose.
  final String dose;

  /// Instante previsto, em ISO 8601 com o desvio de Lisboa.
  final String previstaEm;

  /// Converte para JSON, para guardar no dispositivo.
  Map<String, dynamic> toJson() => {
    'tomaId': tomaId,
    'fichaId': fichaId,
    'nomeFicha': nomeFicha,
    'medicamento': medicamento,
    'dose': dose,
    'previstaEm': previstaEm,
  };
}
