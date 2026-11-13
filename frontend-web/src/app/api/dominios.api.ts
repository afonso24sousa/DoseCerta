import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { ListasNecessidadesDto } from '../models/fichas.models';

/**
 * Listas fechadas do servidor (F002): OP-08, usada pelos formulários de criação e de edição de fichas.
 */
@Injectable({ providedIn: 'root' })
export class DominiosApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-08: condições de saúde, alergias e graus de autonomia (RF-021). */
  necessidades(): Observable<ListasNecessidadesDto> {
    return this.http.get<ListasNecessidadesDto>(this.config.url('/dominios/necessidades'));
  }
}
