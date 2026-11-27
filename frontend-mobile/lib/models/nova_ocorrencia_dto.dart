/// Corpo de OP-46 Registar ocorrência (RF-090).
class NovaOcorrenciaDto {
  /// Cria o corpo.
  const NovaOcorrenciaDto({required this.descricao, required this.ocorridaEm});

  /// Descrição da ocorrência, até 2000 caracteres.
  final String descricao;

  /// Data e hora em que aconteceu, em ISO 8601 com o desvio de Lisboa.
  final String ocorridaEm;

  /// Corpo JSON do pedido.
  Map<String, Object?> toJson() => {
    'descricao': descricao,
    'ocorridaEm': ocorridaEm,
  };
}
