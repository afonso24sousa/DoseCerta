import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ProblemaApi } from '../models/problema-api';

/** Comprimento máximo do e-mail aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_EMAIL = 254;

/** Comprimento máximo do nome da conta aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_NOME = 120;

const PADRAO_EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const LETRA = /\p{L}/u;
const ALGARISMO = /\p{Nd}/u;

/**
 * Validadores prévios dos formulários, iguais às regras do servidor (`ValidadorDados`), para nunca impedirem o envio de
 * dados que o servidor aceitaria. O servidor valida sempre. Cada erro tem como valor a regra a mostrar (RNF-028).
 */
export class ValidadoresFormulario {
  /** Campo obrigatório (o servidor ignora os espaços nas pontas). */
  static obrigatorio(): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null =>
      String(controlo.value ?? '').trim().length === 0 ? { obrigatorio: 'É obrigatório.' } : null;
  }

  /** Texto com um comprimento máximo, depois de retirados os espaços nas pontas. */
  static comprimentoMaximo(maximo: number): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null =>
      String(controlo.value ?? '').trim().length > maximo
        ? { comprimentoMaximo: `Tem de ter no máximo ${maximo} caracteres.` }
        : null;
  }

  /** E-mail com formato válido, como no servidor; vazio fica para o validador de obrigatório. */
  static email(): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null => {
      const valor = String(controlo.value ?? '').trim();
      if (valor.length === 0) {
        return null;
      }

      return valor.length <= COMPRIMENTO_MAXIMO_EMAIL && PADRAO_EMAIL.test(valor)
        ? null
        : { email: 'Tem de ter um formato de e-mail válido, por exemplo nome@exemplo.pt.' };
    };
  }

  /** Palavra-passe do RNF-003: pelo menos 8 caracteres, com uma letra e um algarismo. */
  static palavraPasse(): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null => {
      const valor = String(controlo.value ?? '');
      if (valor.length === 0) {
        return null;
      }

      return valor.length >= 8 && LETRA.test(valor) && ALGARISMO.test(valor)
        ? null
        : { palavraPasse: 'Tem de ter pelo menos 8 caracteres, com uma letra e um algarismo.' };
    };
  }

  /**
   * Coloca os erros por campo devolvidos pelo servidor nos controlos com o mesmo nome, sem alterar os valores escritos
   * (RNF-028). O erro desaparece quando o Utilizador altera o campo.
   *
   * @returns `true` se pelo menos um erro ficou junto de um campo.
   */
  static aplicarErrosDoServidor(formulario: FormGroup, problema: ProblemaApi): boolean {
    let aplicado = false;
    for (const erro of problema.campos ?? []) {
      const controlo = formulario.get(erro.campo);
      if (controlo) {
        controlo.setErrors({ ...(controlo.errors ?? {}), servidor: erro.regra });
        controlo.markAsTouched();
        aplicado = true;
      }
    }

    return aplicado;
  }

  /**
   * Coloca uma mensagem completa do servidor junto de um campo concreto (por exemplo o e-mail já registado); é mostrada
   * tal como está, sem o rótulo do campo antes.
   */
  static erroNoCampo(controlo: AbstractControl | null, mensagem: string): void {
    controlo?.setErrors({ ...(controlo.errors ?? {}), mensagem });
    controlo?.markAsTouched();
  }
}
