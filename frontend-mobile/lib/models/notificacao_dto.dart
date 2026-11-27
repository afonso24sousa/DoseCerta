import 'estado_envio_email.dart';
import 'tipo_notificacao.dart';

/// Notificação do histórico do Utilizador (OP-51). Os instantes vêm da API em ISO 8601 com o desvio de Lisboa.
class NotificacaoDto {
  /// Cria a notificação.
  const NotificacaoDto({
    required this.id,
    required this.tipo,
    required this.mensagem,
    required this.fichaId,
    required this.nomeFicha,
    required this.criadaEm,
    required this.email,
    this.medicamento,
    this.previstaEm,
  });

  /// Lê uma notificação da API.
  factory NotificacaoDto.fromJson(Map<String, dynamic> json) {
    return NotificacaoDto(
      id: json['id'] as String,
      tipo: TipoNotificacao.fromJson(json['tipo']),
      mensagem: json['mensagem'] as String,
      fichaId: json['fichaId'] as String,
      nomeFicha: json['nomeFicha'] as String,
      medicamento: json['medicamento'] as String?,
      previstaEm: json['previstaEm'] as String?,
      criadaEm: json['criadaEm'] as String,
      email: EstadoEnvioEmail.fromJson(json['email']),
    );
  }

  /// Identificador da notificação.
  final String id;

  /// Tipo.
  final TipoNotificacao tipo;

  /// Mensagem.
  final String mensagem;

  /// Ficha da notificação.
  final String fichaId;

  /// Nome do utente da ficha.
  final String nomeFicha;

  /// Medicamento; só nas notificações de tomas.
  final String? medicamento;

  /// Hora prevista da toma; só nas notificações de tomas.
  final String? previstaEm;

  /// Instante em que a notificação foi criada.
  final String criadaEm;

  /// Estado do e-mail.
  final EstadoEnvioEmail email;
}
