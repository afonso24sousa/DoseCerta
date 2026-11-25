import { AntecedenciaAviso, ItemAgendaDto, VistaAgenda } from '../../models/agenda.models';
import { formatarData } from '../fichas/apresentacao-ficha';
import { formatarInstanteToma, somarDias } from '../medicacao/apresentacao-toma';

/** Vistas da agenda, pela ordem dos separadores (RF-084 a RF-086). */
export const VISTAS: readonly { valor: VistaAgenda; rotulo: string }[] = [
  { valor: 'dia', rotulo: 'Dia' },
  { valor: 'semana', rotulo: 'Semana' },
  { valor: 'mes', rotulo: 'Mês' },
];

/** Antecedências do aviso aceites pelo servidor (RF-116); 24 horas por omissão (RF-080). */
export const ANTECEDENCIAS: readonly { valor: AntecedenciaAviso; rotulo: string }[] = [
  { valor: 30, rotulo: '30 minutos' },
  { valor: 60, rotulo: '1 hora' },
  { valor: 120, rotulo: '2 horas' },
  { valor: 1440, rotulo: '24 horas' },
  { valor: 2880, rotulo: '48 horas' },
];

export const ANTECEDENCIA_POR_OMISSAO: AntecedenciaAviso = 1440;

const DIAS_SEMANA = [
  'domingo',
  'segunda-feira',
  'terça-feira',
  'quarta-feira',
  'quinta-feira',
  'sexta-feira',
  'sábado',
];
const DIAS_SEMANA_CURTOS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
const MESES = [
  'janeiro',
  'fevereiro',
  'março',
  'abril',
  'maio',
  'junho',
  'julho',
  'agosto',
  'setembro',
  'outubro',
  'novembro',
  'dezembro',
];

/** Cabeçalhos das colunas do mês, de segunda-feira a domingo. */
export const CABECALHOS_SEMANA = DIAS_SEMANA_CURTOS;

function partes(data: string): [number, number, number] {
  const [ano, mes, dia] = data.split('-').map(Number);
  return [ano, mes, dia];
}

/** Dia da semana de uma data simples, 0 = domingo. */
function diaDaSemana(data: string): number {
  const [ano, mes, dia] = partes(data);
  return new Date(Date.UTC(ano, mes - 1, dia)).getUTCDay();
}

/** Nome do dia da semana, por exemplo «quarta-feira». */
export function nomeDiaSemana(data: string): string {
  return DIAS_SEMANA[diaDaSemana(data)];
}

/** Segunda-feira da semana que contém `data` (RF-085). */
export function inicioDaSemana(data: string): string {
  return somarDias(data, -((diaDaSemana(data) + 6) % 7));
}

/** Primeiro e último dia do mês que contém `data`. */
function limitesDoMes(data: string): { de: string; ate: string } {
  const [ano, mes] = partes(data);
  const de = `${ano}-${String(mes).padStart(2, '0')}-01`;
  const ate = new Date(Date.UTC(ano, mes, 0)).toISOString().slice(0, 10);
  return { de, ate };
}

/** Dias incluídos na vista que contém `data`. */
export function periodoDaVista(vista: VistaAgenda, data: string): { de: string; ate: string } {
  if (vista === 'dia') {
    return { de: data, ate: data };
  }
  if (vista === 'semana') {
    const de = inicioDaSemana(data);
    return { de, ate: somarDias(de, 6) };
  }
  return limitesDoMes(data);
}

/** Data do período anterior (`-1`) ou seguinte (`1`): um dia, uma semana ou um mês. */
export function deslocarData(vista: VistaAgenda, data: string, sentido: -1 | 1): string {
  if (vista === 'dia') {
    return somarDias(data, sentido);
  }
  if (vista === 'semana') {
    return somarDias(inicioDaSemana(data), sentido * 7);
  }
  const [ano, mes] = partes(data);
  return new Date(Date.UTC(ano, mes - 1 + sentido, 1)).toISOString().slice(0, 10);
}

/** Título da vista: «quarta-feira, 25/11/2026», «Semana de 23/11/2026 a 29/11/2026» ou «novembro de 2026». */
export function tituloDaVista(vista: VistaAgenda, data: string): string {
  if (vista === 'dia') {
    return `${nomeDiaSemana(data)}, ${formatarData(data)}`;
  }
  if (vista === 'semana') {
    const { de, ate } = periodoDaVista('semana', data);
    return `Semana de ${formatarData(de)} a ${formatarData(ate)}`;
  }
  const [ano, mes] = partes(data);
  return `${MESES[mes - 1]} de ${ano}`;
}

/** Dia do evento (`AAAA-MM-DD`), lido do desvio de Lisboa enviado pelo servidor (RNF-047). */
export function diaDoEvento(item: Pick<ItemAgendaDto, 'inicio'>): string {
  return item.inicio.slice(0, 10);
}

/** Hora do evento, `hh:mm` (RNF-029). */
export function horaDoEvento(item: Pick<ItemAgendaDto, 'inicio'>): string {
  return formatarInstanteToma(item.inicio).hora;
}

/** Eventos de um dia da vista. */
export interface DiaAgenda {
  dia: string;
  /** O dia pertence ao mês mostrado (na vista mensal, os dias das semanas vizinhas são `false`). */
  doPeriodo: boolean;
  itens: ItemAgendaDto[];
}

/**
 * Dias da vista com os respetivos eventos, pela ordem de OP-45 (início, utente, tipo). Na semana entram os 7 dias, mesmo
 * sem eventos; no mês, as semanas completas de segunda-feira a domingo que cobrem o mês, para formar a grelha.
 */
export function diasDaVista(vista: VistaAgenda, data: string, itens: readonly ItemAgendaDto[]): DiaAgenda[] {
  const porDia = new Map<string, ItemAgendaDto[]>();
  for (const item of itens) {
    const dia = diaDoEvento(item);
    porDia.set(dia, [...(porDia.get(dia) ?? []), item]);
  }

  const periodo = periodoDaVista(vista, data);
  const de = vista === 'mes' ? inicioDaSemana(periodo.de) : periodo.de;
  const ate = vista === 'mes' ? somarDias(inicioDaSemana(periodo.ate), 6) : periodo.ate;
  const dias: DiaAgenda[] = [];
  for (let dia = de; dia <= ate; dia = somarDias(dia, 1)) {
    dias.push({ dia, doPeriodo: dia >= periodo.de && dia <= periodo.ate, itens: porDia.get(dia) ?? [] });
  }
  return dias;
}

/** Divide os dias em semanas de 7 dias (grelha mensal). */
export function semanas(dias: readonly DiaAgenda[]): DiaAgenda[][] {
  const resultado: DiaAgenda[][] = [];
  for (let i = 0; i < dias.length; i += 7) {
    resultado.push(dias.slice(i, i + 7));
  }
  return resultado;
}

const FORMATO_LISBOA = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

/**
 * Data e hora de Lisboa com o desvio certo para esse dia (`+00:00` no inverno, `+01:00` no verão), por exemplo
 * `2026-11-25T10:30:00+00:00`, independentemente do fuso do navegador (RNF-047).
 */
export function dataHoraLisboa(data: string, hora: string): string {
  const [ano, mes, dia] = partes(data);
  const [h, m] = hora.split(':').map(Number);
  const comoUtc = Date.UTC(ano, mes - 1, dia, h, m);
  // Hora que esse instante UTC tem em Lisboa; a diferença é o desvio de Lisboa nesse momento.
  const p = Object.fromEntries(FORMATO_LISBOA.formatToParts(new Date(comoUtc)).map((x) => [x.type, x.value]));
  const emLisboa = Date.UTC(
    Number(p['year']),
    Number(p['month']) - 1,
    Number(p['day']),
    Number(p['hour']),
    Number(p['minute']),
  );
  const desvioMinutos = Math.round((emLisboa - comoUtc) / 60_000);
  const sinal = desvioMinutos < 0 ? '-' : '+';
  const absoluto = Math.abs(desvioMinutos);
  const desvio = `${sinal}${String(Math.floor(absoluto / 60)).padStart(2, '0')}:${String(absoluto % 60).padStart(2, '0')}`;
  return `${data}T${hora}:00${desvio}`;
}

/**
 * O momento do aviso (data e hora menos a antecedência) já passou, mas o compromisso ainda não: o servidor cria a
 * notificação no ciclo seguinte do processo periódico (RF-117, RNF-040).
 */
export function avisoJaPassou(
  dataHora: string,
  antecedencia: AntecedenciaAviso,
  agora: Date = new Date(),
): boolean {
  const instante = Date.parse(dataHora);
  if (Number.isNaN(instante)) {
    return false;
  }
  return instante > agora.getTime() && instante - antecedencia * 60_000 <= agora.getTime();
}

/** O compromisso já passou: o servidor aceita-o, mas não o avisa (secção 4 da API). */
export function compromissoNoPassado(dataHora: string, agora: Date = new Date()): boolean {
  const instante = Date.parse(dataHora);
  return !Number.isNaN(instante) && instante <= agora.getTime();
}
