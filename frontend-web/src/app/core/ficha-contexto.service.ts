import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { FichasApi } from '../api/fichas.api';
import { FichaDto } from '../models/fichas.models';

/**
 * Ficha aberta nas rotas `/fichas/:fichaId/...` (modelo de classes do frontend web v02, secção 6.1), carregada pelo
 * `fichaGuard` (OP-10) e lida pelo `cuidadorGuard`, pelo `fichaAtivaGuard` e pelas páginas da ficha. O papel e o
 * estado servem só para mostrar ou esconder ações: o servidor verifica-os em cada pedido (RNF-004).
 */
@Injectable({ providedIn: 'root' })
export class FichaContextoService {
  private readonly fichasApi = inject(FichasApi);
  private pedido: { fichaId: string; resposta: Observable<FichaDto> } | null = null;

  /** Ficha aberta, ou `null` antes de carregar. */
  readonly ficha = signal<FichaDto | null>(null);

  /** O Utilizador é Cuidador da ficha (Cuidador principal ou Cuidador, RF-047). */
  readonly eCuidador = computed(() => this.ficha()?.meuPapel === 'Cuidador');

  /** A ficha está arquivada e só pode ser consultada (RF-120, RF-122). */
  readonly arquivada = computed(() => this.ficha()?.estado === 'Arquivada');

  /** Volta a pedir a ficha ao servidor (OP-10) e guarda-a; os pedidos seguintes da mesma ficha partilham a resposta. */
  carregar(fichaId: string): Observable<FichaDto> {
    const resposta = this.fichasApi.consultar(fichaId).pipe(
      tap((ficha) => this.ficha.set(ficha)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.pedido = { fichaId, resposta };
    return resposta;
  }

  /** Pedido mais recente da ficha indicada, ou um pedido novo se for outra ficha. */
  obter(fichaId: string): Observable<FichaDto> {
    return this.pedido?.fichaId === fichaId ? this.pedido.resposta : this.carregar(fichaId);
  }
}
