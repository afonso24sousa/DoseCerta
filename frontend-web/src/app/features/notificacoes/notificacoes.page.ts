import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificacoesApi } from '../../api/notificacoes.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { NotificacaoDto } from '../../models/notificacoes.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import {
  APRESENTACAO_TIPOS,
  TEXTOS_EMAIL,
  detalheToma,
  momentoNotificacao,
  ordenarNotificacoes,
} from './apresentacao-notificacao';

/**
 * E25 Notificações (OP-51; UC-34): o histórico das notificações recebidas pelo Utilizador, da mais recente para a mais
 * antiga (RF-140), com o tipo (texto e ícone, RNF-027), a ficha, a mensagem, a toma quando existe (RF-110 a RF-113) e o
 * estado do e-mail, incluindo «E-mail não entregue» (RF-119). Não há notificações push: o histórico é lido ao abrir o
 * ecrã e ao carregar em «Atualizar».
 */
@Component({
  selector: 'app-notificacoes-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  styles: `
    .notificacoes {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .notificacao {
      display: flex;
      gap: 0.75rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid var(--cor-borda);
    }

    .notificacao:last-child {
      border-bottom: none;
    }

    .notificacao__icone {
      flex: none;
      width: 1.5rem;
      height: 1.5rem;
      fill: currentColor;
    }

    .notificacao__corpo {
      display: grid;
      gap: 0.25rem;
      min-width: 0;
    }

    .notificacao__corpo p {
      margin: 0;
    }

    .notificacao__cabecalho {
      display: flex;
      flex-wrap: wrap;
      gap: 0 0.75rem;
      font-weight: 600;
    }

    .notificacao__email--nao-entregue {
      font-weight: 600;
      color: var(--cor-erro);
    }
  `,
  template: `
    <div class="titulo-pagina">
      <h1>Notificações</h1>
      <button type="button" class="botao botao--secundario" [disabled]="aCarregar()" (click)="carregar()">
        {{ aCarregar() ? 'A atualizar…' : 'Atualizar' }}
      </button>
    </div>

    <app-banner-erro [mensagem]="erro()" />

    <section class="cartao cartao--largo" aria-labelledby="historico-notificacoes">
      <h2 id="historico-notificacoes" class="so-leitor">Histórico de notificações</h2>
      @if (aCarregar() && notificacoes().length === 0) {
        <p role="status">A carregar as notificações…</p>
      } @else if (notificacoes().length === 0 && !erro()) {
        <p>Não tem notificações.</p>
      } @else if (notificacoes().length > 0) {
        <ul class="notificacoes">
          @for (n of notificacoes(); track n.id) {
            <li class="notificacao">
              <svg class="notificacao__icone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <path [attr.d]="tipos[n.tipo].desenho" />
              </svg>
              <div class="notificacao__corpo">
                <p class="notificacao__cabecalho">
                  <span class="notificacao__tipo">{{ tipos[n.tipo].texto }}</span>
                  <span class="notificacao__momento">{{ momento(n) }}</span>
                </p>
                <p class="notificacao__ficha">
                  Ficha:
                  @if (n.tipo === 'ConviteRecebido') {
                    {{ n.nomeFicha }}
                  } @else {
                    <a [routerLink]="['/fichas', n.fichaId]">{{ n.nomeFicha }}</a>
                  }
                </p>
                <p class="notificacao__mensagem">{{ n.mensagem }}</p>
                @if (toma(n); as detalhe) {
                  <p class="notificacao__toma">Toma: {{ detalhe }}</p>
                }
                @if (n.tipo === 'ConviteRecebido') {
                  <p><a routerLink="/convites">Ver os convites</a></p>
                }
                <p
                  class="notificacao__email"
                  [class.notificacao__email--nao-entregue]="n.email === 'NaoEntregue'"
                >
                  {{ textosEmail[n.email] }}
                </p>
              </div>
            </li>
          }
        </ul>
      }
      <p class="ajuda">Atualizado ao abrir o ecrã ou ao carregar em «Atualizar».</p>
    </section>
  `,
})
export class NotificacoesPage implements OnInit {
  private readonly notificacoesApi = inject(NotificacoesApi);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly tipos = APRESENTACAO_TIPOS;
  protected readonly textosEmail = TEXTOS_EMAIL;
  protected readonly momento = momentoNotificacao;
  protected readonly toma = detalheToma;

  protected readonly notificacoes = signal<NotificacaoDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);

  ngOnInit(): void {
    this.carregar();
  }

  /** OP-51, ao abrir o ecrã e ao carregar em «Atualizar» (RF-140). */
  protected carregar(): void {
    this.aCarregar.set(true);
    this.erro.set(null);
    this.notificacoesApi.listar().subscribe({
      next: (notificacoes) => {
        this.notificacoes.set(ordenarNotificacoes(notificacoes));
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
