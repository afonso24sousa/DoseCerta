import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { CompromissoDto, DadosCompromissoDto } from '../models/agenda.models';

/** Compromissos de saúde de uma ficha (F008): OP-41 a OP-44. */
@Injectable({ providedIn: 'root' })
export class CompromissosApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-41: regista um compromisso na ficha (RF-080, RF-116). */
  registar(fichaId: string, dados: DadosCompromissoDto): Observable<CompromissoDto> {
    return this.http.post<CompromissoDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/compromissos`),
      dados,
    );
  }

  /** OP-42: compromissos da ficha, ordenados pela data e hora; `de` e `ate` opcionais (`AAAA-MM-DD`, Lisboa). */
  listar(fichaId: string, de?: string, ate?: string): Observable<CompromissoDto[]> {
    let params = new HttpParams();
    if (de) {
      params = params.set('de', de);
    }
    if (ate) {
      params = params.set('ate', ate);
    }
    return this.http.get<CompromissoDto[]>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/compromissos`),
      { params },
    );
  }

  /** OP-43: altera todos os campos do compromisso (RF-081). */
  editar(compromissoId: string, dados: DadosCompromissoDto): Observable<CompromissoDto> {
    return this.http.put<CompromissoDto>(
      this.config.url(`/compromissos/${encodeURIComponent(compromissoId)}`),
      dados,
    );
  }

  /** OP-44: remove o compromisso e cancela o aviso por enviar (RF-082, RF-117). */
  remover(compromissoId: string): Observable<void> {
    return this.http.delete<void>(this.config.url(`/compromissos/${encodeURIComponent(compromissoId)}`));
  }
}
