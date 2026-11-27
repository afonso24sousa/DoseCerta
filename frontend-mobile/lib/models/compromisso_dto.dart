/// Compromisso de uma ficha (OP-41 a OP-43).
class CompromissoDto {
  /// Cria o compromisso.
  const CompromissoDto({
    required this.id,
    required this.fichaId,
    required this.dataHora,
    required this.local,
    required this.motivo,
    required this.antecedenciaAvisoMinutos,
  });

  /// Lê um compromisso da API.
  factory CompromissoDto.fromJson(Map<String, dynamic> json) {
    return CompromissoDto(
      id: json['id'] as String,
      fichaId: json['fichaId'] as String,
      dataHora: json['dataHora'] as String,
      local: json['local'] as String,
      motivo: json['motivo'] as String,
      antecedenciaAvisoMinutos: json['antecedenciaAvisoMinutos'] as int,
    );
  }

  /// Identificador do compromisso.
  final String id;

  /// Identificador da ficha.
  final String fichaId;

  /// Instante, em ISO 8601 com o desvio de Lisboa.
  final String dataHora;

  /// Local.
  final String local;

  /// Motivo.
  final String motivo;

  /// Antecedência do aviso, em minutos (RF-116).
  final int antecedenciaAvisoMinutos;
}
