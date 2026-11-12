import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { AVISO_SESSAO_TERMINADA, AuthService } from '../../core/auth.service';
import { ROTA_INICIAL } from '../../core/guardas';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import { ValidadoresFormulario } from '../../shared/validadores-formulario';

/**
 * E02 Início de sessão (OP-02; RF-012, RNF-013). As credenciais inválidas dão uma mensagem única, que não diz se o
 * e-mail ou a palavra-passe estão errados. Mostra o aviso de sessão terminada (RF-017) e o resultado do registo
 * (RF-044). É a única página carregada no pacote inicial (RNF-019).
 */
@Component({
  selector: 'app-inicio-sessao-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CampoFormularioComponent, BannerErroComponent],
  template: `
    <section class="cartao">
      <h1>Iniciar sessão</h1>

      @if (aviso().sessaoTerminada) {
        <p class="banner banner--aviso" role="status">A sessão terminou. Inicie sessão de novo.</p>
      }
      @if (aviso().contaCriada) {
        <p class="banner banner--sucesso" role="status">
          Conta criada. Inicie sessão para continuar.
          @if (aviso().convites > 0) {
            Tem {{ aviso().convites }}
            {{ aviso().convites === 1 ? 'convite pendente' : 'convites pendentes' }} para ver em Convites.
          }
        </p>
      }
      <app-banner-erro [mensagem]="erro()" />

      <form [formGroup]="formulario" (ngSubmit)="entrar()" novalidate>
        <app-campo-formulario
          rotulo="E-mail"
          tipo="email"
          autocomplete="email"
          [controlo]="formulario.controls.email"
        />
        <app-campo-formulario
          rotulo="Palavra-passe"
          tipo="password"
          autocomplete="current-password"
          [controlo]="formulario.controls.palavraPasse"
        />
        <button type="submit" class="botao" [disabled]="aEnviar()">Entrar</button>
      </form>

      <p>Ainda não tem conta? <a routerLink="/registo">Criar conta</a></p>
    </section>
  `,
})
export class InicioSessaoPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MensagensErroService);

  protected readonly formulario = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [ValidadoresFormulario.obrigatorio()] }),
    palavraPasse: new FormControl('', {
      nonNullable: true,
      validators: [ValidadoresFormulario.obrigatorio()],
    }),
  });

  protected readonly aEnviar = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly aviso = toSignal(
    inject(ActivatedRoute).queryParamMap.pipe(
      map((parametros) => ({
        sessaoTerminada: parametros.get('sessao') === AVISO_SESSAO_TERMINADA,
        contaCriada: parametros.get('registo') === 'ok',
        convites: Number(parametros.get('convites') ?? 0) || 0,
      })),
    ),
    { initialValue: { sessaoTerminada: false, contaCriada: false, convites: 0 } },
  );

  protected entrar(): void {
    this.erro.set(null);
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.aEnviar()) {
      return;
    }

    this.aEnviar.set(true);
    this.auth.iniciarSessao(this.formulario.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(ROTA_INICIAL),
      error: (problema: ProblemaApi) => {
        this.aEnviar.set(false);
        if (!ValidadoresFormulario.aplicarErrosDoServidor(this.formulario, problema)) {
          this.erro.set(this.mensagens.mensagem(problema));
        }
      },
    });
  }
}
