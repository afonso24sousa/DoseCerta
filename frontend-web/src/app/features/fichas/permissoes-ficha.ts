import { EstadoFicha, PapelFicha } from '../../models/fichas.models';

/** Dados de uma ficha que decidem as ações disponíveis, devolvidos por OP-07, OP-09, OP-10 e OP-11. */
export interface PapelNaFicha {
  meuPapel: PapelFicha;
  souPrincipal: boolean;
  estado: EstadoFicha;
}

/** Ações de F002 que a interface mostra numa ficha. */
export interface PermissoesFicha {
  /** OP-11 Editar ficha. */
  editar: boolean;
  /** OP-15 Carregar documento na ficha. */
  carregarDocumentos: boolean;
  /** OP-17 Descarregar documento. */
  descarregarDocumentos: boolean;
  /** OP-12 Arquivar ficha. */
  arquivar: boolean;
  /** OP-13 Reativar ficha. */
  reativar: boolean;
  /** OP-14 Eliminar ficha (o servidor recusa-a se a ficha tiver medicação ou histórico, RF-123). */
  eliminar: boolean;
}

/**
 * Único ponto que decide que ações de F002 aparecem numa ficha, segundo a matriz de permissões por ficha (repetida na
 * coluna «Papéis permitidos» de OP-10 a OP-17 da documentação da API):
 *
 * - o Cuidador principal e o Cuidador têm as mesmas permissões (RF-047): editar, carregar documentos, arquivar,
 *   reativar e eliminar; `souPrincipal` não acrescenta nem retira nenhuma ação de F002;
 * - o Familiar só consulta a ficha e descarrega os documentos (RF-048);
 * - uma ficha arquivada é só de consulta (RF-121): desaparecem editar, carregar documentos e arquivar; ficam reativar e
 *   eliminar, que o servidor aceita numa ficha arquivada (documentação da API, secção 3.3).
 *
 * A interface só esconde ações: o servidor verifica sempre o papel em cada pedido (RNF-004).
 */
export function permissoesFicha(ficha: PapelNaFicha): PermissoesFicha {
  const cuidador = ficha.meuPapel === 'Cuidador';
  const ativa = ficha.estado === 'Ativa';
  return {
    editar: cuidador && ativa,
    carregarDocumentos: cuidador && ativa,
    descarregarDocumentos: true,
    arquivar: cuidador && ativa,
    reativar: cuidador && !ativa,
    eliminar: cuidador,
  };
}

/** Papel do Utilizador na ficha, como texto: «Cuidador principal», «Cuidador» ou «Familiar». */
export function descricaoPapel(ficha: Pick<PapelNaFicha, 'meuPapel' | 'souPrincipal'>): string {
  if (ficha.meuPapel === 'Cuidador' && ficha.souPrincipal) {
    return 'Cuidador principal';
  }

  return ficha.meuPapel;
}
