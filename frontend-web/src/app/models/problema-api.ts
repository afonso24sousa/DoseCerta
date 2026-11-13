/**
 * Erro de um campo devolvido pela API: o campo em erro e a regra violada (RNF-028).
 */
export interface ErroCampo {
  campo: string;
  regra: string;
}

/**
 * Corpo de erro uniforme da API (documentação da API, secção 3.3). `estado` é o código HTTP da resposta, acrescentado
 * pelo `erroInterceptor`; 0 indica que o servidor não respondeu.
 */
export interface ProblemaApi {
  codigo: string;
  mensagem: string;
  campos?: ErroCampo[];
  estado: number;
}

/**
 * Indica se um valor é um {@link ProblemaApi}.
 */
export function eProblemaApi(valor: unknown): valor is ProblemaApi {
  return typeof valor === 'object' && valor !== null && 'codigo' in valor && 'mensagem' in valor;
}
