import { CATEGORIAS_DOCUMENTO, CategoriaDocumento } from '../../models/fichas.models';

const FORMATO_INSTANTE = new Intl.DateTimeFormat('pt-PT', {
  timeZone: 'Europe/Lisbon',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

const FORMATO_NUMERO = new Intl.NumberFormat('pt-PT', { maximumFractionDigits: 1 });

/** Data simples `AAAA-MM-DD` no formato `dd/mm/aaaa` (RNF-029). */
export function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-');
  return ano && mes && dia ? `${dia}/${mes}/${ano}` : data;
}

/** Instante com desvio no formato `dd/mm/aaaa, hh:mm`, na hora de Lisboa (RNF-029, RNF-047). */
export function formatarInstante(instante: string): string {
  const data = new Date(instante);
  return Number.isNaN(data.getTime()) ? instante : FORMATO_INSTANTE.format(data);
}

/** Tamanho de um ficheiro em KB ou MB. */
export function formatarTamanho(bytes: number): string {
  if (bytes < 1024 * 1024) {
    return `${FORMATO_NUMERO.format(Math.max(bytes / 1024, 0.1))} KB`;
  }

  return `${FORMATO_NUMERO.format(bytes / (1024 * 1024))} MB`;
}

/** Texto a mostrar para uma categoria de documento. */
export function rotuloCategoria(categoria: CategoriaDocumento): string {
  return CATEGORIAS_DOCUMENTO.find((c) => c.valor === categoria)?.rotulo ?? categoria;
}
