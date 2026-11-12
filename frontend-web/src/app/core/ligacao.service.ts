import { Injectable } from '@angular/core';
import { ProblemaApi } from '../models/problema-api';

/** Código local (não existe na API) de um pedido sem resposta do servidor. */
export const CODIGO_SEM_LIGACAO = 'SEM_LIGACAO';

/**
 * Falta de ligação ao servidor: pedido sem resposta (estado 0), por exemplo sem Internet ou com o servidor parado.
 */
@Injectable({ providedIn: 'root' })
export class LigacaoService {
  readonly mensagem =
    'Não foi possível contactar o servidor. Verifique a ligação à Internet e tente novamente.';

  /** Problema local que representa a falta de ligação. */
  problema(): ProblemaApi {
    return { codigo: CODIGO_SEM_LIGACAO, mensagem: this.mensagem, estado: 0 };
  }
}
