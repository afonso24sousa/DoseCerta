import {
  EstadoTratamento,
  GRAUS_IMPORTANCIA,
  GrauImportancia,
  TratamentoDto,
} from '../../models/tratamentos.models';
import { formatarData } from '../fichas/apresentacao-ficha';

/** Valores do parâmetro `resultado` das rotas da medicação, para a página de destino mostrar o resultado. */
export const ResultadoMedicacao = {
  /** Tratamento registado (OP-27); a página de destino é E15. */
  registado: 'registado',
  /** Tratamento atualizado (OP-30); a página de destino é E17. */
  atualizado: 'atualizado',
  /** Tentativa de editar um tratamento terminado; a página de destino é E17. */
  naoEditavel: 'nao-editavel',
} as const;

/** Horários como no protótipo: «08:00 e 20:00», «08:00, 14:00 e 20:00». */
export function textoHorarios(horarios: readonly string[]): string {
  if (horarios.length <= 1) {
    return horarios[0] ?? '';
  }

  return `${horarios.slice(0, -1).join(', ')} e ${horarios[horarios.length - 1]}`;
}

/** Período como no protótipo: «desde 01/10/2026, contínuo» ou «01/10/2026 a 31/10/2026» (RF-050, RNF-029). */
export function textoPeriodo(tratamento: Pick<TratamentoDto, 'dataInicio' | 'dataFim'>): string {
  return tratamento.dataFim
    ? `${formatarData(tratamento.dataInicio)} a ${formatarData(tratamento.dataFim)}`
    : `desde ${formatarData(tratamento.dataInicio)}, contínuo`;
}

/** Grau de importância com a tolerância, como no protótipo (E16, E17). */
export function textoGrau(grau: GrauImportancia): string {
  return GRAUS_IMPORTANCIA.find((g) => g.valor === grau)?.rotulo ?? grau;
}

/** Estado do tratamento em texto: «Em curso» ou «Terminado» (RNF-027: o estado nunca depende só da cor). */
export function textoEstado(estado: EstadoTratamento): string {
  return estado === 'EmCurso' ? 'Em curso' : 'Terminado';
}
