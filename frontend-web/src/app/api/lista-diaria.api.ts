import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { ItemListaDiariaDto } from '../models/lista-diaria.models';

/** Lista de tarefas diária consolidada (F010): OP-50. As tomas confirmam-se com OP-33, no {@link TomasApi}. */
@Injectable({ providedIn: 'root' })
export class ListaDiariaApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /**
   * OP-50: tomas e compromissos do dia `data` (`AAAA-MM-DD`, hora de Lisboa; sem ela, hoje) das fichas ativas em que o
   * Utilizador é Cuidador. Hoje inclui também as tomas de ontem ainda `Pendente` (RF-100).
   */
  consultar(data?: string): Observable<ItemListaDiariaDto[]> {
    const params = data ? new HttpParams().set('data', data) : undefined;
    return this.http.get<ItemListaDiariaDto[]>(this.config.url('/lista-diaria'), { params });
  }
}
