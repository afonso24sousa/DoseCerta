import { ConviteDto, PessoaFichaDto } from '../../models/pessoas.models';

/** Papel oferecido por um convite, como no protótipo: «Cuidador principal», «Cuidador» ou «Familiar» (RF-042). */
export function papelOferecido(convite: Pick<ConviteDto, 'papelOferecido' | 'paraPrincipal'>): string {
  return convite.paraPrincipal ? 'Cuidador principal' : convite.papelOferecido;
}

/** Papel de uma pessoa da ficha, com a indicação de principal provisório (RF-025). */
export function papelPessoa(pessoa: Pick<PessoaFichaDto, 'papel' | 'principal' | 'provisorio'>): string {
  if (pessoa.principal) {
    return pessoa.provisorio ? 'Cuidador principal (provisório)' : 'Cuidador principal';
  }

  return pessoa.papel;
}
