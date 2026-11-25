import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { ItemAgendaDto, VistaAgenda } from '../models/agenda.models';

/** Agenda do Utilizador (F008): OP-45. */
@Injectable({ providedIn: 'root' })
export class AgendaApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /**
   * OP-45: compromissos e tomas das fichas ativas em que o Utilizador é Cuidador ou Familiar (RF-083), no dia, na semana
   * (segunda-feira a domingo) ou no mês que contém `data` (`AAAA-MM-DD`, hora de Lisboa), num só pedido (RNF-012).
   */
  consultar(vista: VistaAgenda, data: string): Observable<ItemAgendaDto[]> {
    const params = new HttpParams().set('vista', vista).set('data', data);
    return this.http.get<ItemAgendaDto[]>(this.config.url('/agenda'), { params });
  }
}
