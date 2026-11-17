import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { PessoasApi } from '../../api/pessoas.api';
import { AuthService } from '../../core/auth.service';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto } from '../../models/fichas.models';
import { PessoaFichaDto } from '../../models/pessoas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { papelPessoa } from './apresentacao-pessoas';
import { ResultadoPessoas } from './resultados-pessoas';

/** Ação sobre uma pessoa da ficha, que pede confirmação. */
type AcaoPessoa = 'promover' | 'despromover' | 'remover' | 'tornarPrincipal';

/** Ações disponíveis numa linha da lista. */
interface AcoesPessoa {
  promover: boolean;
  despromover: boolean;
  remover: boolean;
  tornarPrincipal: boolean;
  /** Cuidador principal de uma ficha de terceiro: despromover e remover ficam inativos até outro assumir o papel. */
  principalProtegido: boolean;
}

const SEM_ACOES: AcoesPessoa = {
  promover: false,
  despromover: false,
  remover: false,
  tornarPrincipal: false,
  principalProtegido: false,
};

/** Pergunta de confirmação, texto do botão e mensagem de sucesso de cada ação. */
const ACOES: Readonly<
  Record<AcaoPessoa, { pergunta: (nome: string) => string; botao: string; sucesso: (nome: string) => string }>
> = {
  promover: {
    pergunta: (nome) => `Promover ${nome} a Cuidador? Passa a poder alterar a ficha e os seus registos.`,
    botao: 'Sim, promover a Cuidador',
    sucesso: (nome) => `${nome} passou a Cuidador.`,
  },
  despromover: {
    pergunta: (nome) => `Despromover ${nome} a Familiar? Passa a poder só consultar a ficha.`,
    botao: 'Sim, despromover a Familiar',
    sucesso: (nome) => `${nome} passou a Familiar.`,
  },
  remover: {
    pergunta: (nome) => `Remover o acesso de ${nome} à ficha? Deixa de ver a ficha e os seus dados.`,
    botao: 'Sim, remover o acesso',
    sucesso: (nome) => `${nome} deixou de ter acesso à ficha.`,
  },
  tornarPrincipal: {
    pergunta: (nome) =>
      `Tornar ${nome} Cuidador principal? O Cuidador principal atual continua como Cuidador.`,
    botao: 'Sim, tornar Cuidador principal',
    sucesso: (nome) => `${nome} é agora o Cuidador principal.`,
  },
};

/** Mensagens de sucesso vindas da rota. */
const RESULTADOS: Readonly<Record<string, string>> = {
  [ResultadoPessoas.conviteEnviado]:
    'Convite enviado. A pessoa vê-o na área de convites e recebe um e-mail; se o envio falhar, é reenviado até 3 vezes.',
};

/** Códigos que indicam que a ficha ou as pessoas mudaram entretanto: a página volta a carregar tudo. */
const CODIGOS_RECARREGAR = new Set([
  'FICHA_ARQUIVADA',
  'ACESSO_PROIBIDO',
  'PESSOA_NAO_ENCONTRADA',
  'CUIDADOR_PRINCIPAL_PROTEGIDO',
  'DESTINATARIO_NAO_E_CUIDADOR',
]);

/**
 * E12 Pessoas da ficha (OP-18, OP-23 a OP-26; UC-15, UC-16): os Cuidadores e os Familiares associados, com o papel e a
 * indicação de Cuidador principal (provisório, RF-025). Só um Cuidador, numa ficha ativa, vê as ações: promover
 * (RF-049), despromover (RF-130), remover (RF-131) e tornar Cuidador principal (RF-132, qualquer Cuidador, matriz de
 * permissões v02, RP-08). O Cuidador principal só sai depois de outro assumir o papel (RF-133) e a ficha tem sempre um
 * Cuidador (RF-134). Na ficha própria o titular é sempre o Cuidador principal e não tem ações (RF-033, RF-034). O
 * Familiar só vê a lista (RF-048). A interface só esconde ações: o servidor decide cada pedido (RNF-004).
 */
@Component({
  selector: 'app-pessoas-ficha-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId()]">Voltar à ficha</a></p>

    @if (sucesso(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    @if (ficha(); as f) {
      <div class="titulo-pagina">
        <h1>Pessoas da ficha</h1>
        @if (podeGerir()) {
          <a class="botao" [routerLink]="['/fichas', f.id, 'pessoas', 'convidar']">Convidar pessoa</a>
        }
      </div>
      <p>{{ f.nomeCompleto }}</p>

      @if (f.estado === 'Arquivada') {
        <p class="banner banner--aviso" role="status">
          Ficha arquivada: as pessoas associadas só podem ser consultadas.
        </p>
      } @else if (f.meuPapel === 'Familiar') {
        <p class="banner banner--aviso">Como Familiar, pode consultar as pessoas associadas à ficha.</p>
      }

      <section class="cartao cartao--largo">
        <h2>Pessoas associadas</h2>
        @if (pessoas() === null) {
          @if (!erro()) {
            <p role="status">A carregar as pessoas…</p>
          }
        } @else {
          <table class="tabela">
            <caption class="so-leitor">
              Pessoas associadas à ficha
            </caption>
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">E-mail</th>
                <th scope="col">Papel</th>
                @if (podeGerir()) {
                  <th scope="col">Ações</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (pessoa of pessoas(); track pessoa.utilizadorId) {
                <tr>
                  <td>
                    {{ pessoa.nome }}
                    @if (pessoa.utilizadorId === meuId()) {
                      (eu)
                    }
                  </td>
                  <td>{{ pessoa.email }}</td>
                  <td>{{ papel(pessoa) }}</td>
                  @if (podeGerir()) {
                    <td>
                      @if (porConfirmar()?.pessoa?.utilizadorId === pessoa.utilizadorId) {
                        <div
                          class="confirmacao"
                          role="group"
                          [attr.aria-labelledby]="'pessoa-' + pessoa.utilizadorId"
                        >
                          <p [id]="'pessoa-' + pessoa.utilizadorId">{{ pergunta() }}</p>
                          <div class="acoes">
                            <button type="button" class="botao" [disabled]="aExecutar()" (click)="executar()">
                              {{ aExecutar() ? 'A processar…' : botaoConfirmar() }}
                            </button>
                            <button
                              type="button"
                              class="botao botao--secundario"
                              [disabled]="aExecutar()"
                              (click)="porConfirmar.set(null)"
                            >
                              Cancelar
                            </button>
                          </div>
                        </div>
                      } @else {
                        @let acoes = acoesDe(pessoa);
                        <div class="acoes">
                          @if (acoes.promover) {
                            <button
                              type="button"
                              class="botao botao--secundario"
                              (click)="pedirConfirmacao(pessoa, 'promover')"
                            >
                              Promover a Cuidador
                            </button>
                          }
                          @if (acoes.despromover || acoes.principalProtegido) {
                            <button
                              type="button"
                              class="botao botao--secundario"
                              [disabled]="acoes.principalProtegido"
                              [attr.aria-describedby]="
                                acoes.principalProtegido ? 'pessoas-principal-protegido' : null
                              "
                              (click)="pedirConfirmacao(pessoa, 'despromover')"
                            >
                              Despromover a Familiar
                            </button>
                          }
                          @if (acoes.remover || acoes.principalProtegido) {
                            <button
                              type="button"
                              class="botao botao--perigo"
                              [disabled]="acoes.principalProtegido"
                              [attr.aria-describedby]="
                                acoes.principalProtegido ? 'pessoas-principal-protegido' : null
                              "
                              (click)="pedirConfirmacao(pessoa, 'remover')"
                            >
                              Remover
                            </button>
                          }
                          @if (acoes.tornarPrincipal) {
                            <button
                              type="button"
                              class="botao botao--secundario"
                              (click)="pedirConfirmacao(pessoa, 'tornarPrincipal')"
                            >
                              Tornar principal
                            </button>
                          }
                        </div>
                      }
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>

          @if (temProvisorio()) {
            <p class="ajuda">
              Com um convite para Cuidador principal pendente, o criador aparece como Cuidador principal
              (provisório).
            </p>
          }
          @if (podeGerir()) {
            @if (f.eProprio) {
              <p class="ajuda">
                Na ficha própria o titular é sempre o Cuidador principal: não pode ser despromovido, removido
                nem substituído.
              </p>
            } @else {
              <p class="ajuda" id="pessoas-principal-protegido">
                Indique outro Cuidador para assumir o papel antes de despromover ou remover o Cuidador
                principal.
              </p>
            }
            <p class="ajuda">A ficha tem de ficar com pelo menos um Cuidador.</p>
          }
        }
      </section>
    } @else if (aCarregar()) {
      <p role="status">A carregar a ficha…</p>
    }
  `,
})
export class PessoasFichaPage implements OnChanges {
  private readonly pessoasApi = inject(PessoasApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly auth = inject(AuthService);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `resultado` da rota, depois de convidar uma pessoa. */
  readonly resultado = input<string | undefined>();

  protected readonly papel = papelPessoa;

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly pessoas = signal<PessoaFichaDto[] | null>(null);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);
  protected readonly porConfirmar = signal<{ pessoa: PessoaFichaDto; acao: AcaoPessoa } | null>(null);
  protected readonly aExecutar = signal(false);

  protected readonly meuId = computed(() => this.auth.utilizador()?.id ?? null);

  /** As ações de gestão exigem o papel Cuidador e a ficha ativa (RF-047, RF-048, RF-120). */
  protected readonly podeGerir = computed(() => {
    const ficha = this.ficha();
    return !!ficha && ficha.meuPapel === 'Cuidador' && ficha.estado === 'Ativa';
  });

  protected readonly temProvisorio = computed(() => (this.pessoas() ?? []).some((p) => p.provisorio));

  protected readonly pergunta = computed(() => {
    const pedido = this.porConfirmar();
    return pedido ? ACOES[pedido.acao].pergunta(pedido.pessoa.nome) : '';
  });

  protected readonly botaoConfirmar = computed(() => {
    const pedido = this.porConfirmar();
    return pedido ? ACOES[pedido.acao].botao : '';
  });

  ngOnChanges(): void {
    this.erro.set(null);
    this.porConfirmar.set(null);
    this.sucesso.set(RESULTADOS[this.resultado() ?? ''] ?? null);
    const atual = this.contexto.ficha();
    this.ficha.set(atual?.id === this.fichaId() ? atual : null);
    this.carregar();
  }

  /**
   * Ações de uma linha. A própria linha só tem ações quando é o Cuidador principal de uma ficha de terceiro (inativas,
   * com a explicação do RF-133); sair da ficha por iniciativa própria não está no protótipo.
   */
  protected acoesDe(pessoa: PessoaFichaDto): AcoesPessoa {
    const ficha = this.ficha();
    if (!ficha || !this.podeGerir()) {
      return SEM_ACOES;
    }

    if (pessoa.principal) {
      // Ficha própria: o titular não tem ações (RF-033, RF-034). Ficha de terceiro: só depois de transferir (RF-133).
      return ficha.eProprio ? SEM_ACOES : { ...SEM_ACOES, principalProtegido: true };
    }

    if (pessoa.utilizadorId === this.meuId()) {
      return SEM_ACOES;
    }

    if (pessoa.papel === 'Familiar') {
      return { ...SEM_ACOES, promover: true, remover: true };
    }

    return { ...SEM_ACOES, despromover: true, remover: true, tornarPrincipal: !ficha.eProprio };
  }

  protected pedirConfirmacao(pessoa: PessoaFichaDto, acao: AcaoPessoa): void {
    this.erro.set(null);
    this.sucesso.set(null);
    this.porConfirmar.set({ pessoa, acao });
  }

  /** Executa a ação confirmada (OP-23 a OP-26) e volta a carregar a ficha e as pessoas. */
  protected executar(): void {
    const pedido = this.porConfirmar();
    if (!pedido || this.aExecutar()) {
      return;
    }

    const { pessoa, acao } = pedido;
    const id = this.fichaId();
    const operacao: Observable<unknown> =
      acao === 'promover'
        ? this.pessoasApi.promover(id, pessoa.utilizadorId)
        : acao === 'despromover'
          ? this.pessoasApi.despromover(id, pessoa.utilizadorId)
          : acao === 'remover'
            ? this.pessoasApi.remover(id, pessoa.utilizadorId)
            : this.pessoasApi.transferirPrincipal(id, { utilizadorId: pessoa.utilizadorId });

    this.aExecutar.set(true);
    operacao.subscribe({
      next: () => {
        this.aExecutar.set(false);
        this.porConfirmar.set(null);
        this.sucesso.set(ACOES[acao].sucesso(pessoa.nome));
        this.carregar();
      },
      error: (problema: ProblemaApi) => {
        this.aExecutar.set(false);
        this.porConfirmar.set(null);
        this.erro.set(this.mensagens.mensagem(problema));
        if (problema.codigo === 'FICHA_NAO_ENCONTRADA') {
          this.ficha.set(null);
          this.pessoas.set(null);
        } else if (CODIGOS_RECARREGAR.has(problema.codigo)) {
          this.carregar();
        }
      },
    });
  }

  /** OP-10 (papel e estado da ficha) e OP-18 (pessoas). */
  private carregar(): void {
    const id = this.fichaId();
    this.aCarregar.set(this.ficha() === null);
    this.contexto.carregar(id).subscribe({
      next: (ficha) => {
        this.ficha.set(ficha);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.ficha.set(null);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });

    this.pessoasApi.listar(id).subscribe({
      next: (pessoas) => this.pessoas.set(ordenar(pessoas)),
      error: (problema: ProblemaApi) => {
        this.pessoas.set(null);
        if (problema.codigo !== 'FICHA_NAO_ENCONTRADA') {
          this.erro.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }
}

/** Cuidador principal primeiro, depois os Cuidadores e os Familiares, cada grupo por nome. */
function ordenar(pessoas: readonly PessoaFichaDto[]): PessoaFichaDto[] {
  const ordem = (p: PessoaFichaDto): number => (p.principal ? 0 : p.papel === 'Cuidador' ? 1 : 2);
  return [...pessoas].sort((a, b) => ordem(a) - ordem(b) || a.nome.localeCompare(b.nome, 'pt'));
}
