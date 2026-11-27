import { Component, OnChanges, OnInit, computed, inject, input, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AgendaApi } from '../../api/agenda.api';
import { FichasApi } from '../../api/fichas.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ItemAgendaDto, VistaAgenda } from '../../models/agenda.models';
import { FichaResumoDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { APRESENTACAO_ESTADOS, ApresentacaoEstadoToma, hojeEmLisboa } from '../medicacao/apresentacao-toma';
import { formatarData } from '../fichas/apresentacao-ficha';
import {
  CABECALHOS_SEMANA,
  VISTAS,
  deslocarData,
  diasDaVista,
  horaDoEvento,
  nomeDiaSemana,
  semanas,
  tituloDaVista,
} from './apresentacao-agenda';

/** Mensagens do resultado de E22, recebidas no parâmetro `resultado`. */
export const RESULTADOS_AGENDA: Readonly<Record<string, string>> = {
  registado: 'Compromisso registado.',
  editado: 'Compromisso atualizado.',
  removido: 'Compromisso removido.',
};

/** Ícone dos compromissos (Material `event`), sempre com o texto «Compromisso» (RNF-027). */
const DESENHO_COMPROMISSO =
  'M17 12h-5v5h5v-5zM16 1v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2h-1V1h-2zm3 18H5V8h14v11z';

function vistaValida(valor: string | undefined): VistaAgenda {
  return valor === 'semana' || valor === 'mes' ? valor : 'dia';
}

function dataValida(valor: string | undefined): string {
  return valor && /^\d{4}-\d{2}-\d{2}$/.test(valor) ? valor : hojeEmLisboa();
}

/**
 * E21 Agenda (OP-45; UC-26): os compromissos e as tomas das fichas ativas em que o Utilizador é Cuidador ou Familiar
 * (RF-083), em vista diária ordenada por hora (RF-084), semanal de segunda-feira a domingo (RF-085) e mensal (RF-086),
 * com o nome do utente em cada evento (RF-089). Cada vista é um só pedido à OP-45 (RNF-012). Os compromissos não se
 * confirmam (RF-087) e as tomas não se alteram a partir da agenda (RF-088). «Novo compromisso» e «Editar» só aparecem
 * nas fichas ativas em que o Utilizador é Cuidador (RF-048, RF-120). A vista e a data ficam no endereço
 * (`?vista=semana&data=2026-11-25`), para o regresso de E22 voltar ao mesmo sítio.
 */
@Component({
  selector: 'app-agenda-page',
  standalone: true,
  imports: [FormsModule, NgTemplateOutlet, RouterLink, BannerErroComponent],
  styles: `
    .agenda__navegacao {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem;
      margin: 1rem 0;
    }

    .agenda__titulo {
      margin: 0 0.5rem;
      font-size: 1.15rem;
    }

    .eventos {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .evento {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.25rem 0.75rem;
      padding: 0.5rem 0;
      border-bottom: 1px solid var(--cor-borda);
    }

    .evento:last-child {
      border-bottom: none;
    }

    .evento__hora {
      font-weight: 600;
      min-width: 3rem;
    }

    .evento__tipo {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
    }

    .evento__icone {
      width: 1.25rem;
      height: 1.25rem;
      fill: currentColor;
    }

    .dia-semana h3 {
      margin: 1rem 0 0.25rem;
      font-size: 1rem;
    }

    .mes {
      width: 100%;
      table-layout: fixed;
      border-collapse: collapse;
    }

    .mes th,
    .mes td {
      border: 1px solid var(--cor-borda);
      padding: 0.25rem;
      vertical-align: top;
      font-size: 0.85rem;
    }

    .mes td.fora {
      color: #6b7280;
      background: var(--cor-fundo);
    }

    .mes ul {
      list-style: none;
      margin: 0.25rem 0 0;
      padding: 0;
    }

    .mes li {
      overflow-wrap: anywhere;
    }

    .novo-compromisso {
      display: flex;
      flex-wrap: wrap;
      align-items: end;
      gap: 0.5rem;
      margin: 1rem 0 0.75rem;
    }
  `,
  template: `
    <div class="titulo-pagina">
      <h1>Agenda</h1>
    </div>

    @if (mensagemResultado(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <div class="separadores" role="tablist" aria-label="Vista da agenda">
      @for (v of vistas; track v.valor) {
        <button
          type="button"
          role="tab"
          class="separador"
          [attr.aria-selected]="vistaAtual() === v.valor"
          (click)="irPara(v.valor, dataAtual())"
        >
          {{ v.rotulo }}
        </button>
      }
    </div>

    <div class="agenda__navegacao">
      <button type="button" class="botao botao--secundario" (click)="deslocar(-1)">
        {{ rotuloAnterior() }}
      </button>
      <button type="button" class="botao botao--secundario" (click)="irPara(vistaAtual(), hoje())">
        Hoje
      </button>
      <button type="button" class="botao botao--secundario" (click)="deslocar(1)">
        {{ rotuloSeguinte() }}
      </button>
      <h2 class="agenda__titulo" aria-live="polite">{{ titulo() }}</h2>
    </div>

    <section class="cartao cartao--largo" [attr.aria-busy]="aCarregar()">
      @if (aCarregar()) {
        <p role="status">A carregar a agenda…</p>
      } @else if (vistaAtual() === 'mes') {
        <table class="mes">
          <caption class="so-leitor">
            {{
              titulo()
            }}
          </caption>
          <thead>
            <tr>
              @for (c of cabecalhos; track c) {
                <th scope="col">{{ c }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (semana of semanasDoMes(); track semana[0].dia) {
              <tr>
                @for (d of semana; track d.dia) {
                  <td [class.fora]="!d.doPeriodo">
                    <a
                      [routerLink]="[]"
                      [queryParams]="{ vista: 'dia', data: d.dia }"
                      [attr.aria-label]="'Ver o dia ' + formatar(d.dia)"
                      >{{ d.dia.slice(8) }}</a
                    >
                    @if (d.itens.length > 0) {
                      <ul>
                        @for (item of d.itens; track item.tipo + item.id) {
                          <li>
                            {{ hora(item) }} {{ item.nomeFicha }}:
                            {{ item.tipo === 'Toma' ? 'Toma' : 'Compromisso' }} {{ item.titulo }}
                            @if (item.estado) {
                              ({{ estadoDe(item).texto }})
                            }
                          </li>
                        }
                      </ul>
                    }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      } @else if (itens().length === 0 && !erro()) {
        <p>Não há eventos neste período.</p>
      } @else if (vistaAtual() === 'semana') {
        @for (d of diasVisiveis(); track d.dia) {
          <div class="dia-semana">
            <h3>{{ nomeDia(d.dia) }}, {{ formatar(d.dia) }}</h3>
            @if (d.itens.length === 0) {
              <p>Sem eventos.</p>
            } @else {
              <ng-container *ngTemplateOutlet="lista; context: { $implicit: d.itens }" />
            }
          </div>
        }
      } @else {
        <ng-container *ngTemplateOutlet="lista; context: { $implicit: itens() }" />
      }

      @if (fichasDeEscrita().length > 0) {
        <div class="novo-compromisso">
          @if (fichasDeEscrita().length > 1) {
            <div class="campo">
              <label for="agenda-ficha-novo">Ficha do novo compromisso</label>
              <select id="agenda-ficha-novo" [(ngModel)]="fichaNovo">
                @for (f of fichasDeEscrita(); track f.id) {
                  <option [value]="f.id">{{ f.nomeCompleto }}</option>
                }
              </select>
            </div>
          }
          <a class="botao" [routerLink]="['/fichas', fichaNovoEfetiva(), 'compromissos', 'novo']"
            >Novo compromisso</a
          >
        </div>
      }
      <p class="ajuda">
        Os compromissos não se confirmam e as tomas só se alteram no tratamento. As fichas arquivadas não
        aparecem.
      </p>
    </section>

    <ng-template #lista let-itens>
      <ul class="eventos">
        @for (item of itens; track item.tipo + item.id) {
          <li class="evento">
            <span class="evento__hora">{{ hora(item) }}</span>
            <span class="evento__ficha">{{ item.nomeFicha }}</span>
            @if (item.tipo === 'Toma') {
              <span class="evento__tipo">
                <svg class="evento__icone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                  <path [attr.d]="estadoDe(item).desenho" />
                </svg>
                Toma: {{ item.titulo }} ({{ estadoDe(item).texto }}, só consulta)
              </span>
            } @else {
              <span class="evento__tipo">
                <svg class="evento__icone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                  <path [attr.d]="desenhoCompromisso" />
                </svg>
                Compromisso: {{ item.titulo }}
              </span>
              @if (podeEditar(item)) {
                <a
                  [routerLink]="['/fichas', item.fichaId, 'compromissos', item.id, 'editar']"
                  [queryParams]="regresso()"
                  >Editar<span class="so-leitor"> o compromisso {{ item.titulo }}</span></a
                >
              }
            }
          </li>
        }
      </ul>
    </ng-template>
  `,
})
export class AgendaPage implements OnInit, OnChanges {
  private readonly agendaApi = inject(AgendaApi);
  private readonly fichasApi = inject(FichasApi);
  private readonly mensagens = inject(MensagensErroService);
  private readonly router = inject(Router);

  /** Parâmetro `vista` do endereço (`dia`, `semana` ou `mes`). */
  readonly vista = input<string | undefined>();
  /** Parâmetro `data` do endereço (`AAAA-MM-DD`); por omissão, hoje em Lisboa. */
  readonly data = input<string | undefined>();
  /** Parâmetro `resultado` do endereço, vindo de E22. */
  readonly resultado = input<string | undefined>();

  protected readonly vistas = VISTAS;
  protected readonly cabecalhos = CABECALHOS_SEMANA;
  protected readonly desenhoCompromisso = DESENHO_COMPROMISSO;
  protected readonly hora = horaDoEvento;
  protected readonly nomeDia = nomeDiaSemana;
  protected readonly formatar = formatarData;

  protected readonly itens = signal<ItemAgendaDto[]>([]);
  protected readonly fichas = signal<FichaResumoDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected fichaNovo = '';

  protected readonly vistaAtual = computed(() => vistaValida(this.vista()));
  protected readonly dataAtual = computed(() => dataValida(this.data()));
  protected readonly hoje = computed(() => hojeEmLisboa());
  protected readonly titulo = computed(() => tituloDaVista(this.vistaAtual(), this.dataAtual()));
  protected readonly diasVisiveis = computed(() =>
    diasDaVista(this.vistaAtual(), this.dataAtual(), this.itens()),
  );
  protected readonly semanasDoMes = computed(() => semanas(this.diasVisiveis()));
  protected readonly mensagemResultado = computed(() => RESULTADOS_AGENDA[this.resultado() ?? ''] ?? null);
  protected readonly regresso = computed(() => ({ vista: this.vistaAtual(), data: this.dataAtual() }));

  /** Fichas ativas em que o Utilizador é Cuidador: só nelas pode registar e editar compromissos (RF-048, RF-120). */
  protected readonly fichasDeEscrita = computed(() =>
    this.fichas().filter((f) => f.meuPapel === 'Cuidador' && f.estado === 'Ativa'),
  );

  protected readonly rotuloAnterior = computed(
    () => ({ dia: 'Dia anterior', semana: 'Semana anterior', mes: 'Mês anterior' })[this.vistaAtual()],
  );
  protected readonly rotuloSeguinte = computed(
    () => ({ dia: 'Dia seguinte', semana: 'Semana seguinte', mes: 'Mês seguinte' })[this.vistaAtual()],
  );

  ngOnInit(): void {
    // A lista resumida das fichas (OP-07) só serve para saber onde o Utilizador é Cuidador; é lida uma vez.
    this.fichasApi.listar().subscribe({
      next: (fichas) => this.fichas.set(fichas),
      error: () => this.fichas.set([]),
    });
  }

  ngOnChanges(): void {
    this.carregar();
  }

  protected fichaNovoEfetiva(): string {
    const fichas = this.fichasDeEscrita();
    return fichas.some((f) => f.id === this.fichaNovo) ? this.fichaNovo : (fichas[0]?.id ?? '');
  }

  protected estadoDe(item: ItemAgendaDto): ApresentacaoEstadoToma {
    return APRESENTACAO_ESTADOS[item.estado ?? 'Pendente'];
  }

  protected podeEditar(item: ItemAgendaDto): boolean {
    return this.fichasDeEscrita().some((f) => f.id === item.fichaId);
  }

  protected irPara(vista: VistaAgenda, data: string): void {
    void this.router.navigate([], { queryParams: { vista, data } });
  }

  protected deslocar(sentido: -1 | 1): void {
    this.irPara(this.vistaAtual(), deslocarData(this.vistaAtual(), this.dataAtual(), sentido));
  }

  /** OP-45, um só pedido por vista (RNF-012). */
  private carregar(): void {
    this.aCarregar.set(true);
    this.erro.set(null);
    this.agendaApi.consultar(this.vistaAtual(), this.dataAtual()).subscribe({
      next: (itens) => {
        this.itens.set(itens);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.itens.set([]);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
