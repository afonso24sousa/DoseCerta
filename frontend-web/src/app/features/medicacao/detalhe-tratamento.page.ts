import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DocumentosApi } from '../../api/documentos.api';
import { TratamentosApi } from '../../api/tratamentos.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { TratamentoDto } from '../../models/tratamentos.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import {
  ResultadoMedicacao,
  textoEstado,
  textoGrau,
  textoHorarios,
  textoPeriodo,
} from './apresentacao-tratamento';

/** Extensão do ficheiro da receita descarregada, pelo tipo de conteúdo. */
const EXTENSOES: Readonly<Record<string, string>> = {
  'application/pdf': '.pdf',
  'image/jpeg': '.jpg',
  'image/png': '.png',
};

/** Códigos que indicam que o tratamento ou a ficha mudaram entretanto: a página volta a carregá-los. */
const CODIGOS_RECARREGAR = new Set(['TRATAMENTO_JA_TERMINADO', 'ACESSO_PROIBIDO']);

/**
 * E17 Detalhe do tratamento (OP-29, OP-31, OP-17; UC-18, UC-19): os dados e o estado do tratamento e a receita, se
 * existir. «Editar» só aparece ao Cuidador, numa ficha ativa e com o tratamento em curso (RF-055); «Terminar
 * tratamento» ao Cuidador com o tratamento em curso, também numa ficha arquivada (RF-056; modelo de classes v02, secção
 * 1). O Familiar só consulta (RF-048). As tomas do tratamento e a sua confirmação (OP-32, OP-33, E18) são da I068.
 */
@Component({
  selector: 'app-detalhe-tratamento-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId(), 'medicacao']">Voltar à medicação</a></p>

    @if (sucesso(); as mensagem) {
      <p class="banner banner--sucesso" role="status">{{ mensagem }}</p>
    }
    @if (aviso(); as mensagem) {
      <p class="banner banner--aviso" role="status">{{ mensagem }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    @if (tratamento(); as t) {
      <div class="titulo-pagina">
        <h1>{{ t.medicamento }} {{ t.dose }}</h1>
        <span class="etiqueta" [class.etiqueta--arquivada]="t.estado === 'Terminado'">{{
          estado(t.estado)
        }}</span>
      </div>

      <section class="cartao cartao--largo">
        <h2>Tratamento</h2>
        <dl class="dados">
          <dt>Grau de importância</dt>
          <dd>{{ grau(t.grauImportancia) }}</dd>
          <dt>Horários</dt>
          <dd>{{ horarios(t.horarios) }}</dd>
          <dt>Período</dt>
          <dd>{{ periodo(t) }}</dd>
          <dt>Receita médica</dt>
          <dd>
            @if (t.receitaDocumentoId) {
              <button
                type="button"
                class="botao botao--secundario"
                [disabled]="aAbrirReceita()"
                (click)="abrirReceita(t)"
              >
                {{
                  aAbrirReceita()
                    ? 'A obter a receita…'
                    : erroReceita()
                      ? 'Tentar novamente abrir a receita'
                      : 'Abrir a receita'
                }}
              </button>
              @if (erroReceita(); as mensagem) {
                <p class="campo__erro" role="alert">{{ mensagem }}</p>
              }
            } @else {
              Sem receita
            }
          </dd>
        </dl>
      </section>

      <!--
        I068: tomas do tratamento e confirmação (OP-32 com tratamentoId, OP-33, E18 ConfirmarItemDialog). O
        ListaTomasComponent entra nesta secção, com tratamento() e podeConfirmar() desta página.
      -->
      <section id="tomas-tratamento"></section>

      @if (podeEditar() || podeTerminar()) {
        <section class="cartao cartao--largo">
          <h2>Ações</h2>
          @if (aConfirmarTermino()) {
            <div class="confirmacao" role="group" aria-labelledby="tratamento-confirmacao-texto">
              <p id="tratamento-confirmacao-texto">
                Terminar o tratamento? As tomas futuras deixam de existir; o tratamento e as tomas já
                registadas continuam disponíveis para consulta.
              </p>
              <div class="acoes">
                <button type="button" class="botao" [disabled]="aTerminar()" (click)="terminar()">
                  {{ aTerminar() ? 'A terminar…' : 'Sim, terminar o tratamento' }}
                </button>
                <button
                  type="button"
                  class="botao botao--secundario"
                  [disabled]="aTerminar()"
                  (click)="aConfirmarTermino.set(false)"
                >
                  Cancelar
                </button>
              </div>
            </div>
          } @else {
            <div class="acoes">
              @if (podeEditar()) {
                <a class="botao" [routerLink]="['/fichas', fichaId(), 'medicacao', t.id, 'editar']">Editar</a>
              }
              @if (podeTerminar()) {
                <button type="button" class="botao botao--perigo" (click)="pedirTermino()">
                  Terminar tratamento
                </button>
              }
            </div>
          }
        </section>
      }
    } @else if (aCarregar()) {
      <p role="status">A carregar o tratamento…</p>
    }
  `,
})
export class DetalheTratamentoPage implements OnChanges {
  private readonly tratamentosApi = inject(TratamentosApi);
  private readonly documentosApi = inject(DocumentosApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `tratamentoId` da rota. */
  readonly tratamentoId = input.required<string>();

  /** Parâmetro `resultado` da rota, depois de editar ou de uma edição recusada. */
  readonly resultado = input<string | undefined>();

  protected readonly estado = textoEstado;
  protected readonly grau = textoGrau;
  protected readonly horarios = textoHorarios;
  protected readonly periodo = textoPeriodo;

  readonly tratamento = signal<TratamentoDto | null>(null);
  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly sucesso = signal<string | null>(null);
  protected readonly aviso = signal<string | null>(null);
  protected readonly aConfirmarTermino = signal(false);
  protected readonly aTerminar = signal(false);
  protected readonly aAbrirReceita = signal(false);
  protected readonly erroReceita = signal<string | null>(null);

  /** O Utilizador é Cuidador da ficha: pode confirmar tomas (I068) e terminar o tratamento (RF-047, RF-048). */
  readonly podeConfirmar = computed(() => this.ficha()?.meuPapel === 'Cuidador');

  protected readonly podeEditar = computed(
    () => this.podeConfirmar() && this.ficha()?.estado === 'Ativa' && this.tratamento()?.estado === 'EmCurso',
  );

  /** Terminar continua permitido numa ficha arquivada (RF-056, RF-120). */
  protected readonly podeTerminar = computed(
    () => this.podeConfirmar() && this.tratamento()?.estado === 'EmCurso',
  );

  ngOnChanges(): void {
    const resultado = this.resultado();
    this.erro.set(null);
    this.aConfirmarTermino.set(false);
    this.sucesso.set(resultado === ResultadoMedicacao.atualizado ? 'Alterações guardadas.' : null);
    this.aviso.set(
      resultado === ResultadoMedicacao.naoEditavel
        ? 'O tratamento já terminou e não pode ser editado; continua disponível para consulta.'
        : null,
    );
    const atual = this.contexto.ficha();
    this.ficha.set(atual?.id === this.fichaId() ? atual : null);
    this.carregar();
  }

  protected pedirTermino(): void {
    this.erro.set(null);
    this.sucesso.set(null);
    this.aviso.set(null);
    this.aConfirmarTermino.set(true);
  }

  /** OP-31: o tratamento passa a Terminado e continua consultável (RF-056, RF-057). */
  protected terminar(): void {
    const tratamento = this.tratamento();
    if (!tratamento || this.aTerminar()) {
      return;
    }

    this.aTerminar.set(true);
    this.tratamentosApi.terminar(tratamento.id).subscribe({
      next: (terminado) => {
        this.aTerminar.set(false);
        this.aConfirmarTermino.set(false);
        this.tratamento.set(terminado);
        this.sucesso.set(
          'Tratamento terminado. As tomas futuras deixaram de existir; o histórico mantém-se.',
        );
      },
      error: (problema: ProblemaApi) => {
        this.aTerminar.set(false);
        this.aConfirmarTermino.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
        if (CODIGOS_RECARREGAR.has(problema.codigo)) {
          this.carregar();
        }
      },
    });
  }

  /** OP-17: obtém a receita e entrega-a ao navegador para abrir ou guardar; em falha permite repetir (RF-124). */
  protected abrirReceita(tratamento: TratamentoDto): void {
    const documentoId = tratamento.receitaDocumentoId;
    if (!documentoId || this.aAbrirReceita()) {
      return;
    }

    this.aAbrirReceita.set(true);
    this.erroReceita.set(null);
    this.documentosApi.descarregar(documentoId).subscribe({
      next: (conteudo) => {
        this.aAbrirReceita.set(false);
        const endereco = URL.createObjectURL(conteudo);
        const ligacao = document.createElement('a');
        ligacao.href = endereco;
        ligacao.download = `receita-${tratamento.medicamento}${EXTENSOES[conteudo.type] ?? ''}`;
        document.body.appendChild(ligacao);
        ligacao.click();
        ligacao.remove();
        setTimeout(() => URL.revokeObjectURL(endereco), 60_000);
      },
      error: (problema: ProblemaApi) => {
        this.aAbrirReceita.set(false);
        this.erroReceita.set(this.mensagens.mensagem(problema));
      },
    });
  }

  /** OP-10 (papel e estado da ficha) e OP-29. */
  private carregar(): void {
    this.aCarregar.set(this.tratamento()?.id !== this.tratamentoId());
    this.contexto.obter(this.fichaId()).subscribe({
      next: (ficha) => this.ficha.set(ficha),
      error: (problema: ProblemaApi) => this.erro.set(this.mensagens.mensagem(problema)),
    });
    this.tratamentosApi.obter(this.tratamentoId()).subscribe({
      next: (tratamento) => {
        this.tratamento.set(tratamento);
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        this.tratamento.set(null);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
