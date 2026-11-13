import 'estado_ficha.dart';
import 'item_dominio_dto.dart';
import 'papel.dart';

/// Ficha completa, com o papel do Utilizador (OP-09, OP-10 e OP-11).
class FichaDto {
  /// Cria a ficha.
  const FichaDto({
    required this.id,
    required this.nomeCompleto,
    required this.dataNascimento,
    required this.contactoEmergencia,
    required this.eProprio,
    required this.estado,
    required this.grauAutonomia,
    required this.condicoesSaude,
    required this.alergias,
    required this.meuPapel,
    required this.souPrincipal,
  });

  /// Lê a resposta de OP-09, OP-10 ou OP-11.
  factory FichaDto.fromJson(Map<String, dynamic> json) {
    final grau = json['grauAutonomia'];
    return FichaDto(
      id: json['id'] as String,
      nomeCompleto: json['nomeCompleto'] as String,
      dataNascimento: json['dataNascimento'] as String,
      contactoEmergencia: json['contactoEmergencia'] as String,
      eProprio: json['eProprio'] as bool,
      estado: EstadoFicha.fromJson(json['estado']),
      grauAutonomia: grau is Map<String, dynamic>
          ? ItemDominioDto.fromJson(grau)
          : null,
      condicoesSaude: ItemDominioDto.lista(json['condicoesSaude']),
      alergias: ItemDominioDto.lista(json['alergias']),
      meuPapel: Papel.fromJson(json['meuPapel']),
      souPrincipal: json['souPrincipal'] as bool,
    );
  }

  /// Identificador da ficha.
  final String id;

  /// Nome completo do utente.
  final String nomeCompleto;

  /// Data de nascimento (`AAAA-MM-DD`).
  final String dataNascimento;

  /// Contacto de emergência.
  final String contactoEmergencia;

  /// Indica se é uma ficha própria.
  final bool eProprio;

  /// Estado da ficha.
  final EstadoFicha estado;

  /// Grau de autonomia/mobilidade, se indicado.
  final ItemDominioDto? grauAutonomia;

  /// Condições de saúde relevantes.
  final List<ItemDominioDto> condicoesSaude;

  /// Alergias.
  final List<ItemDominioDto> alergias;

  /// Papel do Utilizador nesta ficha.
  final Papel meuPapel;

  /// Indica se o Utilizador é o Cuidador principal.
  final bool souPrincipal;

  /// Indica se a ficha está ativa.
  bool get ativa => estado == EstadoFicha.ativa;

  /// Indica se o Utilizador é Cuidador nesta ficha (escrita completa, RF-047).
  bool get souCuidador => meuPapel == Papel.cuidador;
}
