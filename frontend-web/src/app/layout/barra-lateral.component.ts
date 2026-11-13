import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/** Entrada da barra lateral. */
interface EntradaNavegacao {
  rotulo: string;
  caminho: string;
}

/**
 * Navegação principal das páginas autenticadas. Só a Conta está implementada nesta versão (I057); as restantes entradas
 * abrem a página provisória até às Issues de cada funcionalidade.
 */
@Component({
  selector: 'app-barra-lateral',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="barra-lateral" aria-label="Navegação principal">
      <ul>
        @for (entrada of entradas; track entrada.caminho) {
          <li>
            <a [routerLink]="entrada.caminho" routerLinkActive="ativa" ariaCurrentWhenActive="page">{{
              entrada.rotulo
            }}</a>
          </li>
        }
      </ul>
    </nav>
  `,
})
export class BarraLateralComponent {
  protected readonly entradas: readonly EntradaNavegacao[] = [
    { rotulo: 'Lista diária', caminho: '/hoje' },
    { rotulo: 'Fichas', caminho: '/fichas' },
    { rotulo: 'Agenda', caminho: '/agenda' },
    { rotulo: 'Convites', caminho: '/convites' },
    { rotulo: 'Notificações', caminho: '/notificacoes' },
    { rotulo: 'Conta', caminho: '/conta' },
  ];
}
