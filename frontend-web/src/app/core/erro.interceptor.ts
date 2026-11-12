import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ProblemaApi, eProblemaApi } from '../models/problema-api';
import { LigacaoService } from './ligacao.service';

/**
 * Converte qualquer falha HTTP num {@link ProblemaApi} (`codigo`, `mensagem`, `campos` e o estado HTTP), para as páginas
 * tratarem um só formato. Sem resposta do servidor (estado 0) devolve o problema de falta de ligação.
 */
export const erroInterceptor: HttpInterceptorFn = (pedido, seguinte) => {
  const ligacao = inject(LigacaoService);
  return seguinte(pedido).pipe(catchError((erro: unknown) => throwError(() => converter(erro, ligacao))));
};

function converter(erro: unknown, ligacao: LigacaoService): ProblemaApi {
  if (!(erro instanceof HttpErrorResponse)) {
    return { codigo: 'ERRO_INTERNO', mensagem: 'Ocorreu um erro inesperado.', estado: -1 };
  }

  if (erro.status === 0) {
    return ligacao.problema();
  }

  if (eProblemaApi(erro.error)) {
    return { ...erro.error, estado: erro.status };
  }

  return {
    codigo: 'ERRO_INTERNO',
    mensagem: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
    estado: erro.status,
  };
}
