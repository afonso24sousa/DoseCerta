import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConvitesApi } from '../../api/convites.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ConviteDto } from '../../models/pessoas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { formatarInstante } from '../fichas/apresentacao-ficha';
import { papelOferecido } from './apresentacao-pessoas';

/** Resposta a um convite, que pede confirmação. */
type Resposta = 'aceitar' | 'recusar';

/** Códigos que indicam que a lista mudou entretanto: a página volta a carregá-la. */
const CODIGOS_RECARREGAR = new Set([
  'CONVITE_JA_RESPONDIDO',
  'CONVITE_NAO_ENCONTRADO',
  'PESSOA_JA_ASSOCIADA',
]);

/**
 * E14 Convites recebidos (OP-20 a OP-22; UC-14): os convites pendentes dirigidos ao e-mail do Utilizador, com a ficha,
 * o papel oferecido e quem convidou (RF-042, RF-044, RF-135), e as respostas aceitar (RF-045, RF-026) e recusar
 * (RF-046, RF-027), cada uma com confirmação. A resposta continua possível numa ficha arquivada.
 */
@Component({
  selector: 'app-convites-recebidos-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <div class="titulo-pagina">
      <h1>Convites</h1>
      <a routerLink="/fichas">Ir para as fichas</a>
    </div>

    @if (sucesso(); as resultado) {
      <p class="banner banner--sucesso" role="status">
        {{ resultado.mensagem }}
        @if (resultado.fichaId) {
          <a [routerLink]="['/fichas', resultado.fichaId]">Abrir a ficha</a>
        }
      </p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <section class="cartao cartao--largo">
      @if (aCarregar()) {
        <p role="status">A carregar os convites…</p>
      } @else if (convites().length === 0 && !erro()) {
        <p>Não tem convites pendentes.</p>
      } @else if (convites().length > 0) {
        <table class="tabela">
          <caption class="so-leitor">
            Convites recebidos
          </caption>
          <thead>
            <tr>
              <th scope="col">Ficha</th>
              <th scope="col">Papel oferecido</th>
              <th scope="col">Convidado por</th>
              <th scope="col">Recebido em</th>
              <th scope="col">Resposta</th>
            </tr>
          </thead>
          <tbody>
            @for (convite of convites(); track convite.id) {
              <tr>
                <td>{{ convite.nomeFicha }}</td>
                <td>{{ papel(convite) }}</td>
                <td>{{ convite.convidadoPor }}</td>
                <td>{{ instante(convite.criadoEm) }}</td>
                <td>
                  @if (porConfirmar()?.convite?.id === convite.id) {
                    <div class="confirmacao" role="group" [attr.aria-labelledby]="'convite-' + convite.id">
                      <p [id]="'convite-' + convite.id">{{ pergunta() }}</p>
                      <div class="acoes">
                        <button type="button" class="botao" [disabled]="aResponder()" (click)="responder()">
                          {{ aResponder() ? 'A processar…' : botaoConfirmar() }}
                        </button>
                        <button
                          type="button"
                          class="botao botao--secundario"
                          [disabled]="aResponder()"
                          (click)="porConfirmar.set(null)"
                        >
                          Cancelar
                        </button>
                      </div>
                    </div>
                  } @else {
                    <div class="acoes">
                      <button type="button" class="botao" (click)="pedirConfirmacao(convite, 'aceitar')">
                        Aceitar
                      </button>
                      <button
                        type="button"
                        class="botao botao--secundario"
                        (click)="pedirConfirmacao(convite, 'recusar')"
                      >
                        Recusar
                      </button>
                    </div>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
        @if (temParaPrincipal()) {
          <p class="ajuda">
            Aceitar o convite para Cuidador principal transfere esse papel: o Cuidador principal atual deixa
            de estar associado à ficha. Recusar mantém o Cuidador principal atual.
          </p>
        }
      }
    </section>
  `,
})
export class ConvitesRecebidosPage implements OnInit {
  private readonly convitesApi = inject(ConvitesApi);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly instante = formatarInstante;
  protected readonly papel = papelOferecido;

  protected readonly convites = signal<ConviteDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<{ mensagem: string; fichaId?: string } | null>(null);
  protected readonly porConfirmar = signal<{ convite: ConviteDto; resposta: Resposta } | null>(null);
  protected readonly aResponder = signal(false);

  protected readonly temParaPrincipal = computed(() => this.convites().some((c) => c.paraPrincipal));

  protected readonly pergunta = computed(() => {
    const pedido = this.porConfirmar();
    if (!pedido) {
      return '';
    }

    const { convite, resposta } = pedido;
    if (resposta === 'recusar') {
      return convite.paraPrincipal
        ? `Recusar o convite para Cuidador principal da ficha «${convite.nomeFicha}»? O Cuidador principal atual mantém-se.`
        : `Recusar o convite para a ficha «${convite.nomeFicha}»? Não fica associado à ficha.`;
    }

    return convite.paraPrincipal
      ? `Aceitar ser Cuidador principal da ficha «${convite.nomeFicha}»? O Cuidador principal atual deixa de estar associado à ficha.`
      : `Aceitar o convite para a ficha «${convite.nomeFicha}» como ${convite.papelOferecido}?`;
  });

  protected readonly botaoConfirmar = computed(() =>
    this.porConfirmar()?.resposta === 'recusar' ? 'Sim, recusar o convite' : 'Sim, aceitar o convite',
  );

  ngOnInit(): void {
    this.carregar();
  }

  protected pedirConfirmacao(convite: ConviteDto, resposta: Resposta): void {
    this.erro.set(null);
    this.sucesso.set(null);
    this.porConfirmar.set({ convite, resposta });
  }

  /** OP-21 ou OP-22, depois da confirmação; a lista é carregada de novo. */
  protected responder(): void {
    const pedido = this.porConfirmar();
    if (!pedido || this.aResponder()) {
      return;
    }

    const { convite, resposta } = pedido;
    this.aResponder.set(true);
    const terminar = (): void => {
      this.aResponder.set(false);
      this.porConfirmar.set(null);
    };

    if (resposta === 'aceitar') {
      this.convitesApi.aceitar(convite.id).subscribe({
        next: () => {
          terminar();
          this.sucesso.set({
            mensagem: `Convite aceite. Já tem acesso à ficha «${convite.nomeFicha}».`,
            fichaId: convite.fichaId,
          });
          this.carregar();
        },
        error: (problema: ProblemaApi) => {
          terminar();
          this.tratarErro(problema);
        },
      });
    } else {
      this.convitesApi.recusar(convite.id).subscribe({
        next: () => {
          terminar();
          this.sucesso.set({ mensagem: 'Convite recusado.' });
          this.carregar();
        },
        error: (problema: ProblemaApi) => {
          terminar();
          this.tratarErro(problema);
        },
      });
    }
  }

  /** OP-20. */
  private carregar(): void {
    this.aCarregar.set(this.convites().length === 0);
    this.convitesApi.listarRecebidos().subscribe({
      next: (convites) => {
        this.convites.set([...convites].sort((a, b) => b.criadoEm.localeCompare(a.criadoEm)));
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.convites.set([]);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  private tratarErro(problema: ProblemaApi): void {
    this.erro.set(this.mensagens.mensagem(problema));
    if (CODIGOS_RECARREGAR.has(problema.codigo)) {
      this.carregar();
    }
  }
}
