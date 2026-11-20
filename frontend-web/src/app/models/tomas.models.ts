import { GrauImportancia } from './tratamentos.models';

/**
 * Estado de uma toma (RF-066 a RF-068): nasce `Pendente`; confirmada até ao fim da tolerância do grau fica `Confirmada`
 * e depois dela `ConfirmadaComAtraso`; sem confirmação 24 horas depois da hora prevista passa a `NaoConfirmada`, estado
 * final.
 */
export type EstadoToma = 'Pendente' | 'Confirmada' | 'ConfirmadaComAtraso' | 'NaoConfirmada';

/** Cuidador que confirmou uma toma (RF-065); `nome` é o nome atual da conta. */
export interface PessoaConfirmouDto {
  id: string;
  nome: string;
}

/** Toma prevista de um tratamento, devolvida por OP-32 e OP-33. */
export interface TomaDto {
  id: string;
  tratamentoId: string;
  medicamento: string;
  dose: string;
  grauImportancia: GrauImportancia;
  /** Data e hora previstas, com o desvio de Lisboa (por exemplo `2026-10-07T08:00:00+01:00`). */
  previstaEm: string;
  estado: EstadoToma;
  /** Quem confirmou, nas tomas confirmadas; `null` nas restantes. */
  confirmadaPor: PessoaConfirmouDto | null;
  /** Momento da confirmação, com o desvio de Lisboa; `null` nas tomas por confirmar. */
  confirmadaEm: string | null;
}
