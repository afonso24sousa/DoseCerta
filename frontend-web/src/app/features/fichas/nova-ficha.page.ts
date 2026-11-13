import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DominiosApi } from '../../api/dominios.api';
import { FichasApi } from '../../api/fichas.api';
import { AuthService } from '../../core/auth.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ListasNecessidadesDto, NovaFichaDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ValidadoresFormulario as V } from '../../shared/validadores-formulario';
import {
  FormularioFichaComponent,
  criarControlosDadosFicha,
  dadosDosControlos,
  documentosDosControlos,
} from './formulario-ficha.component';
import { ResultadoFicha } from './resultados-ficha';

/** Exige que uma das opções de um grupo de botões de opção esteja escolhida. */
function escolhido(mensagem: string) {
  return (controlo: AbstractControl): ValidationErrors | null =>
    controlo.value === null || controlo.value === undefined ? { escolha: mensagem } : null;
}

/**
 * Criação de uma ficha (OP-08 e OP-09; UC-05, UC-06): ficha própria (RF-030, RF-031) ou de terceiro, com o Cuidador
 * principal a ser o próprio Utilizador (RF-024) ou outra pessoa convidada pelo e-mail (RF-025). A ficha própria fica
 * indisponível, com a explicação, se o Utilizador já tiver uma. Em qualquer falha, incluindo `503`
 * `DOCUMENTO_NAO_GUARDADO`, os dados e os ficheiros escolhidos mantêm-se para nova submissão (RF-023).
 */
@Component({
  selector: 'app-nova-ficha-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CampoFormularioComponent,
    BannerErroComponent,
    FormularioFichaComponent,
  ],
  template: `
    <p><a routerLink="/fichas">Voltar às fichas</a></p>
    <h1>Criar ficha</h1>
    <app-banner-erro [mensagem]="erroCarregar()" />

    <section class="cartao cartao--largo">
      <app-banner-erro [mensagem]="erro()" />

      <form [formGroup]="formulario" (ngSubmit)="criar()" novalidate>
        <fieldset
          class="grupo grupo-opcoes"
          [class.campo--erro]="erroDe(formulario.controls.propria)"
          [attr.aria-describedby]="erroDe(formulario.controls.propria) ? 'nova-ficha-propria-erro' : null"
        >
          <legend>Tipo de ficha</legend>
          <label class="opcao">
            <input
              type="radio"
              [formControl]="formulario.controls.propria"
              [value]="true"
              [attr.disabled]="jaTemFichaPropria() ? '' : null"
              [attr.aria-describedby]="jaTemFichaPropria() ? 'nova-ficha-propria-ajuda' : null"
            />
            Ficha própria: os meus dados, para gerir a minha medicação
          </label>
          @if (jaTemFichaPropria()) {
            <p class="ajuda" id="nova-ficha-propria-ajuda">
              Já tem uma ficha própria e só pode ter uma; pode criar fichas de terceiros.
            </p>
          }
          <label class="opcao">
            <input type="radio" [formControl]="formulario.controls.propria" [value]="false" />
            Ficha de terceiro: uma pessoa de quem cuido
          </label>
          @if (erroDe(formulario.controls.propria); as mensagem) {
            <p class="campo__erro" id="nova-ficha-propria-erro">{{ mensagem }}</p>
          }
        </fieldset>

        @if (formulario.controls.propria.value === false) {
          <fieldset
            class="grupo grupo-opcoes"
            [class.campo--erro]="erroDe(cuidador)"
            [attr.aria-describedby]="erroDe(cuidador) ? 'nova-ficha-cuidador-erro' : null"
          >
            <legend>Cuidador principal</legend>
            <label class="opcao">
              <input type="radio" [formControl]="cuidador.controls.eu" [value]="true" />
              Eu sou o Cuidador principal
            </label>
            <label class="opcao">
              <input type="radio" [formControl]="cuidador.controls.eu" [value]="false" />
              Convidar outra pessoa para Cuidador principal
            </label>
            @if (cuidador.controls.eu.value === false) {
              <app-campo-formulario
                rotulo="E-mail da pessoa a convidar"
                tipo="email"
                autocomplete="off"
                [controlo]="cuidador.controls.email"
              />
              <p class="ajuda">
                Fica como Cuidador principal provisório até a pessoa responder. Se aceitar, passa a ser ela o
                Cuidador principal e deixa de estar associado à ficha; se recusar, mantém-se.
              </p>
            }
            @if (erroDe(cuidador.controls.eu) ?? erroDe(cuidador); as mensagem) {
              <p class="campo__erro" id="nova-ficha-cuidador-erro">{{ mensagem }}</p>
            }
          </fieldset>
        }

        <app-formulario-ficha [controlos]="controlos" [listas]="listas()" />

        <div class="acoes">
          <button type="submit" class="botao" [disabled]="aEnviar()">
            {{ aEnviar() ? 'A criar…' : 'Criar ficha' }}
          </button>
          <a class="botao botao--secundario" routerLink="/fichas">Cancelar</a>
        </div>
      </form>
    </section>
  `,
})
export class NovaFichaPage implements OnInit {
  private readonly fichasApi = inject(FichasApi);
  private readonly dominiosApi = inject(DominiosApi);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly controlos = criarControlosDadosFicha();

  protected readonly cuidador = new FormGroup({
    eu: new FormControl<boolean | null>(null, {
      validators: [escolhido('Escolha quem é o Cuidador principal.')],
    }),
    email: new FormControl('', { nonNullable: true }),
  });

  /** Nomes iguais aos campos de OP-09, para os erros do servidor ficarem junto do controlo certo (RNF-028). */
  protected readonly formulario = new FormGroup({
    propria: new FormControl<boolean | null>(null, {
      validators: [escolhido('Escolha se é a sua ficha própria ou a ficha de um terceiro.')],
    }),
    cuidadorPrincipal: this.cuidador,
    ...this.controlos,
  });

  protected readonly listas = signal<ListasNecessidadesDto | null>(null);
  protected readonly jaTemFichaPropria = signal(false);
  protected readonly aEnviar = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly erroCarregar = signal<string | null>(null);

  constructor() {
    this.formulario.controls.propria.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.tipoAlterado());
    this.cuidador.controls.eu.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.regrasEmail());
  }

  ngOnInit(): void {
    this.regrasEmail();

    this.dominiosApi.necessidades().subscribe({
      next: (listas) => this.listas.set(listas),
      error: (problema: ProblemaApi) => this.erroCarregar.set(this.mensagens.mensagem(problema)),
    });

    // A ficha própria do Utilizador é a ficha própria de que é o Cuidador principal (o titular, RF-030, RF-034).
    this.fichasApi.listar().subscribe({
      next: (fichas) => {
        if (fichas.some((ficha) => ficha.eProprio && ficha.souPrincipal)) {
          this.marcarFichaPropriaExistente();
        }
      },
      // Sem a lista, a opção fica disponível e o servidor recusa uma segunda ficha própria (409).
      error: () => undefined,
    });
  }

  protected erroDe(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  /** Na ficha própria, propõe o nome da conta, se o nome ainda estiver por preencher. */
  private tipoAlterado(): void {
    const nome = this.auth.utilizador()?.nome;
    if (
      this.formulario.controls.propria.value === true &&
      nome &&
      !this.controlos.nomeCompleto.value.trim()
    ) {
      this.controlos.nomeCompleto.setValue(nome);
    }

    this.regrasEmail();
  }

  /** O Cuidador principal só é pedido numa ficha de terceiro e o e-mail só quando se convida outra pessoa. */
  private regrasEmail(): void {
    const terceiro = this.formulario.controls.propria.value === false;
    if (terceiro) {
      this.cuidador.enable({ emitEvent: false });
    } else {
      this.cuidador.disable({ emitEvent: false });
    }

    const email = this.cuidador.controls.email;
    if (terceiro && this.cuidador.controls.eu.value === false) {
      email.enable({ emitEvent: false });
      email.setValidators([V.obrigatorio(), V.email()]);
    } else {
      email.disable({ emitEvent: false });
      email.clearValidators();
    }

    email.updateValueAndValidity({ emitEvent: false });
  }

  protected criar(): void {
    this.erro.set(null);
    this.regrasEmail();
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aEnviar()) {
      return;
    }

    const propria = this.formulario.controls.propria.value === true;
    const convidar = !propria && this.cuidador.controls.eu.value === false;
    const pedido: NovaFichaDto = {
      propria,
      ...dadosDosControlos(this.controlos),
      ...(propria
        ? {}
        : {
            cuidadorPrincipal: convidar
              ? { eu: false, email: this.cuidador.controls.email.value.trim() }
              : { eu: true },
          }),
    };

    this.aEnviar.set(true);
    this.fichasApi.criar(pedido, documentosDosControlos(this.controlos)).subscribe({
      next: (ficha) =>
        void this.router.navigate(['/fichas', ficha.id], {
          queryParams: { resultado: convidar ? ResultadoFicha.criadaComConvite : ResultadoFicha.criada },
        }),
      error: (problema: ProblemaApi) => {
        // Nada é limpo: os dados e os ficheiros escolhidos ficam para nova submissão (RF-023).
        this.aEnviar.set(false);
        if (problema.codigo === 'FICHA_PROPRIA_JA_EXISTE') {
          this.marcarFichaPropriaExistente();
          // Só a mensagem do servidor, que substitui o «escolha uma opção» do tipo agora por escolher.
          this.formulario.controls.propria.setErrors({ servidor: this.mensagens.mensagem(problema) });
          this.formulario.controls.propria.markAsTouched();
          return;
        }

        V.aplicarErrosDoServidor(this.formulario, problema);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }

  /** Torna a ficha própria indisponível (RF-031) e, se estava escolhida, deixa o tipo por escolher. */
  private marcarFichaPropriaExistente(): void {
    this.jaTemFichaPropria.set(true);
    if (this.formulario.controls.propria.value === true) {
      this.formulario.controls.propria.setValue(null);
      this.regrasEmail();
    }
  }
}
