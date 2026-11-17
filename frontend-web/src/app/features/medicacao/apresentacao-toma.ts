import { EstadoToma, TomaDto } from '../../models/tomas.models';
import { formatarData } from '../fichas/apresentacao-ficha';

/** Dias do período do calendário de tomas e do avanço ou recuo de cada vez. */
export const DIAS_PERIODO = 7;

/** Apresentação de um estado de toma: o texto e o ícone (Material), sempre juntos (RNF-027). */
export interface ApresentacaoEstadoToma {
  texto: string;
  /** Nome do ícone Material. */
  icone: string;
  /** Desenho do ícone Material (24 × 24), para não depender de um tipo de letra de ícones. */
  desenho: string;
}

/** Texto e ícone de cada estado (contrato de interface da F006, secção 5). */
export const APRESENTACAO_ESTADOS: Readonly<Record<EstadoToma, ApresentacaoEstadoToma>> = {
  Pendente: {
    texto: 'Pendente',
    icone: 'schedule',
    desenho:
      'M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z',
  },
  Confirmada: {
    texto: 'Confirmada',
    icone: 'check_circle',
    desenho:
      'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z',
  },
  ConfirmadaComAtraso: {
    texto: 'Confirmada com atraso',
    icone: 'history',
    desenho:
      'M13 3c-4.97 0-9 4.03-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42C8.27 19.99 10.51 21 13 21c4.97 0 9-4.03 9-9s-4.03-9-9-9zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8H12z',
  },
  NaoConfirmada: {
    texto: 'Não confirmada',
    icone: 'cancel',
    desenho:
      'M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z',
  },
};

/** Instante dividido na data `dd/mm/aaaa` e na hora `hh:mm`. */
export interface InstanteFormatado {
  data: string;
  hora: string;
}

const PADRAO_INSTANTE = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/;

/**
 * Data e hora de um instante com desvio (por exemplo `2026-11-17T10:00:00+00:00` dá `17/11/2026` e `10:00`), lidas da
 * própria cadeia: o servidor já envia a hora de Lisboa no desvio e não se converte para o fuso do navegador (RNF-029,
 * RNF-047).
 */
export function formatarInstanteToma(instante: string): InstanteFormatado {
  const partes = PADRAO_INSTANTE.exec(instante);
  if (!partes) {
    return { data: instante, hora: '' };
  }

  const [, ano, mes, dia, horas, minutos] = partes;
  return { data: `${dia}/${mes}/${ano}`, hora: `${horas}:${minutos}` };
}

/** Dia `AAAA-MM-DD` em que a toma está prevista, em hora de Lisboa. */
export function diaDaToma(toma: Pick<TomaDto, 'previstaEm'>): string {
  return toma.previstaEm.slice(0, 10);
}

/** Texto do autor e do momento de uma toma confirmada (RF-065), ou `null` se não estiver confirmada. */
export function textoConfirmacao(toma: Pick<TomaDto, 'confirmadaPor' | 'confirmadaEm'>): string | null {
  if (!toma.confirmadaPor || !toma.confirmadaEm) {
    return null;
  }

  const { data, hora } = formatarInstanteToma(toma.confirmadaEm);
  return `Confirmada por ${toma.confirmadaPor.nome} em ${data} às ${hora}`;
}

/** Mensagem de sucesso de OP-33 conforme o estado resultante (RF-067). */
export function mensagemConfirmacao(estado: EstadoToma): string {
  return estado === 'ConfirmadaComAtraso' ? 'Toma confirmada com atraso.' : 'Toma confirmada.';
}

/** Tomas de um dia do período. */
export interface DiaTomas {
  /** Data `AAAA-MM-DD`. */
  dia: string;
  tomas: TomaDto[];
}

/**
 * Agrupa as tomas por dia, por ordem dos dias; dentro de cada dia mantém a ordem de OP-32 (hora prevista). Os dias sem
 * tomas não entram.
 */
export function agruparPorDia(tomas: readonly TomaDto[]): DiaTomas[] {
  const dias = new Map<string, TomaDto[]>();
  for (const toma of tomas) {
    const dia = diaDaToma(toma);
    dias.set(dia, [...(dias.get(dia) ?? []), toma]);
  }

  return [...dias.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([dia, tomasDoDia]) => ({ dia, tomas: tomasDoDia }));
}

/** Período do calendário, com os dois dias incluídos (`AAAA-MM-DD`). */
export interface PeriodoTomas {
  de: string;
  ate: string;
}

const FORMATO_DIA_LISBOA = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

/** Data de hoje em Lisboa, `AAAA-MM-DD`. */
export function hojeEmLisboa(agora: Date = new Date()): string {
  const partes = FORMATO_DIA_LISBOA.formatToParts(agora);
  const valor = (tipo: Intl.DateTimeFormatPartTypes): string =>
    partes.find((p) => p.type === tipo)?.value ?? '';
  return `${valor('year')}-${valor('month')}-${valor('day')}`;
}

/** Soma `dias` (positivos ou negativos) a uma data simples `AAAA-MM-DD`, sem depender de fusos horários. */
export function somarDias(data: string, dias: number): string {
  const [ano, mes, dia] = data.split('-').map(Number);
  const resultado = new Date(Date.UTC(ano, mes - 1, dia + dias));
  return resultado.toISOString().slice(0, 10);
}

/** Período de 7 dias que começa em `de`. */
export function periodoDesde(de: string): PeriodoTomas {
  return { de, ate: somarDias(de, DIAS_PERIODO - 1) };
}

/**
 * Período por omissão: de ontem a hoje + 5 dias, em data de Lisboa. Começa em ontem porque uma toma pendente de ontem
 * ainda pode ser confirmada até 24 horas depois da hora prevista (RF-068).
 */
export function periodoPorOmissao(agora: Date = new Date()): PeriodoTomas {
  return periodoDesde(somarDias(hojeEmLisboa(agora), -1));
}

/** Período anterior (`-1`) ou seguinte (`1`), 7 dias de cada vez. */
export function deslocarPeriodo(periodo: PeriodoTomas, sentido: -1 | 1): PeriodoTomas {
  return periodoDesde(somarDias(periodo.de, sentido * DIAS_PERIODO));
}

/** Cabeçalho do período: «Tomas de dd/mm/aaaa a dd/mm/aaaa». */
export function textoPeriodoTomas(periodo: PeriodoTomas): string {
  return `Tomas de ${formatarData(periodo.de)} a ${formatarData(periodo.ate)}`;
}
