import { Component, input } from '@angular/core';

/**
 * Página provisória das entradas da navegação que ainda não estão implementadas (I057). Não tem funcionalidade: cada
 * Issue da m4 substitui a sua rota pela página real.
 */
@Component({
  selector: 'app-em-construcao-page',
  standalone: true,
  template: `
    <h1>{{ titulo() }}</h1>
    <p>Esta página ainda está em construção.</p>
  `,
})
export class EmConstrucaoPage {
  /** Título da página, vindo dos dados da rota. */
  readonly titulo = input('Em construção');
}
