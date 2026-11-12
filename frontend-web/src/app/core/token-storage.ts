import { Injectable } from '@angular/core';
import { SessaoDto } from '../models/conta.models';

const CHAVE = 'dosecerta.sessao';

/**
 * Guarda a sessão (token JWT e expiração) no `sessionStorage` do separador (RNF-054): desaparece ao fechar o separador
 * e nunca vai para o `localStorage`.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  /** Lê a sessão guardada, ou `null` se não existir ou estiver corrompida. */
  ler(): SessaoDto | null {
    try {
      const texto = sessionStorage.getItem(CHAVE);
      if (!texto) {
        return null;
      }

      const sessao = JSON.parse(texto) as Partial<SessaoDto>;
      return typeof sessao.token === 'string' && typeof sessao.expiraEm === 'string'
        ? { token: sessao.token, tipo: sessao.tipo ?? 'Bearer', expiraEm: sessao.expiraEm }
        : null;
    } catch {
      return null;
    }
  }

  /** Guarda a sessão. */
  guardar(sessao: SessaoDto): void {
    try {
      sessionStorage.setItem(CHAVE, JSON.stringify(sessao));
    } catch {
      // Sem armazenamento (por exemplo navegação privada restrita): a sessão fica só em memória.
    }
  }

  /** Apaga a sessão guardada. */
  limpar(): void {
    try {
      sessionStorage.removeItem(CHAVE);
    } catch {
      // Nada a apagar.
    }
  }
}
