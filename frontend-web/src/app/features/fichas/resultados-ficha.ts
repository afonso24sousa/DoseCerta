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
  /** A ficha não existe ou o Utilizador não está associado a ela (`fichaGuard`, 404); a página de destino é a lista. */
  semAcesso: 'sem-acesso',
  /** Um Familiar abriu um ecrã de alteração (`cuidadorGuard`); a página de destino é E08. */
  soConsultaFamiliar: 'so-consulta-familiar',
  /** Ficha arquivada aberta num ecrã de alteração (`fichaAtivaGuard`); a página de destino é E08. */
  soConsultaArquivada: 'so-consulta-arquivada',
} as const;
