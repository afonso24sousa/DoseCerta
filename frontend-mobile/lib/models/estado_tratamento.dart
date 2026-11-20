/// Estado de um tratamento (RF-056, RF-057).
enum EstadoTratamento {
  /// Em curso: pode ser editado e terminado.
  emCurso('EmCurso', 'Em curso'),

  /// Terminado pelo Cuidador ou por ter passado a data de fim; fica só para consulta.
  terminado('Terminado', 'Terminado');

  const EstadoTratamento(this.valor, this.rotulo);

  /// Valor usado pela API.
  final String valor;

  /// Texto apresentado.
  final String rotulo;

  /// Lê o valor da API.
  static EstadoTratamento fromJson(Object? valor) =>
      EstadoTratamento.values.firstWhere(
        (estado) => estado.valor == valor,
        orElse: () =>
            throw FormatException('Estado de tratamento desconhecido: $valor'),
      );
}
