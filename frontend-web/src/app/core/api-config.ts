import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

/**
 * Endereço base da API, lido de `src/environments`. Só aceita HTTPS (RNF-002): um endereço absoluto tem de começar por
 * `https://` e um caminho relativo (proxy de desenvolvimento ou frontend no mesmo domínio) só é aceite numa página
 * servida por HTTPS. Caso contrário a aplicação falha logo no arranque.
 */
@Injectable({ providedIn: 'root' })
export class ApiConfig {
  readonly baseUrl = ApiConfig.validar(environment.apiBaseUrl, globalThis.location?.protocol ?? '');

  /**
   * Valida o endereço base e devolve-o sem a barra final.
   *
   * @param endereco Endereço configurado.
   * @param protocoloDaPagina Protocolo da página (`https:` ou `http:`).
   */
  static validar(endereco: string, protocoloDaPagina: string): string {
    const semBarra = endereco.replace(/\/+$/, '');
    if (semBarra.startsWith('https://')) {
      return semBarra;
    }

    if (semBarra.startsWith('/') && protocoloDaPagina === 'https:') {
      return semBarra;
    }

    throw new Error(`O endereço da API tem de usar HTTPS (RNF-002): "${endereco}".`);
  }

  /**
   * Constrói o endereço completo de uma operação da API.
   *
   * @param caminho Caminho da operação, começado por `/` (por exemplo `/auth/login`).
   */
  url(caminho: string): string {
    return `${this.baseUrl}${caminho}`;
  }
}
