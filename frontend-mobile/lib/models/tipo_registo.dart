/// Tipo de registo da ficha (modelo de classes do frontend mobile v02; RF-090, RF-091).
enum TipoRegisto {
  /// Ocorrência, visível a Cuidadores e Familiares (RF-090, RF-094).
  ocorrencia('Ocorrencia'),

  /// Nota partilhada, dirigida só aos Cuidadores (RF-091, RF-095).
  nota('Nota');

  const TipoRegisto(this.valor);

  /// Valor usado pela API, também no parâmetro `tipo` de OP-48 e OP-49.
  final String valor;

  /// Lê o valor da API.
  static TipoRegisto fromJson(Object? valor) => TipoRegisto.values.firstWhere(
    (tipo) => tipo.valor == valor,
    orElse: () => throw FormatException('Tipo de registo desconhecido: $valor'),
  );
}
