import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ContaApi } from '../../api/conta.api';
import { AuthService } from '../../core/auth.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { COMPRIMENTO_MAXIMO_NOME, ValidadoresFormulario as V } from '../../shared/validadores-formulario';

/**
 * E03 Conta (RF-013 a RF-016): consulta (OP-04) e alteração do nome e do e-mail (OP-05), alteração da palavra-passe
 * (OP-06) e terminar sessão (OP-03). Os erros aparecem junto ao campo e os valores escritos mantêm-se (RNF-028).
 */
@Component({
  selector: 'app-conta-page',
  standalone: true,
  imports: [ReactiveFormsModule, CampoFormularioComponent, BannerErroComponent],
  template: `
    <h1>A minha conta</h1>
    <app-banner-erro [mensagem]="erroCarregar()" />

    <section class="cartao">
      <h2>Dados da conta</h2>
      <app-banner-erro [mensagem]="erroDados()" />
      @if (dadosGuardados()) {
        <p class="banner banner--sucesso" role="status">Dados da conta guardados.</p>
      }
      <form [formGroup]="dados" (ngSubmit)="guardarDados()" novalidate>
        <app-campo-formulario rotulo="Nome" autocomplete="name" [controlo]="dados.controls.nome" />
        <app-campo-formulario
          rotulo="E-mail"
          tipo="email"
          autocomplete="email"
          [controlo]="dados.controls.email"
        />
        <button type="submit" class="botao" [disabled]="aGuardarDados()">Guardar dados</button>
      </form>
    </section>

    <section class="cartao">
      <h2>Alterar palavra-passe</h2>
      <app-banner-erro [mensagem]="erroPalavraPasse()" />
      @if (palavraPasseAlterada()) {
        <p class="banner banner--sucesso" role="status">Palavra-passe alterada.</p>
      }
      <form [formGroup]="palavraPasse" (ngSubmit)="alterarPalavraPasse()" novalidate>
        <app-campo-formulario
          rotulo="Palavra-passe atual"
          tipo="password"
          autocomplete="current-password"
          [controlo]="palavraPasse.controls.palavraPasseAtual"
        />
        <app-campo-formulario
          rotulo="Nova palavra-passe"
          tipo="password"
          autocomplete="new-password"
          [controlo]="palavraPasse.controls.palavraPasseNova"
        />
        <p class="ajuda">Pelo menos 8 caracteres, com uma letra e um algarismo.</p>
        <button type="submit" class="botao" [disabled]="aAlterarPalavraPasse()">Alterar palavra-passe</button>
      </form>
    </section>

    <section class="cartao">
      <h2>Sessão</h2>
      <button type="button" class="botao botao--secundario" (click)="terminarSessao()">
        Terminar sessão
      </button>
    </section>
  `,
})
export class ContaPage implements OnInit {
  private readonly contaApi = inject(ContaApi);
  private readonly auth = inject(AuthService);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly dados = new FormGroup({
    nome: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_NOME)],
    }),
    email: new FormControl('', { nonNullable: true, validators: [V.obrigatorio(), V.email()] }),
  });

  protected readonly palavraPasse = new FormGroup({
    palavraPasseAtual: new FormControl('', { nonNullable: true, validators: [V.obrigatorio()] }),
    palavraPasseNova: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.palavraPasse()],
    }),
  });

  protected readonly erroCarregar = signal<string | null>(null);
  protected readonly erroDados = signal<string | null>(null);
  protected readonly erroPalavraPasse = signal<string | null>(null);
  protected readonly aGuardarDados = signal(false);
  protected readonly aAlterarPalavraPasse = signal(false);
  protected readonly dadosGuardados = signal(false);
  protected readonly palavraPasseAlterada = signal(false);

  ngOnInit(): void {
    this.contaApi.consultar().subscribe({
      next: (conta) => {
        this.dados.setValue({ nome: conta.nome, email: conta.email });
        this.auth.utilizador.set(conta);
      },
      error: (problema: ProblemaApi) => this.erroCarregar.set(this.mensagens.mensagem(problema)),
    });
  }

  protected guardarDados(): void {
    this.erroDados.set(null);
    this.dadosGuardados.set(false);
    this.dados.markAllAsTouched();
    if (this.dados.invalid || this.aGuardarDados()) {
      return;
    }

    this.aGuardarDados.set(true);
    this.contaApi.atualizar(this.dados.getRawValue()).subscribe({
      next: (conta) => {
        this.aGuardarDados.set(false);
        this.auth.utilizador.set(conta);
        this.dados.setValue({ nome: conta.nome, email: conta.email });
        this.dadosGuardados.set(true);
      },
      error: (problema: ProblemaApi) => {
        this.aGuardarDados.set(false);
        if (problema.codigo === 'EMAIL_JA_REGISTADO') {
          V.erroNoCampo(
            this.dados.controls.email,
            this.mensagens.mensagem(problema, { EMAIL_JA_REGISTADO: 'Este e-mail pertence a outra conta.' }),
          );
        } else if (!V.aplicarErrosDoServidor(this.dados, problema)) {
          this.erroDados.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }

  protected alterarPalavraPasse(): void {
    this.erroPalavraPasse.set(null);
    this.palavraPasseAlterada.set(false);
    this.palavraPasse.markAllAsTouched();
    if (this.palavraPasse.invalid || this.aAlterarPalavraPasse()) {
      return;
    }

    this.aAlterarPalavraPasse.set(true);
    this.contaApi.alterarPalavraPasse(this.palavraPasse.getRawValue()).subscribe({
      next: () => {
        this.aAlterarPalavraPasse.set(false);
        this.palavraPasse.reset();
        this.palavraPasseAlterada.set(true);
      },
      error: (problema: ProblemaApi) => {
        this.aAlterarPalavraPasse.set(false);
        if (problema.codigo === 'PALAVRA_PASSE_ATUAL_INCORRETA') {
          V.erroNoCampo(this.palavraPasse.controls.palavraPasseAtual, this.mensagens.mensagem(problema));
        } else if (!V.aplicarErrosDoServidor(this.palavraPasse, problema)) {
          this.erroPalavraPasse.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }

  protected terminarSessao(): void {
    this.auth.terminarSessao().subscribe();
  }
}
