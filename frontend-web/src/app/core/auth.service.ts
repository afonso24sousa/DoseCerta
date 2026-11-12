import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, switchMap, tap } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { ContaApi } from '../api/conta.api';
import {
  ContaCriadaDto,
  CredenciaisDto,
  NovaContaDto,
  SessaoDto,
  UtilizadorDto,
} from '../models/conta.models';
import { TokenStorage } from './token-storage';

/** Parâmetro de `/entrar` que mostra o aviso de sessão terminada. */
export const AVISO_SESSAO_TERMINADA = 'terminada';

/**
 * Estado da sessão do Utilizador, com signals (F001). A sessão dura 8 horas e não é renovada (RNF-009): no instante
 * `expiraEm` termina sozinha e o Utilizador volta ao início de sessão.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApi);
  private readonly contaApi = inject(ContaApi);
  private readonly tokenStorage = inject(TokenStorage);
  private readonly router = inject(Router);
  private temporizador: ReturnType<typeof setTimeout> | undefined;

  /** Sessão atual, ou `null` sem sessão. */
  readonly sessao = signal<SessaoDto | null>(null);

  /** Dados da conta do Utilizador autenticado, quando já carregados. */
  readonly utilizador = signal<UtilizadorDto | null>(null);

  /** Indica se há sessão iniciada. */
  readonly autenticado = computed(() => this.sessao() !== null);

  /** Instante de expiração da sessão. */
  readonly expiraEm = computed(() => {
    const sessao = this.sessao();
    return sessao ? new Date(sessao.expiraEm) : null;
  });

  /** Token JWT da sessão atual, para o `authInterceptor`. */
  token(): string | null {
    return this.sessao()?.token ?? null;
  }

  /** OP-01: cria a conta; não inicia sessão (RF-010). */
  registar(dados: NovaContaDto): Observable<ContaCriadaDto> {
    return this.authApi.registar(dados);
  }

  /** OP-02 e OP-04: inicia sessão, guarda o token e carrega os dados da conta (RF-012). */
  iniciarSessao(credenciais: CredenciaisDto): Observable<void> {
    return this.authApi.iniciarSessao(credenciais).pipe(
      tap((sessao) => this.ativar(sessao)),
      switchMap(() => this.carregarUtilizador()),
      map(() => undefined),
    );
  }

  /** OP-04: carrega os dados da conta para o cabeçalho. */
  carregarUtilizador(): Observable<UtilizadorDto> {
    return this.contaApi.consultar().pipe(tap((utilizador) => this.utilizador.set(utilizador)));
  }

  /**
   * OP-03: termina a sessão no servidor e apaga sempre o token guardado, mesmo que o pedido falhe (RF-016).
   */
  terminarSessao(): Observable<void> {
    return this.authApi.terminarSessao().pipe(
      catchError(() => of(undefined)),
      finalize(() => {
        this.limpar();
        void this.router.navigate(['/entrar']);
      }),
    );
  }

  /** Repõe a sessão guardada no separador, se ainda não expirou (chamado no arranque). */
  restaurarSessao(): void {
    const guardada = this.tokenStorage.ler();
    if (guardada && new Date(guardada.expiraEm).getTime() > Date.now()) {
      this.ativar(guardada);
    } else {
      this.tokenStorage.limpar();
    }
  }

  /**
   * Sessão expirada ou recusada pelo servidor (401): apaga o token e volta ao início de sessão com o aviso (RF-017).
   */
  sessaoExpirada(): void {
    if (!this.autenticado()) {
      return;
    }

    this.limpar();
    void this.router.navigate(['/entrar'], { queryParams: { sessao: AVISO_SESSAO_TERMINADA } });
  }

  private ativar(sessao: SessaoDto): void {
    this.tokenStorage.guardar(sessao);
    this.sessao.set(sessao);
    clearTimeout(this.temporizador);
    const restante = new Date(sessao.expiraEm).getTime() - Date.now();
    this.temporizador = setTimeout(() => this.sessaoExpirada(), Math.max(restante, 0));
  }

  private limpar(): void {
    clearTimeout(this.temporizador);
    this.temporizador = undefined;
    this.tokenStorage.limpar();
    this.sessao.set(null);
    this.utilizador.set(null);
  }
}
