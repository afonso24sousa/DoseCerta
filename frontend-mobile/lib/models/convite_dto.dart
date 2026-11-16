import 'estado_convite.dart';
import 'papel.dart';

/// Convite enviado (OP-19) ou recebido (OP-20).
class ConviteDto {
  /// Cria o convite.
  const ConviteDto({
    required this.id,
    required this.fichaId,
    required this.nomeFicha,
    required this.papelOferecido,
    required this.paraPrincipal,
    required this.convidadoPor,
    required this.estado,
    required this.criadoEm,
  });

  /// Lê um convite da API.
  factory ConviteDto.fromJson(Map<String, dynamic> json) {
    return ConviteDto(
      id: json['id'] as String,
      fichaId: json['fichaId'] as String,
      nomeFicha: json['nomeFicha'] as String,
      papelOferecido: Papel.fromJson(json['papelOferecido']),
      paraPrincipal: json['paraPrincipal'] as bool,
      convidadoPor: json['convidadoPor'] as String,
      estado: EstadoConvite.fromJson(json['estado']),
      criadoEm: json['criadoEm'] as String,
    );
  }

  /// Identificador do convite.
  final String id;

  /// Identificador da ficha.
  final String fichaId;

  /// Nome do utente da ficha.
  final String nomeFicha;

  /// Papel oferecido ao convidado.
  final Papel papelOferecido;

  /// Indica se o convite é para Cuidador principal (RF-025).
  final bool paraPrincipal;

  /// Nome de quem convidou.
  final String convidadoPor;

  /// Estado do convite.
  final EstadoConvite estado;

  /// Instante da criação, em ISO 8601 com desvio.
  final String criadoEm;
}
