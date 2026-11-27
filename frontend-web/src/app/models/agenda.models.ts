import { EstadoToma } from './tomas.models';

/** Tipo de um evento da agenda (OP-45). */
export type TipoItemAgenda = 'Toma' | 'Compromisso';

/** Vista da agenda (OP-45): dia, semana de segunda-feira a domingo ou mês (RF-084 a RF-086). */
export type VistaAgenda = 'dia' | 'semana' | 'mes';

/** Evento da agenda, com o nome do utente (RF-089), devolvido por OP-45. */
export interface ItemAgendaDto {
  /** Identificador da toma ou do compromisso. */
  id: string;
  tipo: TipoItemAgenda;
  fichaId: string;
  nomeFicha: string;
  /** «Medicamento Dose» nas tomas; o motivo nos compromissos. */
  titulo: string;
  /** Data e hora do evento, com o desvio de Lisboa. */
  inicio: string;
  /** Estado da toma; só existe nas tomas. */
  estado?: EstadoToma;
  /** Nunca usado na agenda: as tomas não se confirmam nem alteram a partir dela (RF-087, RF-088). */
  confirmavel: boolean;
}

/** Antecedência do aviso de um compromisso, em minutos (RF-116). */
export type AntecedenciaAviso = 30 | 60 | 120 | 1440 | 2880;

/** Dados de OP-41 e OP-43. */
export interface DadosCompromissoDto {
  /** Data e hora com o desvio de Lisboa (ISO 8601). */
  dataHora: string;
  local: string;
  motivo: string;
  antecedenciaAvisoMinutos: AntecedenciaAviso;
}

/** Compromisso de saúde, devolvido por OP-41 a OP-43. */
export interface CompromissoDto extends DadosCompromissoDto {
  id: string;
  fichaId: string;
}
