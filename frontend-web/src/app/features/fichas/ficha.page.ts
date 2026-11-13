import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DocumentosApi } from '../../api/documentos.api';
import { FichasApi } from '../../api/fichas.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { CATEGORIAS_DOCUMENTO, CategoriaDocumento, DocumentoDto, FichaDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import { formatarData, formatarInstante, formatarTamanho, rotuloCategoria } from './apresentacao-ficha';
import { descricaoPapel, permissoesFicha } from './permissoes-ficha';
import { ResultadoFicha } from './resultados-ficha';
import { ACEITAR_DOCUMENTOS, validarDocumento } from './validacao-documento';

/** Ação de F002 que pede confirmação. */
type AcaoFicha = 'arquivar' | 'reativar' | 'eliminar';

/** Texto da confirmação e do resultado de cada ação. */
const ACOES: Readonly<Record<AcaoFicha, { pergunta: string; botao: string; sucesso?: string }>> = {
  arquivar: {
    pergunta:
      'Arquivar a ficha? Fica só de consulta, com todo o histórico, e deixa de gerar tomas, avisos e notificações ' +
      'até ser reativada.',
    botao: 'Sim, arquivar a ficha',
    sucesso: 'Ficha arquivada. Fica só de consulta até ser reativada.',
  },
  reativar: {
    pergunta:
      'Reativar a ficha? Volta a poder ser alterada e a gerar tomas, avisos e notificações a partir de agora.',
    botao: 'Sim, reativar a ficha',
    sucesso: 'Ficha reativada.',
  },
  eliminar: {
    pergunta:
      'Eliminar a ficha? A ficha, os documentos, os compromissos e os convites são removidos e não podem ser ' +
      'recuperados. Uma ficha com medicação ou histórico não pode ser eliminada, só arquivada.',
    botao: 'Sim, eliminar a ficha',
  },
};

/** Mensagens de sucesso vindas da rota, depois de criar ou editar. */
const RESULTADOS: Readonly<Record<string, string>> = {
  [ResultadoFicha.criada]: 'Ficha criada.',
  [ResultadoFicha.criadaComConvite]:
    'Ficha criada e convite enviado para Cuidador principal. Fica como Cuidador principal provisório até a pessoa ' +
    'responder.',
  [ResultadoFicha.editada]: 'Alterações guardadas.',
};

/** Códigos que indicam que a ficha mudou entretanto: a página volta a carregá-la para ficar coerente. */
const CODIGOS_RECARREGAR = new Set([
  'FICHA_ARQUIVADA',
  'FICHA_JA_ARQUIVADA',
  'FICHA_NAO_ARQUIVADA',
  'ACESSO_PROIBIDO',
]);

/**
 * Detalhe de uma ficha (OP-10 e OP-16; UC-07, UC-09 a UC-12): identificação, necessidades e documentos, com o
 * descarregamento (OP-17) e o carregamento (OP-15) de documentos e as ações arquivar (OP-12), reativar (OP-13) e
 * eliminar (OP-14), cada uma com confirmação. As ações visíveis vêm de {@link permissoesFicha}. Se um ficheiro não
 * puder ser obtido (`503` `DOCUMENTO_INDISPONIVEL`), o erro aparece no lugar do ficheiro, com a opção de repetir
 * (RF-124).
 */
@Component({
  selector: 'app-ficha-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BannerErroComponent],
  template: `
    <p><a routerLink="/fichas">Voltar às fichas</a></p>

    @if (sucesso(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    @if (ficha(); as f) {
      <div class="titulo-pagina">
        <h1>{{ f.nomeCompleto }}</h1>
        <span class="etiqueta" [class.etiqueta--arquivada]="f.estado === 'Arquivada'">{{
          f.estado === 'Ativa' ? 'Ativa' : 'Arquivada'
        }}</span>
        @if (f.eProprio) {
          <span class="etiqueta">Ficha própria</span>
        }
        <span class="etiqueta">O meu papel: {{ papel() }}</span>
      </div>

      @if (f.estado === 'Arquivada') {
        <p class="banner banner--aviso" role="status">
          Ficha arquivada: só pode ser consultada.
          @if (permissoes()?.reativar) {
            Para a alterar, reative-a.
          }
        </p>
      } @else if (f.meuPapel === 'Familiar') {
        <p class="banner banner--aviso">Como Familiar, pode consultar a ficha e descarregar os documentos.</p>
      }

      <section class="cartao cartao--largo">
        <h2>Identificação</h2>
        <dl class="dados">
          <dt>Nome completo</dt>
          <dd>{{ f.nomeCompleto }}</dd>
          <dt>Data de nascimento</dt>
          <dd>{{ data(f.dataNascimento) }}</dd>
          <dt>Contacto de emergência</dt>
          <dd>{{ f.contactoEmergencia }}</dd>
        </dl>
      </section>

      <section class="cartao cartao--largo">
        <h2>Necessidades específicas</h2>
        <dl class="dados">
          <dt>Grau de autonomia e mobilidade</dt>
          <dd>{{ f.grauAutonomia?.designacao ?? 'Sem indicação' }}</dd>
          <dt>Condições de saúde</dt>
          <dd>{{ lista(f.condicoesSaude) }}</dd>
          <dt>Alergias</dt>
          <dd>{{ lista(f.alergias) }}</dd>
        </dl>
      </section>

      <section class="cartao cartao--largo">
        <h2>Documentos</h2>
        <app-banner-erro [mensagem]="erroDocumentos()" />
        @let docs = documentos();
        @if (docs === null) {
          @if (!erroDocumentos()) {
            <p role="status">A carregar os documentos…</p>
          }
        } @else if (docs.length === 0) {
          <p>A ficha não tem documentos.</p>
        } @else {
          <ul class="lista-documentos">
            @for (documento of docs; track documento.id) {
              <li>
                <span class="lista-documentos__nome">{{ documento.nomeOriginal }}</span>
                <span>
                  {{ categoria(documento.categoria) }} · {{ documento.formato }} ·
                  {{ tamanho(documento.tamanhoBytes) }} · carregado em {{ instante(documento.carregadoEm) }}
                </span>
                @if (errosDescarregar()[documento.id]; as mensagem) {
                  <p class="campo__erro" role="alert">{{ mensagem }}</p>
                }
                @if (permissoes()?.descarregarDocumentos) {
                  <button
                    type="button"
                    class="botao botao--secundario"
                    [disabled]="aDescarregar().has(documento.id)"
                    (click)="descarregar(documento)"
                  >
                    @if (aDescarregar().has(documento.id)) {
                      A descarregar…
                    } @else if (errosDescarregar()[documento.id]) {
                      Tentar novamente descarregar {{ documento.nomeOriginal }}
                    } @else {
                      Descarregar {{ documento.nomeOriginal }}
                    }
                  </button>
                }
              </li>
            }
          </ul>
        }

        @if (permissoes()?.carregarDocumentos) {
          <form class="grupo" [formGroup]="carregamento" (ngSubmit)="carregar(seletor)" novalidate>
            <h3>Carregar documento</h3>
            <p class="ajuda">PDF, JPG ou PNG, com 10 MB no máximo.</p>
            <app-banner-erro [mensagem]="erroCarregamento()" />
            @if (documentoCarregado(); as nome) {
              <p class="banner banner--sucesso" role="status">Documento «{{ nome }}» carregado.</p>
            }
            <div class="campo" [class.campo--erro]="erroDe(carregamento.controls.categoria)">
              <label for="ficha-carregar-categoria">Categoria</label>
              <select
                id="ficha-carregar-categoria"
                [formControl]="carregamento.controls.categoria"
                [attr.aria-invalid]="erroDe(carregamento.controls.categoria) ? 'true' : null"
                [attr.aria-describedby]="
                  erroDe(carregamento.controls.categoria) ? 'ficha-carregar-categoria-erro' : null
                "
              >
                <option value="">Escolha a categoria</option>
                @for (opcao of categorias; track opcao.valor) {
                  <option [value]="opcao.valor">{{ opcao.rotulo }}</option>
                }
              </select>
              @if (erroDe(carregamento.controls.categoria); as mensagem) {
                <p class="campo__erro" id="ficha-carregar-categoria-erro">{{ mensagem }}</p>
              }
            </div>
            <div class="campo" [class.campo--erro]="erroDe(carregamento.controls.ficheiro)">
              <label for="ficha-carregar-ficheiro">Ficheiro</label>
              <input
                #seletor
                id="ficha-carregar-ficheiro"
                type="file"
                [accept]="aceitar"
                [attr.aria-invalid]="erroDe(carregamento.controls.ficheiro) ? 'true' : null"
                [attr.aria-describedby]="
                  erroDe(carregamento.controls.ficheiro) ? 'ficha-carregar-ficheiro-erro' : null
                "
                (change)="ficheiroEscolhido(seletor)"
              />
              @if (erroDe(carregamento.controls.ficheiro); as mensagem) {
                <p class="campo__erro" id="ficha-carregar-ficheiro-erro">{{ mensagem }}</p>
              }
            </div>
            <button type="submit" class="botao" [disabled]="aCarregarDocumento()">
              {{ aCarregarDocumento() ? 'A carregar…' : 'Carregar documento' }}
            </button>
          </form>
        }
      </section>

      @if (temAcoes()) {
        <section class="cartao cartao--largo">
          <h2>Ações</h2>
          @if (porConfirmar(); as acao) {
            <div class="confirmacao" role="group" aria-labelledby="ficha-confirmacao-texto">
              <p id="ficha-confirmacao-texto">{{ textos[acao].pergunta }}</p>
              <div class="acoes">
                <button type="button" class="botao" [disabled]="aExecutar()" (click)="executar(acao)">
                  {{ aExecutar() ? 'A processar…' : textos[acao].botao }}
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
            <div class="acoes">
              @if (permissoes()?.editar) {
                <a class="botao" [routerLink]="['/fichas', f.id, 'editar']">Editar ficha</a>
              }
              @if (permissoes()?.arquivar) {
                <button type="button" class="botao botao--secundario" (click)="pedirConfirmacao('arquivar')">
                  Arquivar ficha
                </button>
              }
              @if (permissoes()?.reativar) {
                <button type="button" class="botao botao--secundario" (click)="pedirConfirmacao('reativar')">
                  Reativar ficha
                </button>
              }
              @if (permissoes()?.eliminar) {
                <button type="button" class="botao botao--perigo" (click)="pedirConfirmacao('eliminar')">
                  Eliminar ficha
                </button>
              }
            </div>
          }
        </section>
      }
    } @else if (aCarregar()) {
      <p role="status">A carregar a ficha…</p>
    }
  `,
})
export class FichaPage implements OnChanges {
  private readonly fichasApi = inject(FichasApi);
  private readonly documentosApi = inject(DocumentosApi);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `resultado` da rota, depois de criar ou editar. */
  readonly resultado = input<string | undefined>();

  protected readonly textos = ACOES;
  protected readonly categorias = CATEGORIAS_DOCUMENTO;
  protected readonly aceitar = ACEITAR_DOCUMENTOS;
  protected readonly data = formatarData;
  protected readonly instante = formatarInstante;
  protected readonly tamanho = formatarTamanho;
  protected readonly categoria = rotuloCategoria;

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly documentos = signal<DocumentoDto[] | null>(null);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly erroDocumentos = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);

  protected readonly porConfirmar = signal<AcaoFicha | null>(null);
  protected readonly aExecutar = signal(false);

  protected readonly aDescarregar = signal<ReadonlySet<string>>(new Set());
  protected readonly errosDescarregar = signal<Readonly<Record<string, string>>>({});

  protected readonly carregamento = new FormGroup({
    categoria: new FormControl<CategoriaDocumento | ''>('', {
      nonNullable: true,
      validators: [
        (controlo) => (controlo.value ? null : { categoria: 'Escolha a categoria do documento.' }),
      ],
    }),
    ficheiro: new FormControl<File | null>(null, {
      validators: [(controlo) => (controlo.value ? null : { ficheiro: 'Escolha o ficheiro a carregar.' })],
    }),
  });
  protected readonly aCarregarDocumento = signal(false);
  protected readonly erroCarregamento = signal<string | null>(null);
  protected readonly documentoCarregado = signal<string | null>(null);

  protected readonly permissoes = computed(() => {
    const ficha = this.ficha();
    return ficha ? permissoesFicha(ficha) : null;
  });

  protected readonly temAcoes = computed(() => {
    const p = this.permissoes();
    return !!p && (p.editar || p.arquivar || p.reativar || p.eliminar);
  });

  protected readonly papel = computed(() => {
    const ficha = this.ficha();
    return ficha ? descricaoPapel(ficha) : '';
  });

  ngOnChanges(): void {
    this.erro.set(null);
    this.porConfirmar.set(null);
    this.sucesso.set(RESULTADOS[this.resultado() ?? ''] ?? null);
    this.carregarFicha();
  }

  protected erroDe(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  protected lista(itens: readonly { designacao: string }[]): string {
    return itens.length > 0 ? itens.map((item) => item.designacao).join(', ') : 'Nenhuma indicada';
  }

  protected pedirConfirmacao(acao: AcaoFicha): void {
    this.erro.set(null);
    this.sucesso.set(null);
    this.porConfirmar.set(acao);
  }

  /** Executa a ação confirmada e deixa o ecrã coerente: detalhe atualizado ou, depois de eliminar, a lista. */
  protected executar(acao: AcaoFicha): void {
    if (this.aExecutar()) {
      return;
    }

    const id = this.fichaId();
    const pedido =
      acao === 'arquivar'
        ? this.fichasApi.arquivar(id)
        : acao === 'reativar'
          ? this.fichasApi.reativar(id)
          : this.fichasApi.eliminar(id);

    this.aExecutar.set(true);
    pedido.subscribe({
      next: () => {
        this.aExecutar.set(false);
        this.porConfirmar.set(null);
        if (acao === 'eliminar') {
          void this.router.navigate(['/fichas'], { queryParams: { resultado: ResultadoFicha.eliminada } });
          return;
        }

        this.carregarFicha(ACOES[acao].sucesso);
      },
      error: (problema: ProblemaApi) => {
        this.aExecutar.set(false);
        this.porConfirmar.set(null);
        this.tratarErro(problema);
      },
    });
  }

  /** OP-17: obtém o ficheiro e entrega-o ao navegador para abrir ou guardar (RF-028); em falha permite repetir (RF-124). */
  protected descarregar(documento: DocumentoDto): void {
    if (this.aDescarregar().has(documento.id)) {
      return;
    }

    this.alterarDescarregamento(documento.id, true);
    this.definirErroDescarregar(documento.id, null);
    this.documentosApi.descarregar(documento.id).subscribe({
      next: (conteudo) => {
        this.alterarDescarregamento(documento.id, false);
        const endereco = URL.createObjectURL(conteudo);
        const ligacao = document.createElement('a');
        ligacao.href = endereco;
        ligacao.download = documento.nomeOriginal;
        document.body.appendChild(ligacao);
        ligacao.click();
        ligacao.remove();
        setTimeout(() => URL.revokeObjectURL(endereco), 60_000);
      },
      error: (problema: ProblemaApi) => {
        this.alterarDescarregamento(documento.id, false);
        this.definirErroDescarregar(documento.id, this.mensagens.mensagem(problema));
      },
    });
  }

  protected ficheiroEscolhido(seletor: HTMLInputElement): void {
    const ficheiro = seletor.files?.item(0) ?? null;
    const controlo = this.carregamento.controls.ficheiro;
    this.documentoCarregado.set(null);
    controlo.setValue(ficheiro);
    controlo.markAsTouched();
    const regra = ficheiro ? validarDocumento(ficheiro) : null;
    if (regra) {
      // Recusado antes do envio (RNF-008).
      controlo.setErrors({ formato: regra });
    }
  }

  /** OP-15: carrega um documento; em falha, a categoria e o ficheiro escolhidos mantêm-se (RF-023). */
  protected carregar(seletor: HTMLInputElement): void {
    this.erroCarregamento.set(null);
    this.documentoCarregado.set(null);
    this.carregamento.markAllAsTouched();
    const { categoria, ficheiro } = this.carregamento.getRawValue();
    if (this.carregamento.invalid || !ficheiro || !categoria || this.aCarregarDocumento()) {
      return;
    }

    this.aCarregarDocumento.set(true);
    this.documentosApi.carregar(this.fichaId(), { categoria, ficheiro }).subscribe({
      next: (documento) => {
        this.aCarregarDocumento.set(false);
        this.carregamento.reset();
        seletor.value = '';
        this.documentoCarregado.set(documento.nomeOriginal);
        this.documentos.update((lista) => [...(lista ?? []), documento]);
      },
      error: (problema: ProblemaApi) => {
        this.aCarregarDocumento.set(false);
        V.aplicarErrosDoServidor(this.carregamento, problema);
        if (CODIGOS_RECARREGAR.has(problema.codigo)) {
          // A ficha mudou (por exemplo foi arquivada): o formulário pode desaparecer, por isso a mensagem vai para cima.
          this.erro.set(this.mensagens.mensagem(problema));
          this.carregarFicha();
        } else {
          this.erroCarregamento.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }

  /** OP-10 e OP-16. */
  private carregarFicha(sucesso?: string): void {
    const id = this.fichaId();
    if (sucesso) {
      this.sucesso.set(sucesso);
    }

    this.aCarregar.set(this.ficha()?.id !== id);
    this.fichasApi.consultar(id).subscribe({
      next: (ficha) => {
        this.ficha.set(ficha);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        if (problema.codigo === 'FICHA_NAO_ENCONTRADA') {
          this.ficha.set(null);
        }

        this.erro.set(this.mensagens.mensagem(problema));
      },
    });

    this.erroDocumentos.set(null);
    this.documentosApi.listar(id).subscribe({
      next: (documentos) => this.documentos.set(documentos),
      error: (problema: ProblemaApi) => {
        this.documentos.set(null);
        // Sem a ficha, o erro principal já explica; os documentos só mostram o seu erro com a ficha visível.
        if (problema.codigo !== 'FICHA_NAO_ENCONTRADA') {
          this.erroDocumentos.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }

  private tratarErro(problema: ProblemaApi): void {
    this.erro.set(this.mensagens.mensagem(problema));
    if (problema.codigo === 'FICHA_NAO_ENCONTRADA') {
      this.ficha.set(null);
    } else if (CODIGOS_RECARREGAR.has(problema.codigo)) {
      this.carregarFicha();
    }
  }

  private alterarDescarregamento(id: string, ativo: boolean): void {
    this.aDescarregar.update((atual) => {
      const novo = new Set(atual);
      if (ativo) {
        novo.add(id);
      } else {
        novo.delete(id);
      }

      return novo;
    });
  }

  private definirErroDescarregar(id: string, mensagem: string | null): void {
    this.errosDescarregar.update((atual) => {
      const novo = { ...atual };
      if (mensagem) {
        novo[id] = mensagem;
      } else {
        delete novo[id];
      }

      return novo;
    });
  }
}
