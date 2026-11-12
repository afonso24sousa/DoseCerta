import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { AlterarPalavraPasseDto, DadosContaDto, UtilizadorDto } from '../models/conta.models';

/**
 * Operações da conta do Utilizador autenticado (F001): OP-04 a OP-06.
 */
@Injectable({ providedIn: 'root' })
export class ContaApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-04: consulta o nome e o e-mail da conta (RF-013). */
  consultar(): Observable<UtilizadorDto> {
    return this.http.get<UtilizadorDto>(this.config.url('/conta'));
  }

  /** OP-05: altera o nome e o e-mail (RF-014). */
  atualizar(dados: DadosContaDto): Observable<UtilizadorDto> {
    return this.http.put<UtilizadorDto>(this.config.url('/conta'), dados);
  }

  /** OP-06: altera a palavra-passe (RF-015). */
  alterarPalavraPasse(dados: AlterarPalavraPasseDto): Observable<void> {
    return this.http.put<void>(this.config.url('/conta/palavra-passe'), dados);
  }
}
