/// Papel do Utilizador numa ficha (matriz de permissões, secção 3). O Cuidador principal não é um papel: é um
/// Cuidador com `souPrincipal` verdadeiro.
enum Papel {
  /// Escrita completa sobre a ficha (RF-047).
  cuidador('Cuidador'),

  /// Só consulta (RF-048).
  familiar('Familiar');

  const Papel(this.valor);

  /// Valor usado pela API.
  final String valor;

  /// Lê o valor da API.
  static Papel fromJson(Object? valor) => Papel.values.firstWhere(
    (papel) => papel.valor == valor,
    orElse: () => throw FormatException('Papel desconhecido: $valor'),
  );
}
