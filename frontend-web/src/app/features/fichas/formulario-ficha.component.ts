import { Component, input, signal } from '@angular/core';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  CATEGORIAS_DOCUMENTO,
  CategoriaDocumento,
  DadosFichaDto,
  DocumentoSubmetido,
  FichaDto,
  ItemListaDto,
  ListasNecessidadesDto,
} from '../../models/fichas.models';
import { CampoFormularioComponent } from '../../shared/campo-formulario.component';
import {
  COMPRIMENTO_MAXIMO_NOME_FICHA,
  DATA_NASCIMENTO_MINIMA,
  ValidadoresFormulario as V,
} from '../../shared/validadores-formulario';
import { formatarTamanho } from './apresentacao-ficha';
import { ACEITAR_DOCUMENTOS, validarDocumento } from './validacao-documento';

/** Controlos de um documento a enviar com a ficha: o ficheiro escolhido e a categoria (RF-022). */
export type ControloDocumento = FormGroup<{
  ficheiro: FormControl<File>;
  categoria: FormControl<CategoriaDocumento | ''>;
}>;

/**
 * Controlos comuns à criação (OP-09) e à edição (OP-11) de uma ficha. Os nomes são os dos campos da API, para os erros
 * por campo do servidor ficarem junto do controlo certo (RNF-028), incluindo `documentos[i].categoria`.
 */
export interface ControlosDadosFicha {
  nomeCompleto: FormControl<string>;
  dataNascimento: FormControl<string>;
  contactoEmergencia: FormControl<string>;
  grauAutonomiaId: FormControl<number | null>;
  condicoesSaudeIds: FormControl<number[]>;
  alergiasIds: FormControl<number[]>;
  documentos: FormArray<ControloDocumento>;
}

/** Cria os controlos, vazios ou com os dados atuais da ficha (edição). */
export function criarControlosDadosFicha(ficha?: FichaDto): ControlosDadosFicha {
  return {
    nomeCompleto: new FormControl(ficha?.nomeCompleto ?? '', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.comprimentoMaximo(COMPRIMENTO_MAXIMO_NOME_FICHA)],
    }),
    dataNascimento: new FormControl(ficha?.dataNascimento ?? '', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.dataNascimento()],
    }),
    contactoEmergencia: new FormControl(ficha?.contactoEmergencia ?? '', {
      nonNullable: true,
      validators: [V.obrigatorio(), V.telefone()],
    }),
    grauAutonomiaId: new FormControl<number | null>(ficha?.grauAutonomia?.id ?? null),
    condicoesSaudeIds: new FormControl(ficha?.condicoesSaude.map((c) => c.id) ?? [], { nonNullable: true }),
    alergiasIds: new FormControl(ficha?.alergias.map((a) => a.id) ?? [], { nonNullable: true }),
    documentos: new FormArray<ControloDocumento>([]),
  };
}

/** Corpo de OP-11, e parte comum do corpo de OP-09, a partir dos controlos. */
export function dadosDosControlos(controlos: ControlosDadosFicha): DadosFichaDto {
  return {
    nomeCompleto: controlos.nomeCompleto.value.trim(),
    dataNascimento: controlos.dataNascimento.value.trim(),
    contactoEmergencia: controlos.contactoEmergencia.value.trim(),
    grauAutonomiaId: controlos.grauAutonomiaId.value,
    condicoesSaudeIds: controlos.condicoesSaudeIds.value,
    alergiasIds: controlos.alergiasIds.value,
  };
}

/** Documentos escolhidos, pela ordem em que aparecem no formulário (a ordem das partes `documento` e `categoria`). */
export function documentosDosControlos(controlos: ControlosDadosFicha): DocumentoSubmetido[] {
  return controlos.documentos.controls.map((documento) => ({
    ficheiro: documento.controls.ficheiro.value,
    categoria: documento.controls.categoria.value as CategoriaDocumento,
  }));
}

/** Lista fechada de escolha múltipla e o controlo com os identificadores escolhidos. */
interface GrupoOpcoes {
  chave: string;
  titulo: string;
  itens: ItemListaDto[];
  controlo: FormControl<number[]>;
}

let sequencia = 0;

/**
 * Campos da ficha comuns à criação e à edição: identificação obrigatória (RF-020), necessidades das listas fechadas de
 * OP-08 (RF-021) e documentos opcionais, cada um com a categoria (RF-022). Os ficheiros são verificados ao serem
 * escolhidos (RNF-008) e ficam guardados nos controlos: se o envio falhar, tudo se mantém para nova submissão (RF-023).
 */
@Component({
  selector: 'app-formulario-ficha',
  standalone: true,
  imports: [ReactiveFormsModule, CampoFormularioComponent],
  template: `
    @let c = controlos();

    <fieldset class="grupo">
      <legend>Identificação</legend>
      <app-campo-formulario rotulo="Nome completo" autocomplete="off" [controlo]="c.nomeCompleto" />
      <app-campo-formulario
        rotulo="Data de nascimento"
        tipo="date"
        [minimo]="dataMinima"
        [maximo]="hoje"
        [controlo]="c.dataNascimento"
      />
      <app-campo-formulario
        rotulo="Contacto de emergência"
        tipo="tel"
        autocomplete="off"
        [controlo]="c.contactoEmergencia"
      />
      <p class="ajuda">Por exemplo +351 910 000 001.</p>
    </fieldset>

    <fieldset class="grupo">
      <legend>Necessidades específicas</legend>
      @if (listas(); as l) {
        <div class="campo" [class.campo--erro]="erro(c.grauAutonomiaId)">
          <label [for]="id + '-grau'">Grau de autonomia e mobilidade</label>
          <select
            [id]="id + '-grau'"
            [formControl]="c.grauAutonomiaId"
            [attr.aria-invalid]="erro(c.grauAutonomiaId) ? 'true' : null"
            [attr.aria-describedby]="erro(c.grauAutonomiaId) ? id + '-grau-erro' : null"
          >
            <option [ngValue]="null">Sem indicação</option>
            @for (grau of l.grausAutonomia; track grau.id) {
              <option [ngValue]="grau.id">{{ grau.designacao }}</option>
            }
          </select>
          @if (erro(c.grauAutonomiaId); as mensagem) {
            <p class="campo__erro" [id]="id + '-grau-erro'">{{ mensagem }}</p>
          }
        </div>

        @for (grupo of grupos(l, c); track grupo.chave) {
          <fieldset
            class="grupo-opcoes"
            [class.campo--erro]="erro(grupo.controlo)"
            [attr.aria-describedby]="erro(grupo.controlo) ? id + '-' + grupo.chave + '-erro' : null"
          >
            <legend>{{ grupo.titulo }}</legend>
            @for (item of grupo.itens; track item.id) {
              <label class="opcao">
                <input
                  type="checkbox"
                  [checked]="grupo.controlo.value.includes(item.id)"
                  (change)="alternar(grupo.controlo, item.id, $event)"
                />
                {{ item.designacao }}
              </label>
            } @empty {
              <p>Sem valores disponíveis.</p>
            }
            @if (erro(grupo.controlo); as mensagem) {
              <p class="campo__erro" [id]="id + '-' + grupo.chave + '-erro'">{{ mensagem }}</p>
            }
          </fieldset>
        }
      } @else {
        <p>A carregar as listas de necessidades…</p>
      }
    </fieldset>

    <fieldset class="grupo">
      <legend>Documentos (opcional)</legend>
      <p class="ajuda">PDF, JPG ou PNG, com 10 MB no máximo. Escolha a categoria de cada documento.</p>

      @if (c.documentos.length > 0) {
        <ul class="lista-documentos">
          @for (documento of c.documentos.controls; track documento; let i = $index) {
            <li>
              <span class="lista-documentos__nome">
                {{ documento.controls.ficheiro.value.name }}
                ({{ tamanho(documento.controls.ficheiro.value.size) }})
              </span>
              <div class="campo" [class.campo--erro]="erro(documento.controls.categoria)">
                <label [for]="id + '-cat-' + i"
                  >Categoria de {{ documento.controls.ficheiro.value.name }}</label
                >
                <select
                  [id]="id + '-cat-' + i"
                  [formControl]="documento.controls.categoria"
                  [attr.aria-invalid]="erro(documento.controls.categoria) ? 'true' : null"
                  [attr.aria-describedby]="
                    erro(documento.controls.categoria) ? id + '-cat-' + i + '-erro' : null
                  "
                >
                  <option value="">Escolha a categoria</option>
                  @for (categoria of categorias; track categoria.valor) {
                    <option [value]="categoria.valor">{{ categoria.rotulo }}</option>
                  }
                </select>
                @if (erro(documento.controls.categoria); as mensagem) {
                  <p class="campo__erro" [id]="id + '-cat-' + i + '-erro'">{{ mensagem }}</p>
                }
              </div>
              @if (erro(documento.controls.ficheiro); as mensagem) {
                <p class="campo__erro" role="alert">{{ mensagem }}</p>
              }
              <button type="button" class="botao botao--secundario" (click)="remover(i)">
                Retirar {{ documento.controls.ficheiro.value.name }}
              </button>
            </li>
          }
        </ul>
      }

      <div class="campo" [class.campo--erro]="ficheiroRecusado()">
        <label [for]="id + '-ficheiro'">Adicionar documento</label>
        <input
          #seletor
          type="file"
          [id]="id + '-ficheiro'"
          [accept]="aceitar"
          multiple
          [attr.aria-invalid]="ficheiroRecusado() ? 'true' : null"
          [attr.aria-describedby]="ficheiroRecusado() ? id + '-ficheiro-erro' : null"
          (change)="adicionar(seletor)"
        />
        @if (ficheiroRecusado(); as mensagem) {
          <p class="campo__erro" [id]="id + '-ficheiro-erro'" role="alert">{{ mensagem }}</p>
        }
      </div>
    </fieldset>
  `,
})
export class FormularioFichaComponent {
  /** Controlos da ficha, criados com {@link criarControlosDadosFicha}. */
  readonly controlos = input.required<ControlosDadosFicha>();

  /** Listas fechadas de OP-08, ou `null` enquanto não chegam. */
  readonly listas = input<ListasNecessidadesDto | null>(null);

  protected readonly id = `ficha-${++sequencia}`;
  protected readonly categorias = CATEGORIAS_DOCUMENTO;
  protected readonly aceitar = ACEITAR_DOCUMENTOS;
  protected readonly dataMinima = DATA_NASCIMENTO_MINIMA;
  protected readonly hoje = V.hoje();
  protected readonly tamanho = formatarTamanho;

  /** Regras violadas pelos últimos ficheiros escolhidos e recusados antes do envio (RNF-008). */
  protected readonly ficheiroRecusado = signal<string | null>(null);

  protected erro(controlo: AbstractControl): string | null {
    return V.mensagemErro(controlo);
  }

  /** Listas de escolha múltipla: condições de saúde e alergias (RF-021). */
  protected grupos(listas: ListasNecessidadesDto, controlos: ControlosDadosFicha): GrupoOpcoes[] {
    return [
      {
        chave: 'condicoes',
        titulo: 'Condições de saúde',
        itens: listas.condicoesSaude,
        controlo: controlos.condicoesSaudeIds,
      },
      { chave: 'alergias', titulo: 'Alergias', itens: listas.alergias, controlo: controlos.alergiasIds },
    ];
  }

  protected alternar(controlo: FormControl<number[]>, id: number, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;
    const atual = controlo.value.filter((valor) => valor !== id);
    controlo.setValue(marcado ? [...atual, id] : atual);
    controlo.markAsTouched();
  }

  /** Acrescenta os ficheiros válidos escolhidos e indica os recusados, sem os enviar. */
  protected adicionar(seletor: HTMLInputElement): void {
    const recusados: string[] = [];
    for (const ficheiro of Array.from(seletor.files ?? [])) {
      const regra = validarDocumento(ficheiro);
      if (regra) {
        recusados.push(regra);
        continue;
      }

      this.controlos().documentos.push(
        new FormGroup({
          ficheiro: new FormControl(ficheiro, { nonNullable: true }),
          categoria: new FormControl<CategoriaDocumento | ''>('', {
            nonNullable: true,
            validators: [
              (controlo) => (controlo.value ? null : { categoria: 'Escolha a categoria do documento.' }),
            ],
          }),
        }),
      );
    }

    this.ficheiroRecusado.set(recusados.length > 0 ? recusados.join(' ') : null);
    // Limpa a escolha para o mesmo ficheiro poder ser escolhido de novo; os ficheiros aceites ficam nos controlos.
    seletor.value = '';
  }

  protected remover(indice: number): void {
    this.controlos().documentos.removeAt(indice);
  }
}
