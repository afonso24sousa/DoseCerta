/** Estado de uma ficha (RF-120, RF-121). */
export type EstadoFicha = 'Ativa' | 'Arquivada';

/** Papel do Utilizador numa ficha; o Cuidador principal é um Cuidador com `souPrincipal` verdadeiro. */
export type PapelFicha = 'Cuidador' | 'Familiar';

/** Categoria de um documento da ficha (RF-022). */
export type CategoriaDocumento = 'ReceitaMedica' | 'RelatorioClinico' | 'DocumentoIdentificacao';

/** Formato de um documento, detetado pelo servidor a partir do conteúdo (RNF-008). */
export type FormatoDocumento = 'PDF' | 'JPG' | 'PNG';

/** Categorias de documento com o texto a mostrar, pela ordem da documentação da API. */
export const CATEGORIAS_DOCUMENTO: readonly { valor: CategoriaDocumento; rotulo: string }[] = [
  { valor: 'ReceitaMedica', rotulo: 'Receita médica' },
  { valor: 'RelatorioClinico', rotulo: 'Relatório clínico' },
  { valor: 'DocumentoIdentificacao', rotulo: 'Documento de identificação' },
];

/** Resposta de OP-07 Listar fichas do Utilizador: o resumo de cada ficha (RF-028). */
export interface FichaResumoDto {
  id: string;
  nomeCompleto: string;
  eProprio: boolean;
  estado: EstadoFicha;
  meuPapel: PapelFicha;
  souPrincipal: boolean;
}

/** Valor de uma lista fechada de OP-08 (RF-021). */
export interface ItemListaDto {
  id: number;
  designacao: string;
}

/** Resposta de OP-08 Obter as listas de necessidades (RF-021). */
export interface ListasNecessidadesDto {
  condicoesSaude: ItemListaDto[];
  alergias: ItemListaDto[];
  grausAutonomia: ItemListaDto[];
}

/** Resposta de OP-09, OP-10 e OP-11: os dados de identificação e as necessidades da ficha. */
export interface FichaDto {
  id: string;
  nomeCompleto: string;
  dataNascimento: string;
  contactoEmergencia: string;
  eProprio: boolean;
  estado: EstadoFicha;
  grauAutonomia: ItemListaDto | null;
  condicoesSaude: ItemListaDto[];
  alergias: ItemListaDto[];
  meuPapel: PapelFicha;
  souPrincipal: boolean;
}

/** Pedido de OP-11 Editar ficha e parte comum do pedido de OP-09 (RF-020, RF-021). */
export interface DadosFichaDto {
  nomeCompleto: string;
  /** Data simples `AAAA-MM-DD`, não futura. */
  dataNascimento: string;
  contactoEmergencia: string;
  grauAutonomiaId: number | null;
  condicoesSaudeIds: number[];
  alergiasIds: number[];
}

/**
 * Cuidador principal de uma ficha de terceiro: o próprio criador (`eu=true`, RF-024) ou a pessoa convidada pelo
 * e-mail (`eu=false`, RF-025).
 */
export interface CuidadorPrincipalDto {
  eu: boolean;
  email?: string;
}

/** Pedido de OP-09 Criar ficha: própria (RF-030) ou de terceiro, com o Cuidador principal (RF-024, RF-025). */
export interface NovaFichaDto extends DadosFichaDto {
  propria: boolean;
  cuidadorPrincipal?: CuidadorPrincipalDto;
}

/** Documento a enviar em OP-09 ou OP-11 (partes `documento` e `categoria`) ou em OP-15. */
export interface DocumentoSubmetido {
  ficheiro: File;
  categoria: CategoriaDocumento;
}

/** Resposta de OP-15 e elemento da resposta de OP-16: os metadados de um documento. */
export interface DocumentoDto {
  id: string;
  categoria: CategoriaDocumento;
  nomeOriginal: string;
  formato: FormatoDocumento;
  tamanhoBytes: number;
  carregadoEm: string;
}
