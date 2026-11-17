import { Component, input, signal } from '@angular/core';
import { FormControl } from '@angular/forms';

let sequencia = 0;

/** Hora `HH:mm` válida. */
const PADRAO_HORA = /^([01]\d|2[0-3]):[0-5]\d$/;

/**
 * Lista de horas `HH:mm` (horários do tratamento, RF-050) com acrescentar e remover (modelo de classes do frontend web
 * v02, secção 6.2). Mantém as horas ordenadas e sem repetições, como o servidor exige; o valor fica no controlo
 * recebido, e o erro do controlo (por exemplo a lista vazia) aparece junto do campo (RNF-028).
 */
@Component({
  selector: 'app-lista-horas',
  standalone: true,
  template: `
    <fieldset
      class="grupo"
      [class.campo--erro]="erro()"
      [attr.aria-describedby]="erro() ? id + '-erro' : null"
    >
      <legend>{{ rotulo() }}</legend>
      @if (controlo().value.length > 0) {
        <ul class="lista-horas">
          @for (hora of controlo().value; track hora; let i = $index) {
            <li>
              <span>{{ hora }}</span>
              <button
                type="button"
                class="botao botao--secundario"
                [attr.aria-label]="'Remover a hora ' + hora"
                (click)="remover(i)"
              >
                Remover
              </button>
            </li>
          }
        </ul>
      } @else {
        <p class="ajuda">Ainda não indicou nenhuma hora.</p>
      }
      <div class="campo">
        <label [for]="id">Nova hora</label>
        <input
          #nova
          [id]="id"
          type="time"
          step="60"
          (keydown.enter)="$event.preventDefault(); acrescentar(nova)"
        />
      </div>
      <button type="button" class="botao botao--secundario" (click)="acrescentar(nova)">
        Acrescentar hora
      </button>
      @if (erro(); as mensagem) {
        <p class="campo__erro" [id]="id + '-erro'">{{ mensagem }}</p>
      }
    </fieldset>
  `,
})
export class ListaHorasComponent {
  readonly rotulo = input('Horários (uma ou mais horas fixas por dia)');
  readonly controlo = input.required<FormControl<string[]>>();

  protected readonly id = `lista-horas-${++sequencia}`;
  private readonly erroLocal = signal<string | null>(null);

  /** Erro a mostrar: o da última hora recusada ou o primeiro do controlo, depois de tocado. */
  protected erro(): string | null {
    const local = this.erroLocal();
    if (local) {
      return local;
    }

    const controlo = this.controlo();
    if (!controlo.errors || !controlo.touched) {
      return null;
    }

    const regra = Object.values(controlo.errors as Record<string, unknown>).find(
      (v) => typeof v === 'string',
    );
    return typeof regra === 'string' ? regra : 'Valor inválido.';
  }

  /** Acrescenta a hora escrita, se for válida e ainda não estiver na lista. */
  protected acrescentar(campo: HTMLInputElement): void {
    const hora = campo.value.slice(0, 5);
    if (!PADRAO_HORA.test(hora)) {
      this.erroLocal.set('Indique uma hora no formato hh:mm, por exemplo 08:00.');
      return;
    }

    const controlo = this.controlo();
    if (controlo.value.includes(hora)) {
      this.erroLocal.set(`A hora ${hora} já está na lista; não pode repetir a mesma hora.`);
      return;
    }

    this.erroLocal.set(null);
    controlo.setValue([...controlo.value, hora].sort());
    controlo.markAsTouched();
    controlo.markAsDirty();
    campo.value = '';
  }

  protected remover(indice: number): void {
    const controlo = this.controlo();
    this.erroLocal.set(null);
    controlo.setValue(controlo.value.filter((_, i) => i !== indice));
    controlo.markAsTouched();
    controlo.markAsDirty();
  }
}
