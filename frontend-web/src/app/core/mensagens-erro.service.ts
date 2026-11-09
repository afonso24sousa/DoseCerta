import { Injectable } from '@angular/core';
import { ProblemaApi } from '../models/problema-api';
import { CODIGO_SEM_LIGACAO } from './ligacao.service';

/**
 * Mensagens em português europeu para os códigos de erro de OP-01 a OP-06 (documentação da API, secção 5.1). Os
 * códigos sem mensagem própria usam a mensagem enviada pelo servidor.
 */
@Injectable({ providedIn: 'root' })
export class MensagensErroService {
  private readonly mensagens: Readonly<Record<string, string>> = {
    VALIDACAO_INVALIDA: 'Há campos por corrigir. Veja as indicações junto de cada campo.',
    EMAIL_JA_REGISTADO: 'Já existe uma conta com este e-mail.',
    CREDENCIAIS_INVALIDAS: 'E-mail ou palavra-passe incorretos.',
    NAO_AUTENTICADO: 'A sessão terminou. Inicie sessão de novo.',
    PALAVRA_PASSE_ATUAL_INCORRETA: 'A palavra-passe atual está incorreta.',
    ERRO_INTERNO: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
  };

  /** Mensagem a mostrar ao Utilizador para um problema. */
  mensagem(problema: ProblemaApi): string {
    if (problema.codigo === CODIGO_SEM_LIGACAO) {
      return problema.mensagem;
    }

    return this.mensagens[problema.codigo] ?? problema.mensagem;
  }
}
