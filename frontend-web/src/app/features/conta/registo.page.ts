import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { COMPRIMENTO_MAXIMO_NOME, ValidadoresFormulario as V } from '../../shared/validadores-formulario';

/**
 * E01 Registo (OP-01; RF-010, RF-011, RNF-003). Depois de criar a conta vai para o início de sessão, com o número de
 * convites pendentes para o e-mail (RF-044). O e-mail já registado aparece junto ao campo e-mail.
 */
@Component({
  selector: 'app-registo-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CampoFormularioComponent, BannerErroComponent],
  template: `
    <section class="cartao">
      <h1>Criar conta</h1>
      <app-banner-erro [mensagem]="erro()" />

      <form [formGroup]="formulario" (ngSubmit)="registar()" novalidate>
        <app-campo-formulario rotulo="Nome" autocomplete="name" [controlo]="formulario.controls.nome" />
        <app-campo-formulario
          rotulo="E-mail"
          tipo="email"
          autocomplete="email"
          [controlo]="formulario.controls.email"
        />
        <app-campo-formulario
          rotulo="Palavra-passe"
          tipo="password"
          autocomplete="new-password"
          [controlo]="formulario.controls.palavraPasse"
        />
        <p class="ajuda">Pelo menos 8 caracteres, com uma letra e um algarismo.</p>
        <button type="submit" class="botao" [disabled]="aEnviar()">Criar conta</button>
      </form>

      <p><a routerLink="/entrar">Já tenho conta</a></p>
    </section>
  `,
})
export class RegistoPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly formulario = new FormGroup({
    nome: new FormControl('', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_NOME)],
    }),
    email: new FormControl('', { nonNullable: true, validators: [V.obrigatorio(), V.email()] }),
    palavraPasse: new FormControl('', { nonNullable: true, validators: [V.obrigatorio(), V.palavraPasse()] }),
  });

  protected readonly aEnviar = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected registar(): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aEnviar()) {
      return;
    }

    this.aEnviar.set(true);
    this.auth.registar(this.formulario.getRawValue()).subscribe({
      next: (conta) =>
        void this.router.navigate(['/entrar'], {
          queryParams: { registo: 'ok', convites: conta.convitesPendentes },
        }),
      error: (problema: ProblemaApi) => {
        this.aEnviar.set(false);
        if (problema.codigo === 'EMAIL_JA_REGISTADO') {
          V.erroNoCampo(this.formulario.controls.email, this.mensagens.mensagem(problema));
        } else if (!V.aplicarErrosDoServidor(this.formulario, problema)) {
          this.erro.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }
}
