import 'estado_ficha.dart';
import 'papel.dart';

/// Linha da lista de fichas (OP-07).
class FichaResumoDto {
  /// Cria a linha.
  const FichaResumoDto({
    required this.id,
    required this.nomeCompleto,
    required this.eProprio,
    required this.estado,
    required this.meuPapel,
    required this.souPrincipal,
  });

  /// Lê uma linha de OP-07.
  factory FichaResumoDto.fromJson(Map<String, dynamic> json) {
    return FichaResumoDto(
      id: json['id'] as String,
      nomeCompleto: json['nomeCompleto'] as String,
      eProprio: json['eProprio'] as bool,
      estado: EstadoFicha.fromJson(json['estado']),
      meuPapel: Papel.fromJson(json['meuPapel']),
      souPrincipal: json['souPrincipal'] as bool,
    );
  }

  /// Identificador da ficha.
  final String id;

  /// Nome completo do utente.
  final String nomeCompleto;

  /// Indica se é uma ficha própria (de alguém).
  final bool eProprio;

  /// Estado da ficha.
  final EstadoFicha estado;

  /// Papel do Utilizador nesta ficha.
  final Papel meuPapel;

  /// Indica se o Utilizador é o Cuidador principal.
  final bool souPrincipal;

  /// Indica se é a ficha própria do Utilizador. A API não tem este campo: numa ficha própria o titular é sempre
  /// o Cuidador principal e o papel não se transfere (RF-033, RF-034), por isso uma ficha própria em que o
  /// Utilizador é o principal é a dele (decisão D5 da I060).
  bool get eMinhaFichaPropria => eProprio && souPrincipal;
}
