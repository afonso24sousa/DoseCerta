import { EstadoEnvioEmail, NotificacaoDto, TipoNotificacao } from '../../models/notificacoes.models';
import { formatarInstanteToma } from '../medicacao/apresentacao-toma';

/** Apresentação de um tipo de notificação: o texto e o ícone (Material), sempre juntos, além da cor (RNF-027). */
export interface ApresentacaoTipoNotificacao {
  texto: string;
  /** Nome do ícone Material. */
  icone: string;
  /** Desenho do ícone Material (24 × 24), para não depender de um tipo de letra de ícones. */
  desenho: string;
}

/** Texto e ícone de cada tipo de notificação (RF-041, RF-110 a RF-113, RF-115, RF-117). */
export const APRESENTACAO_TIPOS: Readonly<Record<TipoNotificacao, ApresentacaoTipoNotificacao>> = {
  ConviteRecebido: {
    texto: 'Convite recebido',
    icone: 'mail',
    desenho:
      'M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z',
  },
  TomaEmAtraso: {
    texto: 'Toma em atraso',
    icone: 'alarm',
    desenho:
      'M22 5.72l-4.6-3.86-1.29 1.53 4.6 3.86L22 5.72zM7.88 3.39L6.6 1.86 2 5.71l1.29 1.53 4.59-3.85zM12.5 8H11v6l4.75 2.85.75-1.23-4-2.37V8zM12 4c-4.97 0-9 4.03-9 9s4.02 9 9 9c4.97 0 9-4.03 9-9s-4.03-9-9-9zm0 16c-3.87 0-7-3.13-7-7s3.13-7 7-7 7 3.13 7 7-3.13 7-7 7z',
  },
  TomaGraveFamiliares: {
    texto: 'Toma de grau Grave em atraso prolongado',
    icone: 'warning',
    desenho: 'M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z',
  },
  NovaOcorrencia: {
    texto: 'Nova ocorrência',
    icone: 'description',
    desenho:
      'M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z',
  },
  ProximoCompromisso: {
    texto: 'Compromisso próximo',
    icone: 'event',
    desenho:
      'M17 12h-5v5h5v-5zM16 1v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2h-1V1h-2zm3 18H5V8h14v11z',
  },
};

/** Texto do estado do e-mail de uma notificação (RF-118, RF-119). */
export const TEXTOS_EMAIL: Readonly<Record<EstadoEnvioEmail, string>> = {
  Enviado: 'E-mail enviado',
  Pendente: 'E-mail pendente (reenvio em curso)',
  NaoEntregue: 'E-mail não entregue',
};

/** Data e hora da notificação, `dd/mm/aaaa hh:mm`, na hora de Lisboa enviada pelo servidor (RNF-029). */
export function momentoNotificacao(notificacao: NotificacaoDto): string {
  const { data, hora } = formatarInstanteToma(notificacao.criadaEm);
  return hora ? `${data} ${hora}` : data;
}

/**
 * Detalhe da toma, só nas notificações de tomas: «Paracetamol, prevista para 07/10/2026 às 08:00» (RF-110 a RF-113).
 * Nas restantes notificações devolve `null`.
 */
export function detalheToma(notificacao: NotificacaoDto): string | null {
  if (!notificacao.medicamento || !notificacao.previstaEm) {
    return null;
  }

  const { data, hora } = formatarInstanteToma(notificacao.previstaEm);
  return `${notificacao.medicamento}, prevista para ${data} às ${hora}`;
}

/**
 * Ordena da mais recente para a mais antiga pelo momento da notificação (RF-140). O servidor já devolve por esta ordem;
 * a ordenação no cliente garante-a. As cadeias têm o mesmo desvio de Lisboa no mesmo período, por isso compara-se o
 * instante absoluto.
 */
export function ordenarNotificacoes(notificacoes: readonly NotificacaoDto[]): NotificacaoDto[] {
  return [...notificacoes].sort((a, b) => Date.parse(b.criadaEm) - Date.parse(a.criadaEm));
}
