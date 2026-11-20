import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { TratamentosApi } from '../../api/tratamentos.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import {
  DadosTratamentoDto,
  GRAUS_IMPORTANCIA,
  GrauImportancia,
  TratamentoDto,
} from '../../models/tratamentos.models';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ListaHorasComponent } from '../../shared/lista-horas.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import { ACEITAR_DOCUMENTOS, validarDocumento } from '../fichas/validacao-documento';
import { ResultadoMedicacao } from './apresentacao-tratamento';

/** Comprimentos máximos aceites pelo servidor (`TratamentoService`). */
const COMPRIMENTO_MAXIMO_MEDICAMENTO = 120;
const COMPRIMENTO_MAXIMO_DOSE = 60;

/** Códigos de erro da receita, mostrados junto do campo (RNF-008). */
const ERROS_RECEITA = new Set(['FICHEIRO_DEMASIADO_GRANDE', 'FORMATO_NAO_SUPORTADO']);

/**
 * E16 Registar ou editar tratamento (OP-27, OP-29, OP-30; UC-17, UC-18). Registo: medicamento, dose, grau de importância
 * (um só, RF-051), data de início, data de fim opcional (vazia = contínuo, RF-050), horários e receita opcional (PDF, JPG
 * ou PNG até 10 MB, RF-052, RNF-008). Edição: só de tratamentos em curso, sem alterar a data de início, enviando só os
 * campos alterados (RF-055); o servidor não permite retirar a data de fim. Os erros aparecem junto de cada campo e os
 * dados mantêm-se (RNF-028); se a receita não for guardada, nada é gravado e o formulário mantém os dados e a receita
 * escolhida (RF-053). A rota exige o papel Cuidador e a ficha ativa.
 */
@Component({
  selector: 'app-form-tratamento-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    BannerErroComponent,
    CampoFormularioComponent,
    ListaHorasComponent,
  ],
  template: `
    <p>
      <a [routerLink]="voltar()">{{ edicao() ? 'Voltar ao tratamento' : 'Voltar à medicação' }}</a>
    </p>

    <h1>{{ edicao() ? 'Editar tratamento' : 'Novo tratamento' }}</h1>
    <app-banner-erro [mensagem]="erro()" />

    @if (aCarregar()) {
      <p role="status">A carregar o tratamento…</p>
    } @else {
      <form class="cartao cartao--largo" [formGroup]="formulario" (ngSubmit)="guardar(seletor)" novalidate>
        <app-campo-formulario rotulo="Medicamento" [controlo]="formulario.controls.medicamento" />
        <app-campo-formulario rotulo="Dose" [controlo]="formulario.controls.dose" />

        <app-lista-horas [controlo]="formulario.controls.horarios" />
        @if (edicao()) {
          <p class="ajuda">
            Alterar os horários só afeta as tomas futuras; as tomas anteriores mantêm a hora e o estado.
          </p>
        }

        <app-campo-formulario
          rotulo="Data de início"
          tipo="date"
          [controlo]="formulario.controls.dataInicio"
        />
        @if (edicao()) {
          <p class="ajuda">A data de início não pode ser alterada depois do registo.</p>
        }
        <app-campo-formulario rotulo="Data de fim" tipo="date" [controlo]="formulario.controls.dataFim" />
        @if (edicao() && original()?.dataFim) {
          <p class="ajuda">Num tratamento com data de fim, a data pode ser alterada mas não retirada.</p>
        } @else {
          <p class="ajuda">Opcional; vazia = tratamento contínuo.</p>
        }

        <fieldset
          class="grupo grupo-opcoes"
          [class.campo--erro]="erroDe(formulario.controls.grauImportancia)"
          [attr.aria-describedby]="
            erroDe(formulario.controls.grauImportancia) ? 'tratamento-grau-erro' : null
          "
        >
          <legend>Grau de importância</legend>
          @for (grau of graus; track grau.valor) {
            <label class="opcao">
              <input type="radio" [formControl]="formulario.controls.grauImportancia" [value]="grau.valor" />
              {{ grau.rotulo }}
            </label>
          }
          @if (erroDe(formulario.controls.grauImportancia); as mensagem) {
            <p class="campo__erro" id="tratamento-grau-erro">{{ mensagem }}</p>
          }
        </fieldset>

        <div class="campo" [class.campo--erro]="erroDe(formulario.controls.receita)">
          <label for="tratamento-receita">{{
            edicao() ? 'Receita médica nova (opcional)' : 'Receita médica (opcional)'
          }}</label>
          <input
            #seletor
            id="tratamento-receita"
            type="file"
            [accept]="aceitar"
            [attr.aria-invalid]="erroDe(formulario.controls.receita) ? 'true' : null"
            [attr.aria-describedby]="
              erroDe(formulario.controls.receita) ? 'tratamento-receita-erro' : 'tratamento-receita-ajuda'
            "
            (change)="receitaEscolhida(seletor)"
          />
          <p class="ajuda" id="tratamento-receita-ajuda">PDF, JPG ou PNG, com 10 MB no máximo.</p>
          @if (erroDe(formulario.controls.receita); as mensagem) {
            <p class="campo__erro" id="tratamento-receita-erro">{{ mensagem }}</p>
          }
        </div>

        <div class="acoes">
          <button type="submit" class="botao" [disabled]="aGuardar()">
            {{ aGuardar() ? 'A guardar…' : 'Guardar' }}
          </button>
          <a class="botao botao--secundario" [routerLink]="voltar()">Cancelar</a>
        </div>
      </form>
    }
  `,
})
export class FormTratamentoPage implements OnChanges {
  private readonly tratamentosApi = inject(TratamentosApi);
  private readonly mensagens = inject(MensagensErroService);
  private readonly router = inject(Router);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  /** Parâmetro `tratamentoId` da rota de edição; ausente no registo. */
  readonly tratamentoId = input<string | undefined>();

  protected readonly graus = GRAUS_IMPORTANCIA;
  protected readonly aceitar = ACEITAR_DOCUMENTOS;

  protected readonly original = signal<TratamentoDto | null>(null);
  protected readonly aCarregar = signal(false);
  protected readonly aGuardar = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly edicao = computed(() => !!this.tratamentoId());

  protected readonly voltar = computed(() => {
    const id = this.tratamentoId();
    return id ? ['/fichas', this.fichaId(), 'medicacao', id] : ['/fichas', this.fichaId(), 'medicacao'];
  });

  protected readonly formulario = new FormGroup({
    medicamento: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_MEDICAMENTO)],
    }),
    dose: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_DOSE)],
    }),
    horarios: new FormControl<string[]>([], {
      nonNullable: true,
      validators: [
        (c) =>
          (c.value as string[]).length > 0 ? null : { obrigatorio: 'Indique pelo menos uma hora de toma.' },
      ],
    }),
    dataInicio: new FormControl('', { nonNullable: true, validators: [V.obrigatorio()] }),
    dataFim: new FormControl('', {
      nonNullable: true,
      validators: [(c) => this.validarDataFim(c)],
    }),
    grauImportancia: new FormControl<GrauImportancia | ''>('', {
      nonNullable: true,
      validators: [(c) => (c.value ? null : { obrigatorio: 'Escolha o grau de importância.' })],
    }),
    receita: new FormControl<File | null>(null),
  });

  constructor() {
    // A data de fim depende da de início.
    this.formulario.controls.dataInicio.valueChanges.subscribe(() =>
      this.formulario.controls.dataFim.updateValueAndValidity({ emitEvent: false }),
    );
  }

  ngOnChanges(): void {
    this.erro.set(null);
    const id = this.tratamentoId();
    if (!id) {
      this.original.set(null);
      this.formulario.controls.dataInicio.enable();
      return;
    }

    this.aCarregar.set(true);
    this.tratamentosApi.obter(id).subscribe({
      next: (tratamento) => {
        this.aCarregar.set(false);
        if (tratamento.estado !== 'EmCurso') {
          this.paraDetalhe(ResultadoMedicacao.naoEditavel);
          return;
        }

        this.original.set(tratamento);
        this.formulario.reset({
          medicamento: tratamento.medicamento,
          dose: tratamento.dose,
          horarios: [...tratamento.horarios].sort(),
          dataInicio: tratamento.dataInicio,
          dataFim: tratamento.dataFim ?? '',
          grauImportancia: tratamento.grauImportancia,
          receita: null,
        });
        this.formulario.controls.dataInicio.disable();
      },
      error: (problema: ProblemaApi) => {
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  protected erroDe(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  /** Verificação prévia da receita (RNF-008); o ficheiro fica escolhido mesmo depois de uma falha. */
  protected receitaEscolhida(seletor: HTMLInputElement): void {
    const ficheiro = seletor.files?.item(0) ?? null;
    const controlo = this.formulario.controls.receita;
    controlo.setValue(ficheiro);
    controlo.markAsTouched();
    const regra = ficheiro ? validarDocumento(ficheiro) : null;
    if (regra) {
      controlo.setErrors({ formato: regra });
    }
  }

  /** OP-27 no registo e OP-30 na edição. Em erro, os dados e a receita escolhida mantêm-se (RF-053, RNF-028). */
  protected guardar(seletor: HTMLInputElement): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aGuardar()) {
      return;
    }

    const valores = this.formulario.getRawValue();
    const receita = valores.receita;
    const original = this.original();
    let pedido: Observable<TratamentoDto>;
    if (original) {
      const alteracoes = this.alteracoes(original);
      if (Object.keys(alteracoes).length === 0 && !receita) {
        this.erro.set('Não alterou nenhum dado do tratamento.');
        return;
      }

      pedido = this.tratamentosApi.atualizar(original.id, alteracoes, receita);
    } else {
      const dados: DadosTratamentoDto = {
        medicamento: valores.medicamento.trim(),
        dose: valores.dose.trim(),
        grauImportancia: valores.grauImportancia as GrauImportancia,
        dataInicio: valores.dataInicio,
        dataFim: valores.dataFim || null,
        horarios: valores.horarios,
      };
      pedido = this.tratamentosApi.registar(this.fichaId(), dados, receita);
    }

    this.aGuardar.set(true);
    pedido.subscribe({
      next: () => {
        this.aGuardar.set(false);
        seletor.value = '';
        if (original) {
          this.paraDetalhe(ResultadoMedicacao.atualizado);
        } else {
          void this.router.navigate(['/fichas', this.fichaId(), 'medicacao'], {
            queryParams: { resultado: ResultadoMedicacao.registado },
          });
        }
      },
      error: (problema: ProblemaApi) => {
        this.aGuardar.set(false);
        this.tratarErro(problema);
      },
    });
  }

  /** Campos de OP-30 que mudaram em relação ao tratamento carregado; a data de início nunca é enviada (RF-055). */
  private alteracoes(original: TratamentoDto): Partial<DadosTratamentoDto> {
    const valores = this.formulario.getRawValue();
    const alteracoes: Partial<DadosTratamentoDto> = {};
    if (valores.medicamento.trim() !== original.medicamento) {
      alteracoes.medicamento = valores.medicamento.trim();
    }

    if (valores.dose.trim() !== original.dose) {
      alteracoes.dose = valores.dose.trim();
    }

    if (valores.grauImportancia !== original.grauImportancia) {
      alteracoes.grauImportancia = valores.grauImportancia as GrauImportancia;
    }

    if ((valores.dataFim || null) !== original.dataFim) {
      alteracoes.dataFim = valores.dataFim || null;
    }

    if (valores.horarios.join(',') !== [...original.horarios].sort().join(',')) {
      alteracoes.horarios = valores.horarios;
    }

    return alteracoes;
  }

  /**
   * Data de fim opcional, não anterior à de início (RF-050). Na edição de um tratamento com data de fim, não pode ficar
   * vazia: o servidor (OP-30) mantém a data anterior quando o campo não é enviado.
   */
  private validarDataFim(controlo: AbstractControl): ValidationErrors | null {
    const fim = String(controlo.value ?? '');
    const formulario = this.formulario as FormGroup | undefined;
    if (!fim) {
      return this.original()?.dataFim
        ? { obrigatorio: 'Não pode ficar vazia num tratamento que já tem data de fim.' }
        : null;
    }

    const inicio = formulario?.controls['dataInicio']?.value as string | undefined;
    return inicio && fim < inicio ? { anterior: 'Não pode ser anterior à data de início.' } : null;
  }

  private tratarErro(problema: ProblemaApi): void {
    if (problema.codigo === 'TRATAMENTO_TERMINADO') {
      this.paraDetalhe(ResultadoMedicacao.naoEditavel);
      return;
    }

    const receita = this.formulario.controls.receita;
    if (ERROS_RECEITA.has(problema.codigo)) {
      V.erroNoCampo(receita, this.mensagens.mensagem(problema));
      return;
    }

    if (problema.codigo === 'DOCUMENTO_NAO_GUARDADO') {
      // Nada foi gravado; os dados e a receita escolhida mantêm-se para tentar de novo (RF-053, RNF-046).
      this.erro.set(
        this.edicao()
          ? 'A receita não foi guardada. O tratamento não foi alterado. Os dados mantêm-se: tente novamente.'
          : 'A receita não foi guardada. O tratamento não foi registado. Os dados mantêm-se: tente novamente.',
      );
      return;
    }

    // Os campos indexados dos horários e a parte da receita correspondem aos controlos `horarios` e `receita`.
    V.aplicarErrosDoServidor(this.formulario, {
      ...problema,
      campos: problema.campos?.map((c) => ({
        ...c,
        campo: c.campo.startsWith('horarios')
          ? 'horarios'
          : c.campo.startsWith('receita')
            ? 'receita'
            : c.campo,
      })),
    });
    this.erro.set(this.mensagens.mensagem(problema));
  }

  private paraDetalhe(resultado: string): void {
    void this.router.navigate(['/fichas', this.fichaId(), 'medicacao', this.tratamentoId()], {
      queryParams: { resultado },
      replaceUrl: resultado === ResultadoMedicacao.naoEditavel,
    });
  }
}
