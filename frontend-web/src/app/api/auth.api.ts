import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { ContaCriadaDto, CredenciaisDto, NovaContaDto, SessaoDto } from '../models/conta.models';

/** Caminho de OP-01 Registar conta, público. */
export const CAMINHO_REGISTO = '/auth/registo';

/** Caminho de OP-02 Iniciar sessão, público. */
export const CAMINHO_LOGIN = '/auth/login';

/**
 * Operações de autenticação da API (F001): OP-01 a OP-03.
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-01: cria a conta (RF-010, RF-011). */
  registar(dados: NovaContaDto): Observable<ContaCriadaDto> {
    return this.http.post<ContaCriadaDto>(this.config.url(CAMINHO_REGISTO), dados);
  }

  /** OP-02: inicia sessão e devolve o token (RF-012). */
  iniciarSessao(credenciais: CredenciaisDto): Observable<SessaoDto> {
    return this.http.post<SessaoDto>(this.config.url(CAMINHO_LOGIN), credenciais);
  }

  /** OP-03: termina a sessão e invalida o token no servidor (RF-016). */
  terminarSessao(): Observable<void> {
    return this.http.post<void>(this.config.url('/auth/logout'), null);
  }
}
