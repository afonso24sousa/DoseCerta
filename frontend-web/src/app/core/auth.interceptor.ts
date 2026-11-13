import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { CAMINHO_LOGIN, CAMINHO_REGISTO } from '../api/auth.api';
import { ApiConfig } from './api-config';
import { AuthService } from './auth.service';

/**
 * Envia o token JWT (`Authorization: Bearer`) em todos os pedidos à API, exceto OP-01 e OP-02, que são públicos
 * (RNF-054). Uma resposta 401 a um pedido autenticado termina a sessão e volta ao início de sessão (RF-017).
 */
export const authInterceptor: HttpInterceptorFn = (pedido, seguinte) => {
  const config = inject(ApiConfig);
  const auth = inject(AuthService);
  const publico = [CAMINHO_REGISTO, CAMINHO_LOGIN].some((caminho) => pedido.url === config.url(caminho));
  const daApi = pedido.url.startsWith(config.baseUrl);
  const token = auth.token();

  const enviado =
    daApi && !publico && token ? pedido.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : pedido;

  return seguinte(enviado).pipe(
    catchError((erro: unknown) => {
      if (daApi && !publico && erro instanceof HttpErrorResponse && erro.status === 401) {
        auth.sessaoExpirada();
      }

      return throwError(() => erro);
    }),
  );
};
