import { Component, OnChanges, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ConvitesApi } from '../../api/convites.api';
import { FichaContextoService } from '../../core/ficha-contexto.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { FichaDto, PapelFicha } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import { ResultadoPessoas } from './resultados-pessoas';

/**
 * E13 Convidar pessoa (OP-19; UC-13): o Cuidador indica o e-mail e o papel oferecido, Cuidador ou Familiar (RF-040), e
 * pode oferecer o papel de Cuidador principal (só com o papel Cuidador e nunca na ficha própria, RF-025, RF-027,
 * RF-034). O e-mail tem validação prévia; o servidor valida sempre. Um convite pendente repetido (RF-043) ou uma pessoa
 * já associada aparecem junto do e-mail, sem apagar o que foi escrito (RNF-028). A rota exige o papel Cuidador e a
 * ficha ativa (`cuidadorGuard`, `fichaAtivaGuard`).
 */
@Component({
  selector: 'app-convidar-pessoa-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BannerErroComponent, CampoFormularioComponent],
  template: `
    <p><a [routerLink]="['/fichas', fichaId(), 'pessoas']">Voltar às pessoas da ficha</a></p>

    <h1>Convidar pessoa</h1>
    @if (ficha(); as f) {
      <p>{{ f.nomeCompleto }}</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <form class="cartao" [formGroup]="formulario" (ngSubmit)="convidar()" novalidate>
      <app-campo-formulario
        rotulo="E-mail da pessoa"
        tipo="email"
        autocomplete="off"
        [controlo]="formulario.controls.email"
      />

      <fieldset
        class="grupo grupo-opcoes"
        [class.campo--erro]="erroDe(formulario.controls.papel)"
        [attr.aria-describedby]="erroDe(formulario.controls.papel) ? 'convidar-papel-erro' : null"
      >
        <legend>Papel oferecido</legend>
        <label class="opcao">
          <input type="radio" [formControl]="formulario.controls.papel" value="Cuidador" />
          Cuidador
        </label>
        <label class="opcao">
          <input type="radio" [formControl]="formulario.controls.papel" value="Familiar" />
          Familiar
        </label>
        @if (erroDe(formulario.controls.papel); as mensagem) {
          <p class="campo__erro" id="convidar-papel-erro">{{ mensagem }}</p>
        }
      </fieldset>

      @if (ficha() && !ficha()?.eProprio) {
        <label class="opcao">
          <input type="checkbox" [formControl]="formulario.controls.paraPrincipal" />
          Oferecer o papel de Cuidador principal
        </label>
        <p class="ajuda">
          Até à resposta, o Cuidador principal atual fica provisório. Se o convite for aceite, o papel passa
          para a pessoa convidada e o Cuidador principal atual deixa de estar associado à ficha; se for
          recusado, mantém-se.
        </p>
        @if (formulario.controls.papel.value !== 'Cuidador') {
          <p class="ajuda">O papel de Cuidador principal só pode ser oferecido com o papel Cuidador.</p>
        }
      }

      <p class="ajuda">
        O convite é enviado por e-mail. Se o envio falhar, é reenviado até 3 vezes e o convite mantém-se na
        área de convites.
      </p>

      <div class="acoes">
        <button type="submit" class="botao" [disabled]="aEnviar()">
          {{ aEnviar() ? 'A enviar…' : 'Enviar convite' }}
        </button>
        <a class="botao botao--secundario" [routerLink]="['/fichas', fichaId(), 'pessoas']">Cancelar</a>
      </div>
    </form>
  `,
})
export class ConvidarPessoaPage implements OnChanges {
  private readonly convitesApi = inject(ConvitesApi);
  private readonly contexto = inject(FichaContextoService);
  private readonly mensagens = inject(MensagensErroService);
  private readonly router = inject(Router);

  /** Parâmetro `fichaId` da rota. */
  readonly fichaId = input.required<string>();

  protected readonly ficha = signal<FichaDto | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly aEnviar = signal(false);

  protected readonly formulario = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.email()],
    }),
    papel: new FormControl<PapelFicha | ''>('', {
      nonNullable: true,
      validators: [(controlo) => (controlo.value ? null : { papel: 'Escolha o papel oferecido.' })],
    }),
    paraPrincipal: new FormControl(false, { nonNullable: true }),
  });

  constructor() {
    // O papel de Cuidador principal só existe com o papel Cuidador (OP-19).
    const paraPrincipal = this.formulario.controls.paraPrincipal;
    this.formulario.controls.papel.valueChanges.pipe(takeUntilDestroyed()).subscribe((papel) => {
      if (papel === 'Cuidador') {
        paraPrincipal.enable({ emitEvent: false });
      } else {
        paraPrincipal.setValue(false, { emitEvent: false });
        paraPrincipal.disable({ emitEvent: false });
      }
    });
    paraPrincipal.disable({ emitEvent: false });
  }

  ngOnChanges(): void {
    const atual = this.contexto.ficha();
    this.ficha.set(atual?.id === this.fichaId() ? atual : null);
    this.contexto.obter(this.fichaId()).subscribe({
      next: (ficha) => this.ficha.set(ficha),
      error: (problema: ProblemaApi) => this.erro.set(this.mensagens.mensagem(problema)),
    });
  }

  protected erroDe(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  /** OP-19; com sucesso volta a E12 com a mensagem de convite enviado. Em erro, o que foi escrito mantém-se. */
  protected convidar(): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    const { email, papel, paraPrincipal } = this.formulario.getRawValue();
    if (this.formulario.invalid || !papel || this.aEnviar()) {
      return;
    }

    const oferecerPrincipal = papel === 'Cuidador' && paraPrincipal && !this.ficha()?.eProprio;
    this.aEnviar.set(true);
    this.convitesApi
      .convidar(this.fichaId(), { email: email.trim(), papel, paraPrincipal: oferecerPrincipal })
      .subscribe({
        next: () => {
          this.aEnviar.set(false);
          void this.router.navigate(['/fichas', this.fichaId(), 'pessoas'], {
            queryParams: { resultado: ResultadoPessoas.conviteEnviado },
          });
        },
        error: (problema: ProblemaApi) => {
          this.aEnviar.set(false);
          this.tratarErro(problema, oferecerPrincipal);
        },
      });
  }

  private tratarErro(problema: ProblemaApi, oferecerPrincipal: boolean): void {
    const email = this.formulario.controls.email;
    if (problema.codigo === 'CONVITE_PENDENTE_EXISTE' && oferecerPrincipal) {
      // Com paraPrincipal, o conflito pode ser outro convite para Cuidador principal, para qualquer e-mail (OP-19).
      this.erro.set(
        'Já existe um convite pendente para este e-mail nesta ficha, ou um convite para Cuidador principal ainda ' +
          'sem resposta. Só pode haver um convite para Cuidador principal de cada vez.',
      );
    } else if (problema.codigo === 'CONVITE_PENDENTE_EXISTE' || problema.codigo === 'PESSOA_JA_ASSOCIADA') {
      V.erroNoCampo(email, this.mensagens.mensagem(problema));
    } else {
      // Erros por campo (VALIDACAO_INVALIDA) junto de cada campo; a mensagem geral explica o resto.
      V.aplicarErrosDoServidor(this.formulario, problema);
      this.erro.set(this.mensagens.mensagem(problema));
    }
  }
}
