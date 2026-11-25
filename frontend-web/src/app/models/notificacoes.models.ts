/** Tipo de uma notificação (OP-51; modelo de classes do frontend web v02). */
export type TipoNotificacao =
  'ConviteRecebido' | 'TomaEmAtraso' | 'TomaGraveFamiliares' | 'NovaOcorrencia' | 'ProximoCompromisso';

/**
 * Estado do e-mail de uma notificação: `Enviado`, `Pendente` (ainda não enviado ou com reenvio em curso, RF-118) ou
 * `NaoEntregue`, depois de falharem as 3 tentativas (RF-119).
 */
export type EstadoEnvioEmail = 'Enviado' | 'Pendente' | 'NaoEntregue';

/** Notificação recebida pelo Utilizador, devolvida por OP-51. */
export interface NotificacaoDto {
  id: string;
  tipo: TipoNotificacao;
  mensagem: string;
  fichaId: string;
  nomeFicha: string;
  /** Medicamento, só nas notificações de tomas; `null` nas restantes. */
  medicamento: string | null;
  /** Hora prevista da toma, com o desvio de Lisboa, só nas notificações de tomas; `null` nas restantes. */
  previstaEm: string | null;
  /** Momento da notificação, com o desvio de Lisboa. */
  criadaEm: string;
  email: EstadoEnvioEmail;
}
