import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DominiosApi } from '../../api/dominios.api';
import { FichasApi } from '../../api/fichas.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto, ListasNecessidadesDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import {
  ControlosDadosFicha,
  FormularioFichaComponent,
  criarControlosDadosFicha,
  dadosDosControlos,
  documentosDosControlos,
} from './formulario-ficha.component';
import { permissoesFicha } from './permissoes-ficha';
import { ResultadoFicha } from './resultados-ficha';

/**
 * Edição de uma ficha (OP-11; UC-08, RF-029), com os mesmos campos e regras da criação, sem o tipo de ficha nem o
 * Cuidador principal, e com a possibilidade de acrescentar documentos. Só aparece a quem
 * {@link permissoesFicha} permite editar; em qualquer falha os dados e os ficheiros escolhidos mantêm-se (RF-023).
 */
@Component({
  selector: 'app-editar-ficha-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BannerErroComponent, FormularioFichaComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId()]">Voltar à ficha</a></p>
    <h1>Editar ficha</h1>
    <app-banner-erro [mensagem]="erroCarregar()" />

    @let f = ficha();
    @let e = edicao();
    @if (aCarregar()) {
      <p role="status">A carregar a ficha…</p>
    } @else if (f) {
      @if (!podeEditar()) {
        <p class="banner banner--aviso" role="status">
          @if (f.estado === 'Arquivada') {
            A ficha está arquivada e só pode ser consultada. Reative-a para a editar.
          } @else {
            O seu papel nesta ficha só permite consultar os dados.
          }
        </p>
      } @else if (e) {
        <section class="cartao cartao--largo">
          <app-banner-erro [mensagem]="erro()" />
          <form [formGroup]="e.formulario" (ngSubmit)="guardar()" novalidate>
            <app-formulario-ficha [controlos]="e.controlos" [listas]="listas()" />
            <div class="acoes">
              <button type="submit" class="botao" [disabled]="aEnviar()">
                {{ aEnviar() ? 'A guardar…' : 'Guardar alterações' }}
              </button>
              <a class="botao botao--secundario" [routerLink]="['/fichas', f.id]">Cancelar</a>
            </div>
          </form>
        </section>
      }
    }
  `,
})
export class EditarFichaPage implements OnInit {
  private readonly fichasApi = inject(FichasApi);
  private readonly dominiosApi = inject(DominiosApi);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly listas = signal<ListasNecessidadesDto | null>(null);
  /** Controlos da ficha e o formulário que os agrupa, criados com os dados atuais. */
  protected readonly edicao = signal<{ controlos: ControlosDadosFicha; formulario: FormGroup } | null>(null);
  protected readonly podeEditar = signal(false);
  protected readonly aCarregar = signal(true);
  protected readonly aEnviar = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly erroCarregar = signal<string | null>(null);

  ngOnInit(): void {
    forkJoin({
      ficha: this.fichasApi.consultar(this.fichaId()),
      listas: this.dominiosApi.necessidades(),
    }).subscribe({
      next: ({ ficha, listas }) => {
        const controlos = criarControlosDadosFicha(ficha);
        this.ficha.set(ficha);
        this.listas.set(listas);
        this.edicao.set({ controlos, formulario: new FormGroup({ ...controlos }) });
        this.podeEditar.set(permissoesFicha(ficha).editar);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.erroCarregar.set(this.mensagens.mensagem(problema));
      },
    });
  }

  protected guardar(): void {
    const edicao = this.edicao();
    if (!edicao) {
      return;
    }

    const { controlos, formulario } = edicao;
    this.erro.set(null);
    formulario.markAllAsTouched();
    if (formulario.invalid || this.aEnviar()) {
      return;
    }

    this.aEnviar.set(true);
    this.fichasApi
      .editar(this.fichaId(), dadosDosControlos(controlos), documentosDosControlos(controlos))
      .subscribe({
        next: (ficha) =>
          void this.router.navigate(['/fichas', ficha.id], {
            queryParams: { resultado: ResultadoFicha.editada },
          }),
        error: (problema: ProblemaApi) => {
          // Nada é limpo: os dados e os ficheiros escolhidos ficam para nova submissão (RF-023).
          this.aEnviar.set(false);
          V.aplicarErrosDoServidor(formulario, problema);
          this.erro.set(this.mensagens.mensagem(problema));
          if (problema.codigo === 'FICHA_ARQUIVADA') {
            // Arquivada entretanto por outro Cuidador: passa a só de consulta (RF-121).
            this.ficha.update((ficha) => ficha && { ...ficha, estado: 'Arquivada' });
          }

          if (problema.codigo === 'FICHA_ARQUIVADA' || problema.codigo === 'ACESSO_PROIBIDO') {
            this.podeEditar.set(false);
            this.erroCarregar.set(this.mensagens.mensagem(problema));
          }
        },
      });
  }
}
