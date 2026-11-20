/// Grau de importância de um tratamento: um só valor (RF-051).
enum GrauImportancia {
  /// Leve.
  leve('Leve'),

  /// Moderado.
  moderado('Moderado'),

  /// Grave.
  grave('Grave');

  const GrauImportancia(this.valor);

  /// Valor usado pela API, que é também o texto apresentado.
  final String valor;

  /// Lê o valor da API.
  static GrauImportancia fromJson(Object? valor) =>
      GrauImportancia.values.firstWhere(
        (grau) => grau.valor == valor,
        orElse: () =>
            throw FormatException('Grau de importância desconhecido: $valor'),
      );
}
