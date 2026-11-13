import { Component, input } from '@angular/core';

/**
 * Mensagem de erro geral de um ecrã (por exemplo credenciais inválidas ou falta de ligação), anunciada pelos leitores
 * de ecrã. Os erros de um campo aparecem junto dele, no {@link CampoFormularioComponent}.
 */
@Component({
  selector: 'app-banner-erro',
  standalone: true,
  template: `
    @if (mensagem()) {
      <p class="banner banner--erro" role="alert">{{ mensagem() }}</p>
    }
  `,
})
export class BannerErroComponent {
  readonly mensagem = input<string | null>(null);
}
