import { Component, OnInit, inject, input, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { RegistosApi } from '../../api/registos.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import { RegistoDto, TipoRegisto } from '../../models/registos.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import { MAXIMO_DESCRICAO, ResultadoRegisto, agoraLisboa, instanteLisboa } from './apresentacao-registo';

const PADRAO_HORA = /^([01]\d|2[0-3]):[0-5]\d$/;
const PADRAO_DATA = /^\d{4}-\d{2}-\d{2}$/;

/** Códigos que significam que o Utilizador já não pode registar na ficha: volta a E23, que mostra o motivo. */
const CODIGOS_SEM_PERMISSAO = new Set(['ACESSO_PROIBIDO', 'FICHA_ARQUIVADA']);

/**
 * E24 Novo registo (OP-46 e OP-47; UC-27 e UC-28): uma ocorrência, com a descrição e a data e hora em que aconteceu
 * (por omissão agora, podendo ser anterior ou posterior), ou uma nota para os Cuidadores, só com o texto (RF-090,
 * RF-091). A data e hora são lidas em hora de Lisboa e enviadas com o desvio de Lisboa, qualquer que seja o fuso do
 * navegador (RNF-047). Antes de guardar, avisa que o registo não pode ser alterado nem eliminado (RF-090, RNF-049). Os
 * erros aparecem junto de cada campo e os dados escritos mantêm-se (RNF-028). A rota exige o papel Cuidador e a ficha
 * ativa.
 */
@Component({
  selector: 'app-novo-registo-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BannerErroComponent, CampoFormularioComponent],
  template: `
    <p>
      <a [routerLink]="['/fichas', fichaId(), 'registos']" [queryParams]="{ tipo: tipoEscolhido() }"
        >Voltar aos registos</a
      >
    </p>

    <h1>Novo registo</h1>
    <app-banner-erro [mensagem]="erro()" />

    <form class="cartao cartao--largo" [formGroup]="formulario" (ngSubmit)="guardar()" novalidate>
      <fieldset class="grupo grupo-opcoes">
        <legend>Tipo de registo</legend>
        <label class="opcao">
          <input type="radio" [formControl]="formulario.controls.tipo" value="Ocorrencia" />
          Ocorrência
        </label>
        <p class="ajuda">Os outros Cuidadores e os Familiares da ficha veem-na e são notificados.</p>
        <label class="opcao">
          <input type="radio" [formControl]="formulario.controls.tipo" value="Nota" />
          Nota para os Cuidadores
        </label>
        <p class="ajuda">Só os Cuidadores da ficha a veem.</p>
      </fieldset>

      <div class="campo" [class.campo--erro]="erroDe(formulario.controls.descricao)">
        <label for="registo-descricao">Descrição</label>
        <textarea
          id="registo-descricao"
          rows="5"
          [formControl]="formulario.controls.descricao"
          [attr.maxlength]="maximo"
          [attr.aria-invalid]="erroDe(formulario.controls.descricao) ? 'true' : null"
          [attr.aria-describedby]="
            erroDe(formulario.controls.descricao)
              ? 'registo-descricao-erro registo-descricao-ajuda'
              : 'registo-descricao-ajuda'
          "
        ></textarea>
        <p class="ajuda" id="registo-descricao-ajuda">
          {{ formulario.controls.descricao.value.length }} de {{ maximo }} caracteres.
        </p>
        @if (erroDe(formulario.controls.descricao); as mensagem) {
          <p class="campo__erro" id="registo-descricao-erro">Descrição: {{ minuscula(mensagem) }}</p>
        }
      </div>

      @if (formulario.controls.tipo.value === 'Ocorrencia') {
        <fieldset class="grupo">
          <legend>Data e hora em que aconteceu</legend>
          <app-campo-formulario rotulo="Data" tipo="date" [controlo]="formulario.controls.data" />
          <app-campo-formulario rotulo="Hora (hh:mm)" [controlo]="formulario.controls.hora" />
          <p class="ajuda">Em hora de Portugal continental. Por omissão, agora.</p>
        </fieldset>
      }

      <p class="banner banner--aviso">Depois de guardado, o registo não pode ser alterado nem eliminado.</p>

      <div class="acoes">
        <button type="submit" class="botao" [disabled]="aGuardar()">
          {{ aGuardar() ? 'A guardar…' : 'Guardar' }}
        </button>
        <a [routerLink]="['/fichas', fichaId(), 'registos']" [queryParams]="{ tipo: tipoEscolhido() }"
          >Cancelar</a
        >
      </div>
    </form>
  `,
})
export class NovoRegistoPage implements OnInit {
  private readonly registosApi = inject(RegistosApi);
  private readonly mensagens = inject(MensagensErroService);
  private readonly router = inject(Router);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `tipo` da rota: `Nota` escolhe a nota à partida; caso contrário, a ocorrência. */
  readonly tipo = input<string | undefined>();

  protected readonly maximo = MAXIMO_DESCRICAO;
  protected readonly erro = signal<string | null>(null);
  protected readonly aGuardar = signal(false);

  protected readonly formulario = new FormGroup({
    tipo: new FormControl<TipoRegisto>('Ocorrencia', { nonNullable: true }),
    descricao: new FormControl('', {
      nonNullable: true,
      validators: [
        (c: AbstractControl): ValidationErrors | null =>
          String(c.value ?? '').trim().length === 0
            ? { obrigatorio: 'É obrigatória. Descreva o que aconteceu ou o que quer dizer aos Cuidadores.' }
            : null,
        (c: AbstractControl): ValidationErrors | null =>
          String(c.value ?? '').trim().length > MAXIMO_DESCRICAO
            ? { comprimentoMaximo: `Tem no máximo ${MAXIMO_DESCRICAO} caracteres.` }
            : null,
      ],
    }),
    data: new FormControl('', {
      nonNullable: true,
      validators: [(c: AbstractControl): ValidationErrors | null => this.validarData(c)],
    }),
    hora: new FormControl('', {
      nonNullable: true,
      validators: [(c: AbstractControl): ValidationErrors | null => this.validarHora(c)],
    }),
  });

  ngOnInit(): void {
    const agora = agoraLisboa();
    this.formulario.patchValue({
      tipo: this.tipo() === 'Nota' ? 'Nota' : 'Ocorrencia',
      data: agora.data,
      hora: agora.hora,
    });
    // A data e a hora só contam na ocorrência: ao mudar de tipo, voltam a ser validadas.
    this.formulario.controls.tipo.valueChanges.subscribe(() => {
      this.formulario.controls.data.updateValueAndValidity();
      this.formulario.controls.hora.updateValueAndValidity();
    });
  }

  protected tipoEscolhido(): TipoRegisto {
    return this.formulario.controls.tipo.value;
  }

  protected erroDe(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  protected minuscula(texto: string): string {
    return `${texto.charAt(0).toLocaleLowerCase('pt-PT')}${texto.slice(1)}`;
  }

  /** OP-46 na ocorrência e OP-47 na nota. Em erro, os dados escritos mantêm-se (RNF-028). */
  protected guardar(): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aGuardar()) {
      return;
    }

    const valores = this.formulario.getRawValue();
    const descricao = valores.descricao.trim();
    let pedido: Observable<RegistoDto>;
    if (valores.tipo === 'Ocorrencia') {
      const ocorridaEm = instanteLisboa(valores.data, valores.hora);
      if (!ocorridaEm) {
        V.erroNoCampo(
          this.formulario.controls.hora,
          'Hora (hh:mm): não existe nesse dia, porque os relógios avançaram uma hora. Indique outra hora.',
        );
        return;
      }

      pedido = this.registosApi.registarOcorrencia(this.fichaId(), { descricao, ocorridaEm });
    } else {
      pedido = this.registosApi.registarNota(this.fichaId(), { descricao });
    }

    this.aGuardar.set(true);
    pedido.subscribe({
      next: () => {
        this.aGuardar.set(false);
        void this.router.navigate(['/fichas', this.fichaId(), 'registos'], {
          queryParams: {
            tipo: valores.tipo,
            resultado: valores.tipo === 'Ocorrencia' ? ResultadoRegisto.ocorrencia : ResultadoRegisto.nota,
          },
        });
      },
      error: (problema: ProblemaApi) => {
        this.aGuardar.set(false);
        this.tratarErro(problema);
      },
    });
  }

  private tratarErro(problema: ProblemaApi): void {
    if (CODIGOS_SEM_PERMISSAO.has(problema.codigo)) {
      // O papel ou o estado da ficha mudou entretanto: o registo não foi guardado; a mensagem fica neste ecrã.
      this.erro.set(`${this.mensagens.mensagem(problema)} O registo não foi guardado.`);
      return;
    }

    // `ocorridaEm` corresponde aos campos da data e da hora.
    V.aplicarErrosDoServidor(this.formulario, {
      ...problema,
      campos: problema.campos?.map((c) => ({ ...c, campo: c.campo === 'ocorridaEm' ? 'hora' : c.campo })),
    });
    this.erro.set(this.mensagens.mensagem(problema));
  }

  /** Data obrigatória na ocorrência, no formato do campo de data. */
  private validarData(controlo: AbstractControl): ValidationErrors | null {
    if ((this.formulario as FormGroup | undefined)?.controls['tipo']?.value !== 'Ocorrencia') {
      return null;
    }

    const valor = String(controlo.value ?? '');
    if (!valor) {
      return { obrigatorio: 'É obrigatória. Indique a data em que aconteceu.' };
    }

    return PADRAO_DATA.test(valor) && !Number.isNaN(Date.parse(valor))
      ? null
      : { data: 'Tem de ser uma data válida.' };
  }

  /** Hora obrigatória na ocorrência, no formato hh:mm. */
  private validarHora(controlo: AbstractControl): ValidationErrors | null {
    if ((this.formulario as FormGroup | undefined)?.controls['tipo']?.value !== 'Ocorrencia') {
      return null;
    }

    const valor = String(controlo.value ?? '').trim();
    if (!valor) {
      return { obrigatorio: 'É obrigatória. Indique a hora em que aconteceu, no formato hh:mm.' };
    }

    return PADRAO_HORA.test(valor) ? null : { hora: 'Tem de estar no formato hh:mm, por exemplo 15:05.' };
  }
}
