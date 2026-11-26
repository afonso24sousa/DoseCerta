import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ListaDiariaApi } from '../../api/lista-diaria.api';
import { TomasApi } from '../../api/tomas.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ItemListaDiariaDto } from '../../models/lista-diaria.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { formatarData } from '../fichas/apresentacao-ficha';
import {
  APRESENTACAO_ESTADOS,
  formatarInstanteToma,
  mensagemConfirmacao,
} from '../medicacao/apresentacao-toma';

/**
 * Mensagens próprias das recusas de OP-33, iguais às do calendário de tomas (contrato de interface da F006, secção 6).
 * `TOMA_JA_CONFIRMADA` usa a mensagem do servidor, que indica quem confirmou e quando (RF-105).
 */
const MENSAGENS_TOMAS: Readonly<Record<string, string>> = {
  TOMA_FORA_DA_JANELA: 'A toma só pode ser confirmada a partir das 00:00 do dia previsto.',
  TOMA_NAO_CONFIRMAVEL: 'A toma ficou não confirmada e já não pode ser confirmada.',
};

/** Códigos HTTP de OP-33 depois dos quais a lista é carregada de novo, para mostrar o estado atual. */
const ESTADOS_RECUSA: readonly number[] = [403, 404, 409];

const FORMATO_DIA_LISBOA = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

const FORMATO_DIA_SEMANA = new Intl.DateTimeFormat('pt-PT', { weekday: 'long', timeZone: 'UTC' });

/** Data de hoje, `AAAA-MM-DD`, em hora de Lisboa, qualquer que seja o fuso do navegador (RNF-047). */
export function hojeLisboa(): string {
  return FORMATO_DIA_LISBOA.format(new Date());
}

/** Dia `AAAA-MM-DD` deslocado `dias` dias. */
export function deslocarDia(dia: string, dias: number): string {
  const [ano, mes, d] = dia.split('-').map(Number);
  return new Date(Date.UTC(ano, mes - 1, d + dias)).toISOString().slice(0, 10);
}

/** Nome do dia da semana de um dia `AAAA-MM-DD`, com maiúscula inicial (por exemplo «Quinta-feira»). */
export function diaDaSemana(dia: string): string {
  const [ano, mes, d] = dia.split('-').map(Number);
  const nome = FORMATO_DIA_SEMANA.format(new Date(Date.UTC(ano, mes - 1, d)));
  return nome.charAt(0).toLocaleUpperCase('pt-PT') + nome.slice(1);
}

/**
 * E26 Lista de tarefas diária (OP-50, OP-33; UC-30, UC-22): numa só lista, as tomas e os compromissos do dia de todas
 * as fichas ativas em que o Utilizador é Cuidador, por hora, cada um com o nome do utente (RF-100, RF-101). Hoje inclui
 * também as tomas de ontem que continuam por confirmar. Uma toma confirmável confirma-se aqui com um só clique, sem
 * abrir a ficha (RF-102, RNF-021); uma toma confirmada por outro Cuidador aparece com o autor e o momento ao abrir ou
 * atualizar a lista (RF-104). Os compromissos são só de consulta (RF-087). As fichas arquivadas e aquelas em que o
 * Utilizador é apenas Familiar não aparecem.
 */
@Component({
  selector: 'app-lista-diaria-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <div class="titulo-pagina">
      <h1>Lista diária</h1>
      <button type="button" class="botao botao--secundario" (click)="carregar()" [disabled]="aCarregar()">
        Atualizar
      </button>
    </div>

    <section class="cartao cartao--largo">
      <div class="lista-diaria__cabecalho">
        <h2>{{ tituloDia() }}</h2>
        <div class="acoes">
          <button type="button" class="botao botao--secundario" (click)="mudarDia(-1)">Dia anterior</button>
          @if (!eHoje()) {
            <button type="button" class="botao botao--secundario" (click)="irParaHoje()">Hoje</button>
          }
          <button type="button" class="botao botao--secundario" (click)="mudarDia(1)">Dia seguinte</button>
        </div>
      </div>

      @if (sucesso(); as mensagem) {
        <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
      }
      <app-banner-erro [mensagem]="erro()" />

      @if (aCarregar()) {
        <p role="status">A carregar a lista do dia…</p>
      } @else if (falhou()) {
        <p>
          <button type="button" class="botao botao--secundario" (click)="carregar()">Tentar novamente</button>
        </p>
      } @else if (itens().length === 0) {
        <p>Não há tarefas para este dia.</p>
        <p class="ajuda">
          A lista reúne as tomas e os compromissos das fichas ativas em que é Cuidador. Pode consultar as
          fichas em
          <a routerLink="/fichas">Fichas</a>.
        </p>
      } @else {
        <ul class="lista-diaria">
          @for (item of itens(); track item.tipo + item.id) {
            <li class="lista-diaria__item">
              <span class="lista-diaria__hora">
                @if (doutroDia(item)) {
                  {{ dataCurta(item.previstaEm) }}
                }
                {{ instante(item.previstaEm).hora }}
              </span>
              <span class="lista-diaria__titulo">{{ item.titulo }}</span>
              <a [routerLink]="['/fichas', item.fichaId]">{{ item.nomeFicha }}</a>
              @if (item.tipo === 'Toma' && item.estado) {
                <span class="lista-diaria__estado">
                  <svg class="lista-diaria__icone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path [attr.d]="estados[item.estado].desenho" />
                  </svg>
                  {{ estados[item.estado].texto }}
                </span>
                @if (doutroDia(item)) {
                  <span class="etiqueta">De ontem, por confirmar</span>
                }
              } @else {
                <span class="etiqueta">Compromisso, sem confirmação</span>
              }
              @if (textoConfirmacao(item); as texto) {
                <span class="lista-diaria__confirmacao">{{ texto }}</span>
              }
              @if (item.tipo === 'Toma' && item.confirmavel) {
                <button
                  type="button"
                  class="botao"
                  [disabled]="aConfirmar().has(item.id)"
                  [attr.aria-label]="'Confirmar ' + item.titulo + ' de ' + item.nomeFicha"
                  (click)="confirmar(item)"
                >
                  Confirmar toma
                </button>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    /* I075 — lista de tarefas diária */
    .lista-diaria__cabecalho {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      margin-bottom: 1rem;

      h2 {
        margin: 0;
      }
    }

    .lista-diaria {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .lista-diaria__item {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem 1rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid var(--cor-borda);
    }

    .lista-diaria__hora,
    .lista-diaria__titulo,
    .lista-diaria__estado {
      font-weight: 600;
    }

    .lista-diaria__estado {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
    }

    .lista-diaria__icone {
      width: 1.25rem;
      height: 1.25rem;
      fill: currentColor;
    }

    .lista-diaria__confirmacao {
      flex-basis: 100%;
    }
  `,
})
export class ListaDiariaPage implements OnInit {
  private readonly listaDiariaApi = inject(ListaDiariaApi);
  private readonly tomasApi = inject(TomasApi);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly estados = APRESENTACAO_ESTADOS;
  protected readonly instante = formatarInstanteToma;

  protected readonly dia = signal(hojeLisboa());
  protected readonly itens = signal<ItemListaDiariaDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly falhou = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);
  /** Tomas com OP-33 em curso, cujo botão fica desativado. */
  protected readonly aConfirmar = signal<ReadonlySet<string>>(new Set());

  protected readonly eHoje = computed(() => this.dia() === hojeLisboa());
  protected readonly tituloDia = computed(() => {
    const dia = this.dia();
    const prefixo = this.eHoje() ? 'Hoje, ' : '';
    return `${prefixo}${diaDaSemana(dia)}, ${formatarData(dia)}`;
  });

  /** Identifica o pedido de OP-50 mais recente, para ignorar respostas de dias anteriores. */
  private pedidoAtual = 0;

  ngOnInit(): void {
    this.carregar();
  }

  protected mudarDia(dias: number): void {
    this.dia.update((dia) => deslocarDia(dia, dias));
    this.limparMensagens();
    this.carregar();
  }

  protected irParaHoje(): void {
    this.dia.set(hojeLisboa());
    this.limparMensagens();
    this.carregar();
  }

  /** Item de outro dia: na lista de hoje, as tomas de ontem ainda por confirmar (RF-100). */
  protected doutroDia(item: ItemListaDiariaDto): boolean {
    return item.previstaEm.slice(0, 10) !== this.dia();
  }

  protected dataCurta(instante: string): string {
    return formatarInstanteToma(instante).data;
  }

  /** Autor e momento de uma toma confirmada (RF-104), ou `null`. */
  protected textoConfirmacao(item: ItemListaDiariaDto): string | null {
    if (!item.confirmadaPor || !item.confirmadaEm) {
      return null;
    }

    const { data, hora } = formatarInstanteToma(item.confirmadaEm);
    return `Confirmada por ${item.confirmadaPor} em ${data} às ${hora}`;
  }

  /** OP-33 com um só clique, sem abrir a ficha nem pedir confirmação (RF-102, RNF-021). */
  protected confirmar(item: ItemListaDiariaDto): void {
    if (this.aConfirmar().has(item.id)) {
      return;
    }

    this.limparMensagens();
    this.marcarAConfirmar(item.id, true);
    this.tomasApi.confirmar(item.id).subscribe({
      next: (toma) => {
        this.marcarAConfirmar(item.id, false);
        this.itens.update((itens) =>
          itens.map((i) =>
            i.tipo === 'Toma' && i.id === toma.id
              ? {
                  ...i,
                  estado: toma.estado,
                  confirmadaPor: toma.confirmadaPor?.nome ?? null,
                  confirmadaEm: toma.confirmadaEm,
                  confirmavel: false,
                }
              : i,
          ),
        );
        this.sucesso.set(mensagemConfirmacao(toma.estado));
      },
      error: (problema: ProblemaApi) => {
        this.marcarAConfirmar(item.id, false);
        this.erro.set(this.mensagens.mensagem(problema, MENSAGENS_TOMAS));
        if (ESTADOS_RECUSA.includes(problema.estado)) {
          this.carregar(true);
        }
      },
    });
  }

  /** OP-50 do dia escolhido; também serve para atualizar (RF-104). Com `manterErro`, a recusa continua visível. */
  protected carregar(manterErro = false): void {
    const pedido = ++this.pedidoAtual;
    const dia = this.dia();
    this.aCarregar.set(true);
    this.falhou.set(false);
    if (!manterErro) {
      this.erro.set(null);
    }

    this.listaDiariaApi.consultar(dia).subscribe({
      next: (itens) => {
        if (pedido !== this.pedidoAtual) {
          return;
        }

        this.itens.set(itens);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        if (pedido !== this.pedidoAtual) {
          return;
        }

        this.itens.set([]);
        this.aCarregar.set(false);
        this.falhou.set(true);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  private limparMensagens(): void {
    this.erro.set(null);
    this.sucesso.set(null);
  }

  private marcarAConfirmar(id: string, emCurso: boolean): void {
    this.aConfirmar.update((ids) => {
      const novos = new Set(ids);
      if (emCurso) {
        novos.add(id);
      } else {
        novos.delete(id);
      }
      return novos;
    });
  }
}
