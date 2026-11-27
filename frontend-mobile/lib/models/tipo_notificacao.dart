/// Tipo de uma notificação do histórico (OP-51; RF-041, RF-110 a RF-113, RF-115, RF-117).
enum TipoNotificacao {
  /// Convite para uma ficha (RF-041).
  conviteRecebido('ConviteRecebido', 'Convite recebido'),

  /// Toma sem confirmação depois da tolerância (RF-110, RF-111, RF-112).
  tomaEmAtraso('TomaEmAtraso', 'Toma em atraso'),

  /// Toma de grau Grave em atraso prolongado, também enviada aos Familiares (RF-113).
  tomaGraveFamiliares('TomaGraveFamiliares', 'Toma Grave em atraso prolongado'),

  /// Ocorrência registada na ficha (RF-115).
  novaOcorrencia('NovaOcorrencia', 'Nova ocorrência'),

  /// Compromisso próximo (RF-117).
  proximoCompromisso('ProximoCompromisso', 'Compromisso próximo');

  const TipoNotificacao(this.valor, this.rotulo);

  /// Valor usado pela API.
  final String valor;

  /// Texto apresentado.
  final String rotulo;

  /// Lê o valor da API.
  static TipoNotificacao fromJson(Object? valor) =>
      TipoNotificacao.values.firstWhere(
        (tipo) => tipo.valor == valor,
        orElse: () =>
            throw FormatException('Tipo de notificação desconhecido: $valor'),
      );
}
