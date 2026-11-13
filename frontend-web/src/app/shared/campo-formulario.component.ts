import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

let sequencia = 0;

/**
 * Campo de formulário com rótulo, entrada e a regra violada junto ao campo (RNF-028). Mostra o primeiro erro do
 * controlo depois de o Utilizador lhe tocar ou de o formulário ser submetido; o valor escrito nunca é apagado.
 */
@Component({
  selector: 'app-campo-formulario',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="campo" [class.campo--erro]="erro">
      <label [for]="id">{{ rotulo() }}</label>
      <input
        [id]="id"
        [type]="tipo()"
        [formControl]="controlo()"
        [attr.autocomplete]="autocomplete()"
        [attr.min]="minimo()"
        [attr.max]="maximo()"
        [attr.aria-invalid]="erro ? 'true' : null"
        [attr.aria-describedby]="erro ? id + '-erro' : null"
      />
      @if (erro) {
        <p class="campo__erro" [id]="id + '-erro'">{{ erro }}</p>
      }
    </div>
  `,
})
export class CampoFormularioComponent {
  readonly rotulo = input.required<string>();
  readonly controlo = input.required<FormControl<string>>();
  readonly tipo = input<'text' | 'email' | 'password' | 'date' | 'tel'>('text');
  readonly autocomplete = input<string | null>(null);
  /** Valor mínimo do campo de data (atributo `min`). */
  readonly minimo = input<string | null>(null);
  /** Valor máximo do campo de data (atributo `max`). */
  readonly maximo = input<string | null>(null);

  protected readonly id = `campo-${++sequencia}`;

  /**
   * Primeiro erro, ou `null` se o campo estiver válido ou ainda não tiver sido tocado. Uma regra de validação aparece
   * com o rótulo do campo, como no protótipo v02 («Palavra-passe: tem de ter…», RNF-028); uma mensagem completa
   * (erro `mensagem`, por exemplo «Já existe uma conta com este e-mail.») aparece tal como está.
   */
  protected get erro(): string | null {
    const controlo = this.controlo();
    if (!controlo.errors || !controlo.touched) {
      return null;
    }

    const erros = controlo.errors as Record<string, unknown>;
    if (typeof erros['mensagem'] === 'string') {
      return erros['mensagem'];
    }

    const regra = Object.values(erros).find((v) => typeof v === 'string');
    const texto = typeof regra === 'string' ? regra : 'Valor inválido.';
    return `${this.rotulo()}: ${texto.charAt(0).toLocaleLowerCase('pt-PT')}${texto.slice(1)}`;
  }
}
