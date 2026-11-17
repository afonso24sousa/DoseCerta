import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { TomasApi } from '../../api/tomas.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import { TomaDto } from '../../models/tomas.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { formatarData } from '../fichas/apresentacao-ficha';
import {
  APRESENTACAO_ESTADOS,
  PeriodoTomas,
  agruparPorDia,
  deslocarPeriodo,
  formatarInstanteToma,
  mensagemConfirmacao,
  periodoPorOmissao,
  textoConfirmacao,
  textoPeriodoTomas,
} from './apresentacao-toma';

/**
 * Mensagens próprias deste ecrã para as recusas de OP-33 (contrato de interface da F006, secção 6).
 * `TOMA_JA_CONFIRMADA` usa a mensagem do servidor, que indica quem confirmou e quando (RF-105); `ACESSO_PROIBIDO`
 * usa a mensagem geral.
 */
const MENSAGENS_TOMAS: Readonly<Record<string, string>> = {
  TOMA_FORA_DA_JANELA: 'A toma só pode ser confirmada a partir das 00:00 do dia previsto.',
  TOMA_NAO_CONFIRMAVEL: 'A toma ficou não confirmada e já não pode ser confirmada.',
};

/** Códigos HTTP de OP-33 depois dos quais a lista é carregada de novo, para mostrar o estado atual da toma. */
const ESTADOS_RECUSA: readonly number[] = [403, 409];

/**
 * Calendário de tomas da ficha, segundo separador de E15 (OP-32, OP-33; UC-20, UC-22): as tomas de 7 dias agrupadas
 * por dia, com a hora, o medicamento, a dose, o grau, o estado (ícone e texto, RNF-027) e, nas confirmadas, quem
 * confirmou e quando (RF-063, RF-065). O Cuidador confirma uma toma `Pendente` com um só clique (RF-064, RNF-021); a
 * janela é decidida pelo servidor, por isso o botão também aparece nas tomas de dias futuros. O Familiar só consulta
 * (RF-048) e numa ficha arquivada ainda se confirmam as tomas que ficaram por confirmar (RF-121).
 */
@Component({
  selector: 'app-lista-tomas',
  standalone: true,
  imports: [BannerErroComponent],
  template: `
    <section class="cartao cartao--largo">
      <div class="tomas__cabecalho">
        <h2>{{ cabecalho() }}</h2>
        <div class="acoes">
          <button type="button" class="botao botao--secundario" (click)="deslocar(-1)">
            Período anterior
          </button>
          <button type="button" class="botao botao--secundario" (click)="deslocar(1)">
            Período seguinte
          </button>
        </div>
      </div>

      @if (eFamiliar()) {
        <p class="banner banner--aviso" role="status">
          Como Familiar, pode consultar as tomas, mas não as confirmar.
        </p>
      }
      @if (arquivada()) {
        <p class="banner banner--aviso" role="status">
          Ficha arquivada: não são geradas tomas novas. Ainda pode confirmar as tomas que ficaram por
          confirmar.
        </p>
      }

      @if (sucesso(); as mensagem) {
        <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
      }
      <app-banner-erro [mensagem]="erro()" />

      @if (aCarregar()) {
        <p role="status">A carregar as tomas…</p>
      } @else if (dias().length === 0 && !erro()) {
        <p>Não há tomas previstas neste período.</p>
      } @else {
        @for (d of dias(); track d.dia) {
          <h3>{{ data(d.dia) }}</h3>
          <ul class="tomas">
            @for (t of d.tomas; track t.id) {
              <li class="toma">
                <span class="toma__hora">{{ instante(t.previstaEm).hora }}</span>
                <span class="toma__medicamento">{{ t.medicamento }}</span>
                <span>{{ t.dose }}</span>
                <span>{{ t.grauImportancia }}</span>
                <span class="toma__estado">
                  <svg class="toma__icone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path [attr.d]="estados[t.estado].desenho" />
                  </svg>
                  {{ estados[t.estado].texto }}
                </span>
                @if (confirmacao(t); as texto) {
                  <span class="toma__confirmacao">{{ texto }}</span>
                }
                @if (eCuidador() && t.estado === 'Pendente') {
                  <button
                    type="button"
                    class="botao"
                    [disabled]="aConfirmar().has(t.id)"
                    (click)="confirmar(t)"
                  >
                    Confirmar toma
                  </button>
                }
              </li>
            }
          </ul>
        }
      }
    </section>
  `,
  styles: `
    /* I068 — calendário de tomas */
    .tomas__cabecalho {
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

    h3 {
      margin: 1.25rem 0 0.5rem;
    }

    .tomas {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .toma {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem 1rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid var(--cor-borda);
    }

    .toma__hora,
    .toma__medicamento,
    .toma__estado {
      font-weight: 600;
    }

    .toma__estado {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
    }

    .toma__icone {
      width: 1.25rem;
      height: 1.25rem;
      fill: currentColor;
    }

    .toma__confirmacao {
      flex-basis: 100%;
    }
  `,
})
export class ListaTomasComponent implements OnChanges {
  private readonly tomasApi = inject(TomasApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);

  /** Ficha cujas tomas se mostram. */
  readonly fichaId = input.required<string>();

  protected readonly estados = APRESENTACAO_ESTADOS;
  protected readonly data = formatarData;
  protected readonly instante = formatarInstanteToma;
  protected readonly confirmacao = textoConfirmacao;

  /** O botão «Confirmar toma» só aparece ao Cuidador (RF-047, RF-048); o servidor verifica o papel em OP-33. */
  protected readonly eCuidador = this.contexto.eCuidador;
  protected readonly eFamiliar = computed(() => this.contexto.ficha()?.meuPapel === 'Familiar');
  protected readonly arquivada = this.contexto.arquivada;

  protected readonly periodo = signal<PeriodoTomas>(periodoPorOmissao());
  protected readonly tomas = signal<TomaDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);
  /** Tomas com OP-33 em curso, cujo botão fica desativado. */
  protected readonly aConfirmar = signal<ReadonlySet<string>>(new Set());

  protected readonly cabecalho = computed(() => textoPeriodoTomas(this.periodo()));
  protected readonly dias = computed(() => agruparPorDia(this.tomas()));

  /** Identifica o pedido de OP-32 mais recente, para ignorar respostas de períodos ou fichas anteriores. */
  private pedidoAtual = 0;

  ngOnChanges(): void {
    this.periodo.set(periodoPorOmissao());
    this.limparMensagens();
    this.carregar();
  }

  /** Período anterior (`-1`) ou seguinte (`1`), 7 dias de cada vez. */
  protected deslocar(sentido: -1 | 1): void {
    this.periodo.update((periodo) => deslocarPeriodo(periodo, sentido));
    this.limparMensagens();
    this.carregar();
  }

  /** OP-33 com um só clique, sem diálogo (RNF-021). */
  protected confirmar(toma: TomaDto): void {
    if (this.aConfirmar().has(toma.id)) {
      return;
    }

    this.limparMensagens();
    this.marcarAConfirmar(toma.id, true);
    this.tomasApi.confirmar(toma.id).subscribe({
      next: (confirmada) => {
        this.marcarAConfirmar(toma.id, false);
        this.tomas.update((tomas) => tomas.map((t) => (t.id === confirmada.id ? confirmada : t)));
        this.sucesso.set(mensagemConfirmacao(confirmada.estado));
      },
      error: (problema: ProblemaApi) => {
        this.marcarAConfirmar(toma.id, false);
        this.erro.set(this.mensagens.mensagem(problema, MENSAGENS_TOMAS));
        if (ESTADOS_RECUSA.includes(problema.estado)) {
          this.carregar(true);
        }
      },
    });
  }

  /** OP-32 do período atual; com `manterErro`, a mensagem de uma recusa continua visível depois de carregar. */
  private carregar(manterErro = false): void {
    const pedido = ++this.pedidoAtual;
    const { de, ate } = this.periodo();
    this.aCarregar.set(true);
    if (!manterErro) {
      this.erro.set(null);
    }

    this.tomasApi.listar(this.fichaId(), de, ate).subscribe({
      next: (tomas) => {
        if (pedido !== this.pedidoAtual) {
          return;
        }

        this.tomas.set(tomas);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        if (pedido !== this.pedidoAtual) {
          return;
        }

        this.tomas.set([]);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  private marcarAConfirmar(tomaId: string, emCurso: boolean): void {
    this.aConfirmar.update((atuais) => {
      const novos = new Set(atuais);
      if (emCurso) {
        novos.add(tomaId);
      } else {
        novos.delete(tomaId);
      }
      return novos;
    });
  }

  private limparMensagens(): void {
    this.erro.set(null);
    this.sucesso.set(null);
  }
}
