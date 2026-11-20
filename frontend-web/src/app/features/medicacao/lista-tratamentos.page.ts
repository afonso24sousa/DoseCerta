import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TratamentosApi } from '../../api/tratamentos.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { EstadoTratamento, TratamentoDto } from '../../models/tratamentos.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { ResultadoMedicacao, textoGrau, textoHorarios, textoPeriodo } from './apresentacao-tratamento';
import { ListaTomasComponent } from './lista-tomas.component';

/** Vista de E15: os tratamentos (I066) ou o calendário de tomas da ficha (OP-32, RF-063; I068). */
export type VistaMedicacao = 'tratamentos' | 'calendario';

/** Mensagens de sucesso vindas da rota. */
const RESULTADOS: Readonly<Record<string, string>> = {
  [ResultadoMedicacao.registado]: 'Tratamento registado. As tomas previstas foram geradas.',
};

/**
 * E15 Medicação da ficha (OP-28; UC-18): os tratamentos em curso e os terminados, em separadores (RF-054, RF-057), com o
 * medicamento, a dose, os horários, o grau de importância e o período (um tratamento sem data de fim é contínuo).
 * Cuidadores e Familiares consultam (RF-048); «Novo tratamento» só aparece ao Cuidador de uma ficha ativa (protótipo
 * v02, E15). Cada tratamento abre E17.
 */
@Component({
  selector: 'app-lista-tratamentos-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent, ListaTomasComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId()]">Voltar à ficha</a></p>

    @if (sucesso(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <div class="titulo-pagina">
      <h1>Medicação</h1>
      @if (podeRegistar()) {
        <a class="botao" [routerLink]="['/fichas', fichaId(), 'medicacao', 'novo']">Novo tratamento</a>
      }
    </div>
    @if (ficha(); as f) {
      <p>{{ f.nomeCompleto }}</p>
      @if (f.estado === 'Arquivada') {
        <p class="banner banner--aviso" role="status">
          Ficha arquivada: a medicação só pode ser consultada; os tratamentos em curso ainda podem ser
          terminados.
        </p>
      }
    }

    <!-- Separadores «Tratamentos» e «Calendário de tomas» (I068), comandados pelo signal vista e por ?vista= da rota. -->
    <div class="separadores" role="tablist" aria-label="Medicação">
      @for (s of separadores; track s.valor) {
        <button
          type="button"
          role="tab"
          class="separador"
          [attr.aria-selected]="vista() === s.valor"
          (click)="mudarVista(s.valor)"
        >
          {{ s.rotulo }}
        </button>
      }
    </div>

    @if (vista() === 'calendario') {
      <app-lista-tomas [fichaId]="fichaId()" />
    }
    @if (vista() === 'tratamentos') {
      <section class="cartao cartao--largo">
        <h2 class="so-leitor">Tratamentos</h2>
        <fieldset class="filtro">
          <legend>Mostrar</legend>
          @for (opcao of filtros; track opcao.valor) {
            <label class="opcao">
              <input
                type="radio"
                name="filtro-tratamentos"
                [value]="opcao.valor"
                [checked]="filtro() === opcao.valor"
                (change)="filtrar(opcao.valor)"
              />
              {{ opcao.rotulo }}
            </label>
          }
        </fieldset>

        @if (aCarregar()) {
          <p role="status">A carregar os tratamentos…</p>
        } @else if (tratamentos().length === 0 && !erro()) {
          <p>
            {{ filtro() === 'EmCurso' ? 'Não há tratamentos em curso.' : 'Não há tratamentos terminados.' }}
          </p>
        } @else if (tratamentos().length > 0) {
          <table class="tabela">
            <caption class="so-leitor">
              {{
                filtro() === 'EmCurso' ? 'Tratamentos em curso' : 'Tratamentos terminados'
              }}
            </caption>
            <thead>
              <tr>
                <th scope="col">Medicamento</th>
                <th scope="col">Horários</th>
                <th scope="col">Grau de importância</th>
                <th scope="col">Período</th>
              </tr>
            </thead>
            <tbody>
              @for (t of tratamentos(); track t.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/fichas', fichaId(), 'medicacao', t.id]"
                      >{{ t.medicamento }} {{ t.dose }}</a
                    >
                  </td>
                  <td>{{ horarios(t.horarios) }}</td>
                  <td>{{ grau(t.grauImportancia) }}</td>
                  <td>{{ periodo(t) }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    }
  `,
})
export class ListaTratamentosPage implements OnChanges {
  private readonly tratamentosApi = inject(TratamentosApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `resultado` da rota, depois de registar. */
  readonly resultado = input<string | undefined>();

  private readonly router = inject(Router);
  private readonly rota = inject(ActivatedRoute);

  protected readonly separadores: readonly { valor: VistaMedicacao; rotulo: string }[] = [
    { valor: 'tratamentos', rotulo: 'Tratamentos' },
    { valor: 'calendario', rotulo: 'Calendário de tomas' },
  ];

  protected readonly filtros: readonly { valor: EstadoTratamento; rotulo: string }[] = [
    { valor: 'EmCurso', rotulo: 'Em curso' },
    { valor: 'Terminado', rotulo: 'Terminados' },
  ];
  protected readonly horarios = textoHorarios;
  protected readonly grau = textoGrau;
  protected readonly periodo = textoPeriodo;

  /** Vista atual: os tratamentos ou o calendário de tomas. */
  readonly vista = signal<VistaMedicacao>('tratamentos');

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly filtro = signal<EstadoTratamento>('EmCurso');
  protected readonly tratamentos = signal<TratamentoDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);

  /** «Novo tratamento» só para o Cuidador de uma ficha ativa (RF-050, RF-120). */
  protected readonly podeRegistar = computed(() => {
    const ficha = this.ficha();
    return !!ficha && ficha.meuPapel === 'Cuidador' && ficha.estado === 'Ativa';
  });

  constructor() {
    // Parâmetro `vista` da rota: `?vista=calendario` abre o calendário de tomas; sem ele, os tratamentos. Lido aqui,
    // e não como entrada, porque o nome coincide com o signal vista.
    this.rota.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe((parametros) =>
        this.vista.set(parametros.get('vista') === 'calendario' ? 'calendario' : 'tratamentos'),
      );
  }

  ngOnChanges(): void {
    this.erro.set(null);
    this.sucesso.set(RESULTADOS[this.resultado() ?? ''] ?? null);
    const atual = this.contexto.ficha();
    this.ficha.set(atual?.id === this.fichaId() ? atual : null);
    this.contexto.obter(this.fichaId()).subscribe({
      next: (ficha) => this.ficha.set(ficha),
      error: (problema: ProblemaApi) => this.erro.set(this.mensagens.mensagem(problema)),
    });
    this.carregar();
  }

  /** Muda de separador e atualiza `?vista=` na rota, sem acrescentar uma entrada ao histórico. */
  protected mudarVista(vista: VistaMedicacao): void {
    this.vista.set(vista);
    this.sucesso.set(null);
    void this.router.navigate([], {
      relativeTo: this.rota,
      queryParams: { vista: vista === 'calendario' ? 'calendario' : null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected filtrar(filtro: EstadoTratamento): void {
    this.filtro.set(filtro);
    this.sucesso.set(null);
    this.carregar();
  }

  /** OP-28 com o filtro escolhido. */
  private carregar(): void {
    const filtro = this.filtro();
    this.aCarregar.set(true);
    this.erro.set(null);
    this.tratamentosApi.listar(this.fichaId(), filtro).subscribe({
      next: (tratamentos) => {
        if (filtro !== this.filtro()) {
          return;
        }

        this.tratamentos.set(
          [...tratamentos].sort((a, b) => a.medicamento.localeCompare(b.medicamento, 'pt')),
        );
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        if (filtro !== this.filtro()) {
          return;
        }

        this.tratamentos.set([]);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
