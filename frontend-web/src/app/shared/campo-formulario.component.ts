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
  readonly tipo = input<'text' | 'email' | 'password'>('text');
  readonly autocomplete = input<string | null>(null);

  protected readonly id = `campo-${++sequencia}`;

  /** Primeira regra violada, ou `null` se o campo estiver válido ou ainda não tiver sido tocado. */
  protected get erro(): string | null {
    const controlo = this.controlo();
    if (!controlo.errors || !controlo.touched) {
      return null;
    }

    const primeiro = Object.values(controlo.errors as Record<string, unknown>).find(
      (v) => typeof v === 'string',
    );
    return typeof primeiro === 'string' ? primeiro : 'Valor inválido.';
  }
}
