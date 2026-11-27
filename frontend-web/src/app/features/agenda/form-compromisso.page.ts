import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { CompromissosApi } from '../../api/compromissos.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { AntecedenciaAviso, CompromissoDto, DadosCompromissoDto } from '../../models/agenda.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import { formatarInstanteToma } from '../medicacao/apresentacao-toma';
import {
  ANTECEDENCIAS,
  ANTECEDENCIA_POR_OMISSAO,
  avisoJaPassou,
  compromissoNoPassado,
  dataHoraLisboa,
} from './apresentacao-agenda';

/** Comprimentos máximos aceites pelo servidor (OP-41). */
const COMPRIMENTO_MAXIMO_LOCAL = 150;
const COMPRIMENTO_MAXIMO_MOTIVO = 250;

/** Erros do servidor que não dizem respeito a um campo: a página mostra a mensagem e não grava nada. */
const CODIGOS_SEM_CAMPO = new Set(['FICHA_ARQUIVADA', 'ACESSO_PROIBIDO', 'RECURSO_NAO_ENCONTRADO']);

/**
 * E22 Registar ou editar compromisso (OP-41 a OP-44; UC-25). Data, hora, local e motivo são obrigatórios (RF-080) e a
 * antecedência do aviso é 30 minutos, 1 hora, 2 horas, 24 horas (por omissão) ou 48 horas (RF-116). Os erros aparecem
 * junto de cada campo e os dados mantêm-se (RNF-028). Na edição, «Remover» pede confirmação e cancela o aviso
 * (RF-081, RF-082, RF-117). A data e a hora são as de Lisboa (RNF-029, RNF-047). A rota exige o papel Cuidador e a
 * ficha ativa.
 */
@Component({
  selector: 'app-form-compromisso-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BannerErroComponent, CampoFormularioComponent],
  template: `
    <p><a routerLink="/agenda" [queryParams]="regresso()">Voltar à agenda</a></p>

    <h1>{{ edicao() ? 'Editar compromisso' : 'Novo compromisso' }}</h1>
    @if (nomeFicha(); as nome) {
      <p>Ficha: {{ nome }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    @if (aCarregar()) {
      <p role="status">A carregar o compromisso…</p>
    } @else if (!edicao() || original()) {
      <form class="cartao cartao--largo" [formGroup]="formulario" (ngSubmit)="guardar()" novalidate>
        <app-campo-formulario rotulo="Data" tipo="date" [controlo]="formulario.controls.dia" />
        <app-campo-formulario rotulo="Hora" tipo="time" [controlo]="formulario.controls.hora" />
        <p class="ajuda">Hora de Portugal continental.</p>
        <app-campo-formulario rotulo="Local" [controlo]="formulario.controls.local" />
        <app-campo-formulario rotulo="Motivo" [controlo]="formulario.controls.motivo" />

        <div class="campo">
          <label for="compromisso-antecedencia">Avisar com antecedência de</label>
          <select id="compromisso-antecedencia" [formControl]="formulario.controls.antecedenciaAvisoMinutos">
            @for (a of antecedencias; track a.valor) {
              <option [ngValue]="a.valor">
                {{ a.rotulo }}{{ a.valor === porOmissao ? ' (por omissão)' : '' }}
              </option>
            }
          </select>
        </div>
        <p class="ajuda" id="compromisso-aviso">
          Os Cuidadores e os Familiares da ficha recebem uma notificação quando faltar esse tempo.
        </p>
        @if (noPassado()) {
          <p class="banner banner--aviso" role="status">
            A data e a hora já passaram: o compromisso fica registado, mas não é avisado.
          </p>
        } @else if (avisoPassou()) {
          <p class="banner banner--aviso" role="status">
            O momento do aviso já passou: a notificação é criada logo depois de guardar.
          </p>
        }

        <div class="acoes">
          <button type="submit" class="botao" [disabled]="aGuardar()">
            {{ aGuardar() ? 'A guardar…' : 'Guardar' }}
          </button>
          <a class="botao botao--secundario" routerLink="/agenda" [queryParams]="regresso()">Cancelar</a>
        </div>
      </form>

      @if (original(); as compromisso) {
        <section class="cartao cartao--largo" aria-labelledby="remover-compromisso">
          <h2 id="remover-compromisso">Remover compromisso</h2>
          @if (aConfirmarRemocao()) {
            <div class="confirmacao" role="group" aria-labelledby="pergunta-remover">
              <p id="pergunta-remover">
                Remover o compromisso «{{ compromisso.motivo }}» de {{ descricao(compromisso) }}? O aviso
                deixa de ser enviado.
              </p>
              <div class="acoes">
                <button type="button" class="botao" [disabled]="aRemover()" (click)="remover()">
                  {{ aRemover() ? 'A remover…' : 'Sim, remover o compromisso' }}
                </button>
                <button
                  type="button"
                  class="botao botao--secundario"
                  [disabled]="aRemover()"
                  (click)="aConfirmarRemocao.set(false)"
                >
                  Cancelar
                </button>
              </div>
            </div>
          } @else {
            <button type="button" class="botao botao--secundario" (click)="aConfirmarRemocao.set(true)">
              Remover
            </button>
          }
        </section>
      }
    }
  `,
})
export class FormCompromissoPage implements OnChanges {
  private readonly compromissosApi = inject(CompromissosApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);
  private readonly router = inject(Router);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();
  /** Parâmetro `compromissoId` da rota de edição; ausente no registo. */
  readonly compromissoId = input<string | undefined>();
  /** Vista da agenda de onde se veio, para o regresso. */
  readonly vista = input<string | undefined>();
  /** Data da agenda de onde se veio, para o regresso. */
  readonly data = input<string | undefined>();

  protected readonly antecedencias = ANTECEDENCIAS;
  protected readonly porOmissao = ANTECEDENCIA_POR_OMISSAO;

  protected readonly original = signal<CompromissoDto | null>(null);
  protected readonly aCarregar = signal(false);
  protected readonly aGuardar = signal(false);
  protected readonly aRemover = signal(false);
  protected readonly aConfirmarRemocao = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly edicao = computed(() => !!this.compromissoId());
  protected readonly nomeFicha = computed(() => {
    const ficha = this.contexto.ficha();
    return ficha?.id === this.fichaId() ? ficha.nomeCompleto : null;
  });
  protected readonly regresso = computed(() => {
    const vista = this.vista();
    const data = this.data();
    return vista && data ? { vista, data } : {};
  });

  protected readonly formulario = new FormGroup({
    dia: new FormControl('', { nonNullable: true, validators: [V.obrigatorio()] }),
    hora: new FormControl('', { nonNullable: true, validators: [V.obrigatorio()] }),
    local: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_LOCAL)],
    }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_MOTIVO)],
    }),
    antecedenciaAvisoMinutos: new FormControl<AntecedenciaAviso>(ANTECEDENCIA_POR_OMISSAO, {
      nonNullable: true,
    }),
  });

  private readonly valores = toSignal(this.formulario.valueChanges, {
    initialValue: this.formulario.getRawValue(),
  });

  private readonly dataHora = computed(() => {
    const { dia, hora } = { ...this.formulario.getRawValue(), ...this.valores() };
    return dia && hora ? dataHoraLisboa(dia, hora) : null;
  });

  protected readonly noPassado = computed(() => {
    const dataHora = this.dataHora();
    return !!dataHora && compromissoNoPassado(dataHora);
  });

  protected readonly avisoPassou = computed(() => {
    const dataHora = this.dataHora();
    const antecedencia = this.valores().antecedenciaAvisoMinutos ?? ANTECEDENCIA_POR_OMISSAO;
    return !!dataHora && avisoJaPassou(dataHora, antecedencia);
  });

  ngOnChanges(): void {
    this.erro.set(null);
    const id = this.compromissoId();
    if (!id) {
      this.original.set(null);
      return;
    }

    // Não há operação para obter um compromisso: lê os da ficha (OP-42) e escolhe o pedido.
    this.aCarregar.set(true);
    this.compromissosApi.listar(this.fichaId()).subscribe({
      next: (compromissos) => {
        this.aCarregar.set(false);
        const compromisso = compromissos.find((c) => c.id === id);
        if (!compromisso) {
          this.erro.set(
            this.mensagens.mensagem({ estado: 404, codigo: 'RECURSO_NAO_ENCONTRADO', mensagem: '' }),
          );
          return;
        }

        this.original.set(compromisso);
        this.formulario.reset({
          dia: compromisso.dataHora.slice(0, 10),
          hora: formatarInstanteToma(compromisso.dataHora).hora,
          local: compromisso.local,
          motivo: compromisso.motivo,
          antecedenciaAvisoMinutos: compromisso.antecedenciaAvisoMinutos,
        });
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  protected descricao(compromisso: CompromissoDto): string {
    const { data, hora } = formatarInstanteToma(compromisso.dataHora);
    return `${data} às ${hora}`;
  }

  /** OP-41 no registo e OP-43 na edição (todos os campos). Em erro, os dados mantêm-se (RNF-028). */
  protected guardar(): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aGuardar()) {
      return;
    }

    const valores = this.formulario.getRawValue();
    const dados: DadosCompromissoDto = {
      dataHora: dataHoraLisboa(valores.dia, valores.hora),
      local: valores.local.trim(),
      motivo: valores.motivo.trim(),
      antecedenciaAvisoMinutos: valores.antecedenciaAvisoMinutos,
    };
    const original = this.original();
    const pedido: Observable<CompromissoDto> = original
      ? this.compromissosApi.editar(original.id, dados)
      : this.compromissosApi.registar(this.fichaId(), dados);

    this.aGuardar.set(true);
    pedido.subscribe({
      next: (compromisso) => {
        this.aGuardar.set(false);
        // Volta à agenda no dia do compromisso, para o ver logo (RF-084).
        void this.router.navigate(['/agenda'], {
          queryParams: {
            vista: this.vista() === 'semana' || this.vista() === 'mes' ? this.vista() : 'dia',
            data: compromisso.dataHora.slice(0, 10),
            resultado: original ? 'editado' : 'registado',
          },
        });
      },
      error: (problema: ProblemaApi) => {
        this.aGuardar.set(false);
        this.tratarErro(problema);
      },
    });
  }

  /** OP-44, depois da confirmação; o aviso por enviar é cancelado (RF-082, RF-117). */
  protected remover(): void {
    const original = this.original();
    if (!original || this.aRemover()) {
      return;
    }

    this.erro.set(null);
    this.aRemover.set(true);
    this.compromissosApi.remover(original.id).subscribe({
      next: () => {
        this.aRemover.set(false);
        void this.router.navigate(['/agenda'], {
          queryParams: { ...this.regresso(), resultado: 'removido' },
        });
      },
      error: (problema: ProblemaApi) => {
        this.aRemover.set(false);
        this.aConfirmarRemocao.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  private tratarErro(problema: ProblemaApi): void {
    if (!CODIGOS_SEM_CAMPO.has(problema.codigo)) {
      // O servidor indica a data e a hora num só campo, `dataHora`, que corresponde ao campo da data.
      V.aplicarErrosDoServidor(this.formulario, {
        ...problema,
        campos: problema.campos?.map((c) => ({ ...c, campo: c.campo === 'dataHora' ? 'dia' : c.campo })),
      });
    }
    this.erro.set(this.mensagens.mensagem(problema));
  }
}
