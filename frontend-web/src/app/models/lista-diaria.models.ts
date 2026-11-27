import { EstadoToma } from './tomas.models';

/** Tipo de um item da lista de tarefas diária (F010). */
export type TipoItemListaDiaria = 'Toma' | 'Compromisso';

/**
 * Item da lista de tarefas diária devolvido por OP-50: uma toma ou um compromisso de uma ficha ativa em que o
 * Utilizador é Cuidador, com o nome do utente (RF-100, RF-101).
 */
export interface ItemListaDiariaDto {
  tipo: TipoItemListaDiaria;
  id: string;
  fichaId: string;
  nomeFicha: string;
  /** «Medicamento Dose» nas tomas; o motivo nos compromissos. */
  titulo: string;
  /** Data e hora previstas, com o desvio de Lisboa. */
  previstaEm: string;
  /** Estado da toma; não existe nos compromissos. */
  estado?: EstadoToma;
  /** Nome atual de quem confirmou, nas tomas `Confirmada` e `ConfirmadaComAtraso` (RF-104). */
  confirmadaPor?: string | null;
  /** Momento da confirmação, com o desvio de Lisboa. */
  confirmadaEm?: string | null;
  /** `true` só numa toma `Pendente` dentro da janela de confirmação; `false` nos compromissos (RF-087). */
  confirmavel: boolean;
}
