import 'item_dominio_dto.dart';

/// Listas fechadas de condições de saúde, alergias e graus de autonomia/mobilidade (OP-08; RF-021).
class DominiosNecessidadesDto {
  /// Cria as listas.
  const DominiosNecessidadesDto({
    required this.condicoesSaude,
    required this.alergias,
    required this.grausAutonomia,
  });

  /// Lê a resposta de OP-08.
  factory DominiosNecessidadesDto.fromJson(Map<String, dynamic> json) {
    return DominiosNecessidadesDto(
      condicoesSaude: ItemDominioDto.lista(json['condicoesSaude']),
      alergias: ItemDominioDto.lista(json['alergias']),
      grausAutonomia: ItemDominioDto.lista(json['grausAutonomia']),
    );
  }

  /// Condições de saúde relevantes.
  final List<ItemDominioDto> condicoesSaude;

  /// Alergias.
  final List<ItemDominioDto> alergias;

  /// Graus de autonomia/mobilidade.
  final List<ItemDominioDto> grausAutonomia;
}
