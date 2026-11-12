import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { BarraLateralComponent } from './barra-lateral.component';
import { CabecalhoComponent } from './cabecalho.component';

/**
 * Estrutura das páginas autenticadas: cabeçalho, barra lateral e a página atual. Ao abrir depois de recarregar o
 * separador, carrega os dados da conta para o cabeçalho (OP-04).
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, CabecalhoComponent, BarraLateralComponent],
  template: `
    <app-cabecalho />
    <div class="shell">
      <app-barra-lateral />
      <main class="shell__conteudo">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);

  ngOnInit(): void {
    if (this.auth.utilizador() === null) {
      // Um 401 já é tratado pelo authInterceptor; outros erros só deixam o nome por mostrar.
      this.auth.carregarUtilizador().subscribe({ error: () => undefined });
    }
  }
}
