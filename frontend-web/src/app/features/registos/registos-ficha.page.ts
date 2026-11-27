import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RegistosApi } from '../../api/registos.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { RegistoDto, TipoRegisto } from '../../models/registos.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { ResultadoRegisto, textoInstante } from './apresentacao-registo';

/** Mensagens de sucesso vindas de E24 pela rota. */
const RESULTADOS: Readonly<Record<string, string>> = {
  [ResultadoRegisto.ocorrencia]: 'Ocorrência registada.',
  [ResultadoRegisto.nota]: 'Nota registada.',
};

/**
 * E23 Registos da ficha (OP-48 e OP-49; UC-29): as ocorrências diárias e, só para os Cuidadores, as notas partilhadas,
 * em separadores, do registo mais recente para o mais antigo. Cada registo mostra a descrição, o autor, o momento do
 * registo e, nas ocorrências, quando aconteceu (RF-092). Os registos não podem ser alterados nem eliminados (RF-090,
 * RNF-049), por isso não há ações sobre eles. Os registos dos outros Cuidadores aparecem ao abrir ou ao atualizar o ecrã
 * (RF-093). O Familiar vê as ocorrências (RF-094) e não vê as notas (RF-095). «Novo registo» só aparece ao Cuidador de
 * uma ficha ativa (RF-090, RF-120).
 */
@Component({
  selector: 'app-registos-ficha-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId()]">Voltar à ficha</a></p>

    @if (sucesso(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <div class="titulo-pagina">
      <h1>Ocorrências e notas</h1>
      @if (podeRegistar()) {
        <a
          class="botao"
          [routerLink]="['/fichas', fichaId(), 'registos', 'novo']"
          [queryParams]="{ tipo: tipoAtual() }"
          >Novo registo</a
        >
      }
    </div>
    @if (ficha(); as f) {
      <p>{{ f.nomeCompleto }}</p>
      @if (f.estado === 'Arquivada') {
        <p class="banner banner--aviso" role="status">
          Ficha arquivada: os registos só podem ser consultados. Para registar, reative a ficha.
        </p>
      } @else if (f.meuPapel === 'Familiar') {
        <p class="banner banner--aviso">
          Como Familiar, pode consultar as ocorrências da ficha. As notas são só para os Cuidadores.
        </p>
      }
      <p class="ajuda">Os registos não se alteram nem se eliminam.</p>
    }

    @if (eCuidador()) {
      <div class="separadores" role="tablist" aria-label="Registos da ficha">
        @for (s of separadores; track s.valor) {
          <button
            type="button"
            role="tab"
            class="separador"
            [attr.aria-selected]="tipoAtual() === s.valor"
            (click)="mudarTipo(s.valor)"
          >
            {{ s.rotulo }}
          </button>
        }
      </div>
    }

    <section class="cartao cartao--largo" aria-live="polite">
      <div class="titulo-pagina">
        <h2>{{ tipoAtual() === 'Nota' ? 'Notas para os Cuidadores' : 'Ocorrências' }}</h2>
        <button type="button" class="botao botao--secundario" (click)="carregar()" [disabled]="aCarregar()">
          Atualizar
        </button>
      </div>

      @if (aCarregar()) {
        <p role="status">
          {{ tipoAtual() === 'Nota' ? 'A carregar as notas…' : 'A carregar as ocorrências…' }}
        </p>
      } @else if (falhou()) {
        <p>
          <button type="button" class="botao botao--secundario" (click)="carregar()">Tentar novamente</button>
        </p>
      } @else if (registos().length === 0) {
        <p>
          {{
            tipoAtual() === 'Nota'
              ? 'Ainda não há notas registadas nesta ficha.'
              : 'Ainda não há ocorrências registadas nesta ficha.'
          }}
        </p>
      } @else {
        <ul class="lista-registos">
          @for (r of registos(); track r.id) {
            <li class="lista-registos__item">
              <p class="lista-registos__descricao">{{ r.descricao }}</p>
              @if (r.ocorridaEm) {
                <p><strong>Aconteceu em</strong> {{ instante(r.ocorridaEm) }}</p>
              }
              <p class="ajuda">Registada em {{ instante(r.criadaEm) }} por {{ r.autor.nome }}</p>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class RegistosFichaPage implements OnChanges {
  private readonly registosApi = inject(RegistosApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `resultado` da rota, depois de registar em E24. */
  readonly resultado = input<string | undefined>();

  /** Parâmetro `tipo` da rota: `Nota` abre o separador das notas (só para os Cuidadores). */
  readonly tipo = input<string | undefined>();

  protected readonly separadores: readonly { valor: TipoRegisto; rotulo: string }[] = [
    { valor: 'Ocorrencia', rotulo: 'Ocorrências' },
    { valor: 'Nota', rotulo: 'Notas' },
  ];
  protected readonly instante = textoInstante;

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly tipoAtual = signal<TipoRegisto>('Ocorrencia');
  protected readonly registos = signal<RegistoDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly falhou = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);

  protected readonly eCuidador = computed(() => this.ficha()?.meuPapel === 'Cuidador');

  /** «Novo registo» só para o Cuidador de uma ficha ativa (RF-090, RF-091, RF-120). */
  protected readonly podeRegistar = computed(() => {
    const ficha = this.ficha();
    return !!ficha && ficha.meuPapel === 'Cuidador' && ficha.estado === 'Ativa';
  });

  ngOnChanges(): void {
    this.erro.set(null);
    this.sucesso.set(RESULTADOS[this.resultado() ?? ''] ?? null);
    const atual = this.contexto.ficha();
    this.ficha.set(atual?.id === this.fichaId() ? atual : null);
    this.contexto.obter(this.fichaId()).subscribe({
      next: (ficha) => {
        this.ficha.set(ficha);
        // O Familiar só vê as ocorrências (RF-095): nunca abre o separador das notas.
        this.tipoAtual.set(this.tipo() === 'Nota' && ficha.meuPapel === 'Cuidador' ? 'Nota' : 'Ocorrencia');
        this.carregar();
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.falhou.set(true);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  protected mudarTipo(tipo: TipoRegisto): void {
    if (tipo === this.tipoAtual()) {
      return;
    }

    this.tipoAtual.set(tipo);
    this.sucesso.set(null);
    this.carregar();
  }

  /** OP-48 ou OP-49, conforme o separador; também serve para atualizar a lista (RF-093). */
  protected carregar(): void {
    const tipo = this.tipoAtual();
    this.aCarregar.set(true);
    this.falhou.set(false);
    this.erro.set(null);
    this.registosApi.listar(this.fichaId(), tipo).subscribe({
      next: (registos) => {
        if (tipo !== this.tipoAtual()) {
          return;
        }

        this.registos.set(registos);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        if (tipo !== this.tipoAtual()) {
          return;
        }

        this.registos.set([]);
        this.aCarregar.set(false);
        this.falhou.set(true);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
