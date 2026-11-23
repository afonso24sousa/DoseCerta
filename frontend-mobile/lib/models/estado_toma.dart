/// Estado de uma toma (diagrama de estados da toma v02; RF-064, RF-066 a RF-068).
enum EstadoToma {
  /// Ainda por confirmar; mantém-se até ser confirmada ou passar a não confirmada.
  pendente('Pendente'),

  /// Confirmada até ao fim da tolerância do grau (RF-067).
  confirmada('Confirmada'),

  /// Confirmada depois da tolerância do grau (RF-067).
  confirmadaComAtraso('ConfirmadaComAtraso'),

  /// Estado final: passaram 24 horas sem confirmação (RF-068).
  naoConfirmada('NaoConfirmada');

  const EstadoToma(this.valor);

  /// Valor usado pela API.
  final String valor;

  /// Lê o valor da API.
  static EstadoToma fromJson(Object? valor) => EstadoToma.values.firstWhere(
    (estado) => estado.valor == valor,
    orElse: () => throw FormatException('Estado de toma desconhecido: $valor'),
  );
}
