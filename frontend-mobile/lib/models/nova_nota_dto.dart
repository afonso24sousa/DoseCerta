/// Corpo de OP-47 Registar nota partilhada (RF-091).
class NovaNotaDto {
  /// Cria o corpo.
  const NovaNotaDto({required this.descricao});

  /// Texto da nota, até 2000 caracteres.
  final String descricao;

  /// Corpo JSON do pedido.
  Map<String, Object?> toJson() => {'descricao': descricao};
}
