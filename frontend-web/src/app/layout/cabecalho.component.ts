import { Component, inject } from '@angular/core';
import { AuthService } from '../core/auth.service';

/**
 * Cabeçalho das páginas autenticadas: nome da aplicação, nome do Utilizador e "Terminar sessão" (RF-016).
 */
@Component({
  selector: 'app-cabecalho',
  standalone: true,
  template: `
    <header class="cabecalho">
      <span class="cabecalho__marca">DoseCerta</span>
      <span class="cabecalho__utilizador">{{ auth.utilizador()?.nome }}</span>
      <button type="button" class="botao botao--secundario" (click)="terminarSessao()">
        Terminar sessão
      </button>
    </header>
  `,
})
export class CabecalhoComponent {
  protected readonly auth = inject(AuthService);

  protected terminarSessao(): void {
    this.auth.terminarSessao().subscribe();
  }
}
