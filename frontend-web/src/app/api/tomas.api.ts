import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { TomaDto } from '../models/tomas.models';

/** Tomas de uma ficha (F006): OP-32 e OP-33. */
@Injectable({ providedIn: 'root' })
export class TomasApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /**
   * OP-32: tomas da ficha entre `de` e `ate` (datas `AAAA-MM-DD` em hora de Lisboa, os dois dias incluídos, no máximo 31
   * dias), ordenadas pela hora prevista (RF-063, RF-065).
   */
  listar(fichaId: string, de: string, ate: string): Observable<TomaDto[]> {
    const params = new HttpParams().set('de', de).set('ate', ate);
    return this.http.get<TomaDto[]>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}/tomas`), {
      params,
    });
  }

  /** OP-33: confirma a toma e devolve-a com o estado resultante, quem confirmou e quando (RF-064, RF-065, RF-067). */
  confirmar(tomaId: string): Observable<TomaDto> {
    return this.http.post<TomaDto>(this.config.url(`/tomas/${encodeURIComponent(tomaId)}/confirmacao`), null);
  }
}
