/// Estado do e-mail de uma notificação (OP-51; RF-118, RF-119).
enum EstadoEnvioEmail {
  /// Ainda não enviado ou com reenvio em curso (RF-118).
  pendente('Pendente', 'E-mail pendente'),

  /// Enviado.
  enviado('Enviado', 'E-mail enviado'),

  /// As 3 tentativas de reenvio falharam (RF-119).
  naoEntregue('NaoEntregue', 'E-mail não entregue');

  const EstadoEnvioEmail(this.valor, this.rotulo);

  /// Valor usado pela API.
  final String valor;

  /// Texto apresentado.
  final String rotulo;

  /// Lê o valor da API.
  static EstadoEnvioEmail fromJson(Object? valor) =>
      EstadoEnvioEmail.values.firstWhere(
        (estado) => estado.valor == valor,
        orElse: () =>
            throw FormatException('Estado do e-mail desconhecido: $valor'),
      );
}
