/// Corpo de OP-41 (registar) e de OP-43 (editar, com todos os campos; RF-080, RF-081, RF-116).
class DadosCompromissoDto {
  /// Cria os dados.
  const DadosCompromissoDto({
    required this.dataHora,
    required this.local,
    required this.motivo,
    this.antecedenciaAvisoMinutos = antecedenciaPorOmissao,
  });

  /// Antecedências do aviso aceites pela API, em minutos: 30 minutos, 1, 2, 24 e 48 horas (RF-116).
  static const List<int> antecedencias = [30, 60, 120, 1440, 2880];

  /// Antecedência por omissão: 24 horas (RF-080).
  static const int antecedenciaPorOmissao = 1440;

  /// Instante, em ISO 8601 com o desvio de Lisboa.
  final String dataHora;

  /// Local (até 150 caracteres).
  final String local;

  /// Motivo (até 250 caracteres).
  final String motivo;

  /// Antecedência do aviso, em minutos.
  final int antecedenciaAvisoMinutos;

  /// Corpo do pedido.
  Map<String, Object?> toJson() => {
    'dataHora': dataHora,
    'local': local,
    'motivo': motivo,
    'antecedenciaAvisoMinutos': antecedenciaAvisoMinutos,
  };
}
