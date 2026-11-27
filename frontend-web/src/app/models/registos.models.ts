/** Tipo de um registo da ficha (F009): uma ocorrência diária ou uma nota partilhada entre os Cuidadores. */
export type TipoRegisto = 'Ocorrencia' | 'Nota';

/** Autor de um registo; `nome` é o nome atual da conta (RF-092). */
export interface AutorRegistoDto {
  id: string;
  nome: string;
}

/** Ocorrência ou nota devolvida por OP-46 a OP-49. Depois de registado, não pode ser alterado nem eliminado. */
export interface RegistoDto {
  id: string;
  fichaId: string;
  tipo: TipoRegisto;
  descricao: string;
  autor: AutorRegistoDto;
  /** Data e hora em que a ocorrência aconteceu, com o desvio de Lisboa; `null` nas notas. */
  ocorridaEm: string | null;
  /** Momento do registo, com o desvio de Lisboa (RF-092). */
  criadaEm: string;
}

/** Pedido de OP-46: a descrição (até 2000 caracteres) e a data e hora em que aconteceu, com o desvio. */
export interface NovaOcorrenciaDto {
  descricao: string;
  ocorridaEm: string;
}

/** Pedido de OP-47: o texto da nota (até 2000 caracteres). */
export interface NovaNotaDto {
  descricao: string;
}
