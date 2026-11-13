/**
 * Valores do parâmetro `resultado` das rotas das fichas, que indicam a operação acabada de concluir para a página de
 * destino mostrar o resultado.
 */
export const ResultadoFicha = {
  /** Ficha criada (OP-09). */
  criada: 'criada',
  /** Ficha de terceiro criada com convite para Cuidador principal (OP-09, RF-025). */
  criadaComConvite: 'criada-com-convite',
  /** Ficha editada (OP-11). */
  editada: 'editada',
  /** Ficha eliminada (OP-14); a página de destino é a lista. */
  eliminada: 'eliminada',
} as const;
