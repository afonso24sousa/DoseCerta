import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ProblemaApi } from '../models/problema-api';

/** Comprimento máximo do e-mail aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_EMAIL = 254;

/** Comprimento máximo do nome da conta aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_NOME = 120;

/** Comprimento máximo do nome completo de uma ficha aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_NOME_FICHA = 150;

/** Comprimento máximo do contacto de emergência aceite pelo servidor. */
export const COMPRIMENTO_MAXIMO_TELEFONE = 30;

/** Data de nascimento mais antiga aceite pelo servidor. */
export const DATA_NASCIMENTO_MINIMA = '1900-01-01';

const PADRAO_EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const PADRAO_TELEFONE = /^\+?[0-9][0-9 ]{5,}$/;
const PADRAO_DATA = /^\d{4}-\d{2}-\d{2}$/;
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

  /** Contacto telefónico: algarismos, espaços e o prefixo + opcional, até 30 caracteres; vazio fica para o obrigatório. */
  static telefone(): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null => {
      const valor = String(controlo.value ?? '').trim();
      if (valor.length === 0) {
        return null;
      }

      return valor.length <= COMPRIMENTO_MAXIMO_TELEFONE && PADRAO_TELEFONE.test(valor)
        ? null
        : {
            telefone:
              'Tem de ser um número de telefone com algarismos, espaços e o prefixo + opcional, até 30 caracteres, ' +
              'por exemplo +351 910 000 001.',
          };
    };
  }

  /**
   * Data de nascimento `AAAA-MM-DD` entre 1900-01-01 e hoje (na hora local, que o servidor confirma na hora de Lisboa);
   * vazia fica para o obrigatório.
   */
  static dataNascimento(): ValidatorFn {
    return (controlo: AbstractControl): ValidationErrors | null => {
      const valor = String(controlo.value ?? '').trim();
      if (valor.length === 0) {
        return null;
      }

      if (!PADRAO_DATA.test(valor) || Number.isNaN(Date.parse(valor))) {
        return { data: 'Tem de ser uma data válida.' };
      }

      if (valor > ValidadoresFormulario.hoje()) {
        return { dataFutura: 'Não pode ser uma data futura.' };
      }

      return valor < DATA_NASCIMENTO_MINIMA
        ? { dataMinima: 'Tem de ser igual ou posterior a 01/01/1900.' }
        : null;
    };
  }

  /** Data de hoje no formato `AAAA-MM-DD`, na hora local do navegador. */
  static hoje(): string {
    const agora = new Date();
    const mes = String(agora.getMonth() + 1).padStart(2, '0');
    const dia = String(agora.getDate()).padStart(2, '0');
    return `${agora.getFullYear()}-${mes}-${dia}`;
  }

  /**
   * Coloca os erros por campo devolvidos pelo servidor nos controlos com o mesmo nome, sem alterar os valores escritos
   * (RNF-028). O erro desaparece quando o Utilizador altera o campo. Os campos com índice (por exemplo
   * `documentos[0].categoria`) correspondem ao caminho `documentos.0.categoria` do formulário.
   *
   * @returns `true` se pelo menos um erro ficou junto de um campo.
   */
  static aplicarErrosDoServidor(formulario: FormGroup, problema: ProblemaApi): boolean {
    let aplicado = false;
    for (const erro of problema.campos ?? []) {
      const controlo = formulario.get(erro.campo.replace(/\[(\d+)\]/g, '.$1'));
      if (controlo) {
        controlo.setErrors({ ...(controlo.errors ?? {}), servidor: erro.regra });
        controlo.markAsTouched();
        aplicado = true;
      }
    }

    return aplicado;
  }

  /**
   * Primeira regra violada de um controlo, para mostrar junto dele, ou `null` se estiver válido ou ainda não tiver sido
   * tocado. Uma mensagem completa (erro `mensagem`) aparece tal como está, como no {@link CampoFormularioComponent}.
   */
  static mensagemErro(controlo: AbstractControl): string | null {
    if (!controlo.errors || !controlo.touched) {
      return null;
    }

    const erros = controlo.errors as Record<string, unknown>;
    if (typeof erros['mensagem'] === 'string') {
      return erros['mensagem'];
    }

    const primeiro = Object.values(erros).find((v) => typeof v === 'string');
    return typeof primeiro === 'string' ? primeiro : 'Valor inválido.';
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
