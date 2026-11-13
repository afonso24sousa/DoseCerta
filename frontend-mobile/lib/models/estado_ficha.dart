/// Estado da ficha (RF-120, RF-121).
enum EstadoFicha {
  /// Ficha ativa.
  ativa('Ativa'),

  /// Ficha arquivada: só consulta, reativação e eliminação sem registos.
  arquivada('Arquivada');

  const EstadoFicha(this.valor);

  /// Valor usado pela API.
  final String valor;

  /// Lê o valor da API.
  static EstadoFicha fromJson(Object? valor) => EstadoFicha.values.firstWhere(
    (estado) => estado.valor == valor,
    orElse: () => throw FormatException('Estado de ficha desconhecido: $valor'),
  );
}
