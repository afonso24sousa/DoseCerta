import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';
import { ProblemaApi, eProblemaApi } from '../models/problema-api';
import { LigacaoService } from './ligacao.service';

/**
 * Converte qualquer falha HTTP num {@link ProblemaApi} (`codigo`, `mensagem`, `campos` e o estado HTTP), para as páginas
 * tratarem um só formato. Sem resposta do servidor (estado 0) devolve o problema de falta de ligação. Nos pedidos de
 * ficheiros (`responseType: 'blob'`, OP-17) o corpo do erro chega como `Blob` e é lido antes da conversão.
 */
export const erroInterceptor: HttpInterceptorFn = (pedido, seguinte) => {
  const ligacao = inject(LigacaoService);
  return seguinte(pedido).pipe(
    catchError((erro: unknown) =>
      lerCorpoBinario(erro).pipe(switchMap((lido) => throwError(() => converter(lido, ligacao)))),
    ),
  );
};

/** Substitui um corpo de erro `Blob` pelo JSON que contém, quando o tem. */
function lerCorpoBinario(erro: unknown): Observable<unknown> {
  if (!(erro instanceof HttpErrorResponse) || !(erro.error instanceof Blob)) {
    return from([erro]);
  }

  return from(
    erro.error
      .text()
      .then((texto) => JSON.parse(texto) as unknown)
      .catch(() => null)
      .then(
        (corpo) =>
          new HttpErrorResponse({
            error: corpo,
            headers: erro.headers,
            status: erro.status,
            statusText: erro.statusText,
            url: erro.url ?? undefined,
          }),
      ),
  );
}

/** Códigos da API para os estados de ficheiro recusado (RNF-008), usados quando a resposta não traz o corpo da API. */
const CODIGOS_FICHEIRO_RECUSADO: Readonly<Record<number, string>> = {
  413: 'FICHEIRO_DEMASIADO_GRANDE',
  415: 'FORMATO_NAO_SUPORTADO',
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

  // Um ficheiro recusado antes de chegar à API (por exemplo pelo limite do servidor web) não traz o corpo da API.
  const ficheiroRecusado = CODIGOS_FICHEIRO_RECUSADO[erro.status];
  if (ficheiroRecusado) {
    return { codigo: ficheiroRecusado, mensagem: erro.statusText, estado: erro.status };
  }

  return {
    codigo: 'ERRO_INTERNO',
    mensagem: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
    estado: erro.status,
  };
}
