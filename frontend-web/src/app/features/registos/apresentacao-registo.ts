import { formatarInstanteToma } from '../medicacao/apresentacao-toma';

/** Comprimento máximo da descrição de uma ocorrência ou de uma nota (RF-090, RF-091). */
export const MAXIMO_DESCRICAO = 2000;

/** Mensagens de sucesso de E24, passadas a E23 pela rota. */
export const ResultadoRegisto = {
  ocorrencia: 'ocorrencia-registada',
  nota: 'nota-registada',
} as const;

/** Data e hora de um instante com desvio, `dd/mm/aaaa às hh:mm`, lidas da própria cadeia (hora de Lisboa, RNF-047). */
export function textoInstante(instante: string): string {
  const { data, hora } = formatarInstanteToma(instante);
  return hora ? `${data} às ${hora}` : data;
}

const FORMATO_DESVIO = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Lisbon',
  timeZoneName: 'longOffset',
});

const FORMATO_LOCAL = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

/** Desvio de Lisboa, em minutos, num instante UTC. */
function desvioEmMinutos(instanteUtc: number): number {
  const nome = FORMATO_DESVIO.formatToParts(new Date(instanteUtc)).find(
    (p) => p.type === 'timeZoneName',
  )?.value;
  const partes = /GMT([+-])(\d{2}):(\d{2})/.exec(nome ?? '');
  if (!partes) {
    return 0;
  }

  const minutos = Number(partes[2]) * 60 + Number(partes[3]);
  return partes[1] === '-' ? -minutos : minutos;
}

/** Data e hora locais de Lisboa de um instante UTC, `AAAA-MM-DD hh:mm`. */
function localLisboa(instanteUtc: number): string {
  const p = Object.fromEntries(
    FORMATO_LOCAL.formatToParts(new Date(instanteUtc)).map((parte) => [parte.type, parte.value]),
  );
  return `${p['year']}-${p['month']}-${p['day']} ${p['hour']}:${p['minute']}`;
}

/**
 * Instante com o desvio de Lisboa (por exemplo `2026-10-07T15:05:00+01:00`) para a data `AAAA-MM-DD` e a hora `hh:mm`
 * indicadas em hora de Lisboa, independentemente do fuso do navegador (RNF-047). Devolve `null` se a hora não existir
 * nesse dia, por os relógios terem avançado (por exemplo às 01:30 do último domingo de março).
 */
export function instanteLisboa(data: string, hora: string): string | null {
  const [ano, mes, dia] = data.split('-').map(Number);
  const [horas, minutos] = hora.split(':').map(Number);
  const comoUtc = Date.UTC(ano, mes - 1, dia, horas, minutos);
  let desvio = desvioEmMinutos(comoUtc);
  desvio = desvioEmMinutos(comoUtc - desvio * 60_000);
  const instante = comoUtc - desvio * 60_000;
  if (localLisboa(instante) !== `${data} ${hora}`) {
    return null;
  }

  const sinal = desvio < 0 ? '-' : '+';
  const absoluto = Math.abs(desvio);
  const hh = String(Math.floor(absoluto / 60)).padStart(2, '0');
  const mm = String(absoluto % 60).padStart(2, '0');
  return `${data}T${hora}:00${sinal}${hh}:${mm}`;
}

/** Data `AAAA-MM-DD` e hora `hh:mm` de agora, em hora de Lisboa, para preencher a ocorrência por omissão. */
export function agoraLisboa(): { data: string; hora: string } {
  const [data, hora] = localLisboa(Date.now()).split(' ');
  return { data, hora };
}
