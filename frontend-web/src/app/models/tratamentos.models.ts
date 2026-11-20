/** Grau de importância de um tratamento, que define a tolerância da toma (RF-051). */
export type GrauImportancia = 'Leve' | 'Moderado' | 'Grave';

/** Estado de um tratamento: em curso ou terminado (RF-054, RF-056). */
export type EstadoTratamento = 'EmCurso' | 'Terminado';

/** Graus de importância, com o texto do protótipo v02 (E16), por ordem. */
export const GRAUS_IMPORTANCIA: readonly { valor: GrauImportancia; rotulo: string }[] = [
  { valor: 'Leve', rotulo: 'Leve (tolerância 2 h)' },
  { valor: 'Moderado', rotulo: 'Moderado (tolerância 1 h)' },
  { valor: 'Grave', rotulo: 'Grave (tolerância 30 min)' },
];

/**
 * Dados de OP-27 (todos, exceto `dataFim`, obrigatórios) e de OP-30 (só os que mudam; `dataInicio` não se altera). Sem
 * `dataFim`, o tratamento é contínuo (RF-050, RF-060).
 */
export interface DadosTratamentoDto {
  medicamento: string;
  dose: string;
  grauImportancia: GrauImportancia;
  /** Data simples `AAAA-MM-DD`. */
  dataInicio: string;
  /** Data simples `AAAA-MM-DD`, não anterior a `dataInicio`; `null` num tratamento contínuo. */
  dataFim: string | null;
  /** Horas fixas de toma por dia, `HH:mm`, sem repetições. */
  horarios: string[];
}

/** Tratamento de uma ficha, devolvido por OP-27 a OP-31. */
export interface TratamentoDto {
  id: string;
  fichaId: string;
  medicamento: string;
  dose: string;
  grauImportancia: GrauImportancia;
  dataInicio: string;
  dataFim: string | null;
  horarios: string[];
  estado: EstadoTratamento;
  /** Documento da receita (OP-17), se existir. */
  receitaDocumentoId: string | null;
}
