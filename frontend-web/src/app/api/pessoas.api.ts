import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { MudancaPapelDto, PessoaFichaDto, TransferenciaPrincipalDto } from '../models/pessoas.models';

/**
 * Pessoas associadas a uma ficha e os seus papéis (F004): OP-18 e OP-23 a OP-26. OP-23 e OP-24 usam o mesmo endpoint;
 * o corpo indica o papel pretendido.
 */
@Injectable({ providedIn: 'root' })
export class PessoasApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-18: Cuidadores e Familiares da ficha, com o papel e a indicação de Cuidador principal (RF-025, RF-048). */
  listar(fichaId: string): Observable<PessoaFichaDto[]> {
    return this.http.get<PessoaFichaDto[]>(this.config.url(PessoasApi.base(fichaId)));
  }

  /** OP-23: promove um Familiar a Cuidador (RF-049). */
  promover(fichaId: string, utilizadorId: string): Observable<PessoaFichaDto> {
    return this.mudarPapel(fichaId, utilizadorId, { papel: 'Cuidador' });
  }

  /** OP-24: despromove um Cuidador a Familiar (RF-130, RF-133, RF-134). */
  despromover(fichaId: string, utilizadorId: string): Observable<PessoaFichaDto> {
    return this.mudarPapel(fichaId, utilizadorId, { papel: 'Familiar' });
  }

  /** OP-25: remove o acesso da pessoa à ficha (RF-131, RF-133, RF-134). */
  remover(fichaId: string, utilizadorId: string): Observable<void> {
    return this.http.delete<void>(
      this.config.url(`${PessoasApi.base(fichaId)}/${encodeURIComponent(utilizadorId)}`),
    );
  }

  /** OP-26: atribui o papel de Cuidador principal a outro Cuidador da ficha (RF-132). */
  transferirPrincipal(fichaId: string, dados: TransferenciaPrincipalDto): Observable<PessoaFichaDto> {
    return this.http.post<PessoaFichaDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/cuidador-principal`),
      dados,
    );
  }

  private mudarPapel(
    fichaId: string,
    utilizadorId: string,
    dados: MudancaPapelDto,
  ): Observable<PessoaFichaDto> {
    return this.http.put<PessoaFichaDto>(
      this.config.url(`${PessoasApi.base(fichaId)}/${encodeURIComponent(utilizadorId)}/papel`),
      dados,
    );
  }

  private static base(fichaId: string): string {
    return `/fichas/${encodeURIComponent(fichaId)}/pessoas`;
  }
}
