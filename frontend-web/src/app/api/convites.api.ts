import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { ConviteDto, NovoConviteDto, PessoaFichaDto } from '../models/pessoas.models';

/** Convites para as fichas (F004): OP-19 a OP-22. */
@Injectable({ providedIn: 'root' })
export class ConvitesApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-19: convida uma pessoa por e-mail para a ficha, como Cuidador ou Familiar (RF-040, RF-043). */
  convidar(fichaId: string, dados: NovoConviteDto): Observable<ConviteDto> {
    return this.http.post<ConviteDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/convites`),
      dados,
    );
  }

  /** OP-20: convites pendentes dirigidos ao e-mail do Utilizador (RF-042, RF-044, RF-135). */
  listarRecebidos(): Observable<ConviteDto[]> {
    return this.http.get<ConviteDto[]>(this.config.url('/convites'));
  }

  /** OP-21: aceita o convite e associa o Utilizador à ficha com o papel oferecido (RF-045, RF-026). */
  aceitar(conviteId: string): Observable<PessoaFichaDto> {
    return this.http.post<PessoaFichaDto>(
      this.config.url(`/convites/${encodeURIComponent(conviteId)}/aceitacao`),
      null,
    );
  }

  /** OP-22: recusa o convite, sem criar associação (RF-046, RF-027). */
  recusar(conviteId: string): Observable<void> {
    return this.http.post<void>(this.config.url(`/convites/${encodeURIComponent(conviteId)}/recusa`), null);
  }
}
