/// Valor de uma lista fechada de necessidades (OP-08; RF-021).
class ItemDominioDto {
  /// Cria o valor.
  const ItemDominioDto({required this.id, required this.designacao});

  /// Lê um valor da API.
  factory ItemDominioDto.fromJson(Map<String, dynamic> json) {
    return ItemDominioDto(
      id: json['id'] as int,
      designacao: json['designacao'] as String,
    );
  }

  /// Lê uma lista de valores da API (vazia se o campo faltar).
  static List<ItemDominioDto> lista(Object? json) =>
      (json as List<dynamic>? ?? const [])
          .cast<Map<String, dynamic>>()
          .map(ItemDominioDto.fromJson)
          .toList(growable: false);

  /// Identificador do valor.
  final int id;

  /// Texto do valor.
  final String designacao;
}
