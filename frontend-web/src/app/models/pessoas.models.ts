import { PapelFicha } from './fichas.models';

/** Estado de um convite (modelo de classes do frontend web v02, secção 4.1). */
export type EstadoConvite = 'Pendente' | 'Aceite' | 'Recusado';

/** Pessoa associada a uma ficha, devolvida por OP-18, OP-21, OP-23, OP-24 e OP-26. */
export interface PessoaFichaDto {
  utilizadorId: string;
  nome: string;
  email: string;
  papel: PapelFicha;
  /** É o Cuidador principal da ficha. */
  principal: boolean;
  /** É Cuidador principal a título provisório, enquanto o convite para Cuidador principal está pendente (RF-025). */
  provisorio: boolean;
}

/** Pedido de OP-19 Convidar pessoa para a ficha (RF-040). */
export interface NovoConviteDto {
  email: string;
  papel: PapelFicha;
  /** Oferece o papel de Cuidador principal; só com `papel` `Cuidador` e nunca na ficha própria (RF-025, RF-034). */
  paraPrincipal: boolean;
}

/** Convite enviado (OP-19) ou recebido (OP-20). */
export interface ConviteDto {
  id: string;
  fichaId: string;
  nomeFicha: string;
  papelOferecido: PapelFicha;
  paraPrincipal: boolean;
  /** Nome de quem convidou. */
  convidadoPor: string;
  estado: EstadoConvite;
  criadoEm: string;
}

/** Pedido de OP-23 e OP-24: o papel pretendido para a pessoa. */
export interface MudancaPapelDto {
  papel: PapelFicha;
}

/** Pedido de OP-26: o Cuidador que passa a Cuidador principal (RF-132). */
export interface TransferenciaPrincipalDto {
  utilizadorId: string;
}
