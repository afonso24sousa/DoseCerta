import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FichasApi } from '../../api/fichas.api';
import { MensagensErroService } from '../../core/mensagens-erro.service';
import { EstadoFicha, FichaResumoDto } from '../../models/fichas.models';
import { ProblemaApi } from '../../models/problema-api';
import { BannerErroComponent } from '../../shared/banner-erro.component';
import { descricaoPapel } from './permissoes-ficha';
import { ResultadoFicha } from './resultados-ficha';

/** Filtro da lista: um estado ou as duas. */
type FiltroEstado = EstadoFicha | 'Todas';

/**
 * Lista das fichas do Utilizador (OP-07; RF-028, RF-120, RF-121), com o filtro Ativas, Arquivadas ou Todas, a indicação
 * da ficha própria, do papel do Utilizador e do estado de cada ficha, e o acesso à criação de uma ficha.
 */
@Component({
  selector: 'app-lista-fichas-page',
  standalone: true,
  imports: [RouterLink, BannerErroComponent],
  template: `
    <div class="titulo-pagina">
      <h1>Fichas</h1>
      <a class="botao" routerLink="/fichas/nova">Criar ficha</a>
    </div>

    @if (resultado() === eliminada) {
      <p class="banner banner--sucesso" role="status">Ficha eliminada.</p>
    }
    <app-banner-erro [mensagem]="erro()" />

    <section class="cartao cartao--largo">
      <fieldset class="filtro">
        <legend>Mostrar</legend>
        @for (opcao of filtros; track opcao.valor) {
          <label class="opcao">
            <input
              type="radio"
              name="filtro-estado"
              [value]="opcao.valor"
              [checked]="filtro() === opcao.valor"
              (change)="filtrar(opcao.valor)"
            />
            {{ opcao.rotulo }}
          </label>
        }
      </fieldset>

      @if (aCarregar()) {
        <p role="status">A carregar as fichas…</p>
      } @else if (fichas().length === 0 && !erro()) {
        <p>{{ semFichas() }}</p>
      } @else if (fichas().length > 0) {
        <table class="tabela">
          <caption class="so-leitor">
            Fichas
          </caption>
          <thead>
            <tr>
              <th scope="col">Nome</th>
              <th scope="col">Tipo</th>
              <th scope="col">O meu papel</th>
              <th scope="col">Estado</th>
            </tr>
          </thead>
          <tbody>
            @for (ficha of fichas(); track ficha.id) {
              <tr>
                <td>
                  <a [routerLink]="['/fichas', ficha.id]">{{ ficha.nomeCompleto }}</a>
                </td>
                <td>{{ tipo(ficha) }}</td>
                <td>{{ papel(ficha) }}</td>
                <td>
                  <span class="etiqueta" [class.etiqueta--arquivada]="ficha.estado === 'Arquivada'">{{
                    ficha.estado === 'Ativa' ? 'Ativa' : 'Arquivada'
                  }}</span>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class ListaFichasPage implements OnInit {
  private readonly fichasApi = inject(FichasApi);
  private readonly mensagens = inject(MensagensErroService);

  /** Parâmetro `resultado` da rota, depois de eliminar uma ficha. */
  readonly resultado = input<string | undefined>();

  protected readonly eliminada = ResultadoFicha.eliminada;
  protected readonly filtros: readonly { valor: FiltroEstado; rotulo: string }[] = [
    { valor: 'Ativa', rotulo: 'Ativas' },
    { valor: 'Arquivada', rotulo: 'Arquivadas' },
    { valor: 'Todas', rotulo: 'Todas' },
  ];

  protected readonly filtro = signal<FiltroEstado>('Ativa');
  protected readonly fichas = signal<FichaResumoDto[]>([]);
  protected readonly aCarregar = signal(true);
  protected readonly erro = signal<string | null>(null);

  ngOnInit(): void {
    this.carregar();
  }

  protected filtrar(filtro: FiltroEstado): void {
    this.filtro.set(filtro);
    this.carregar();
  }

  protected tipo(ficha: FichaResumoDto): string {
    if (!ficha.eProprio) {
      return 'De terceiro';
    }

    // O titular da ficha própria é sempre o seu Cuidador principal (RF-030, RF-034).
    return ficha.souPrincipal ? 'Ficha própria (a sua)' : 'Ficha própria do utente';
  }

  protected papel(ficha: FichaResumoDto): string {
    return descricaoPapel(ficha);
  }

  protected semFichas(): string {
    switch (this.filtro()) {
      case 'Ativa':
        return 'Não tem fichas ativas.';
      case 'Arquivada':
        return 'Não tem fichas arquivadas.';
      default:
        return 'Ainda não tem fichas. Crie a primeira com «Criar ficha».';
    }
  }

  private carregar(): void {
    const filtro = this.filtro();
    this.aCarregar.set(true);
    this.erro.set(null);
    this.fichasApi.listar(filtro === 'Todas' ? undefined : filtro).subscribe({
      next: (fichas) => {
        if (filtro !== this.filtro()) {
          return;
        }

        this.fichas.set([...fichas].sort((a, b) => a.nomeCompleto.localeCompare(b.nomeCompleto, 'pt')));
        this.aCarregar.set(false);
      },
      error: (problema: ProblemaApi) => {
        if (filtro !== this.filtro()) {
          return;
        }

        this.fichas.set([]);
        this.aCarregar.set(false);
        this.erro.set(this.mensagens.mensagem(problema));
      },
    });
  }
}
