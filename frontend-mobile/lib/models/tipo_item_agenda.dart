/// Tipo de um evento da agenda e da lista diária (OP-45, OP-50).
enum TipoItemAgenda {
  /// Toma prevista de um tratamento.
  toma('Toma'),

  /// Compromisso (consulta, exame ou outro; RF-080).
  compromisso('Compromisso');

  const TipoItemAgenda(this.valor);

  /// Valor usado pela API.
  final String valor;

  /// Lê o valor da API.
  static TipoItemAgenda fromJson(Object? valor) =>
      TipoItemAgenda.values.firstWhere(
        (tipo) => tipo.valor == valor,
        orElse: () =>
            throw FormatException('Tipo de evento desconhecido: $valor'),
      );
}
