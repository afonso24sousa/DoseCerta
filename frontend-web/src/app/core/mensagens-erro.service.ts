import { Injectable } from '@angular/core';
import { ProblemaApi } from '../models/problema-api';
import { CODIGO_SEM_LIGACAO } from './ligacao.service';

/**
 * Mensagens em português europeu para os códigos de erro de OP-01 a OP-17 (documentação da API, secções 5.1 e 5.2). Os
 * códigos sem mensagem própria usam a mensagem enviada pelo servidor.
 */
@Injectable({ providedIn: 'root' })
export class MensagensErroService {
  private readonly mensagens: Readonly<Record<string, string>> = {
    VALIDACAO_INVALIDA: 'Há campos por corrigir. Veja as indicações junto de cada campo.',
    EMAIL_JA_REGISTADO: 'Já existe uma conta com este e-mail.',
    CREDENCIAIS_INVALIDAS: 'E-mail ou palavra-passe incorretos.',
    NAO_AUTENTICADO: 'A sessão terminou. Inicie sessão de novo.',
    PALAVRA_PASSE_ATUAL_INCORRETA:
      'A palavra-passe atual está incorreta. A palavra-passe anterior mantém-se.',
    ERRO_INTERNO: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
    FICHA_PROPRIA_JA_EXISTE: 'Já tem uma ficha própria; pode criar fichas de terceiros.',
    FICHA_NAO_ENCONTRADA: 'A ficha não existe ou não tem acesso a ela.',
    ACESSO_PROIBIDO: 'O seu papel nesta ficha não permite esta operação.',
    FICHA_ARQUIVADA: 'A ficha está arquivada e só pode ser consultada. Reative-a para a alterar.',
    FICHA_JA_ARQUIVADA: 'A ficha já está arquivada.',
    FICHA_NAO_ARQUIVADA: 'A ficha não está arquivada.',
    FICHA_COM_HISTORICO:
      'A ficha tem medicação ou histórico registados e não pode ser eliminada; só pode ser arquivada.',
    FICHEIRO_DEMASIADO_GRANDE: 'O ficheiro tem mais de 10 MB; o tamanho máximo é 10 MB.',
    FORMATO_NAO_SUPORTADO: 'O formato do ficheiro não é suportado: use PDF, JPG ou PNG.',
    DOCUMENTO_NAO_GUARDADO:
      'Não foi possível guardar o documento e nada foi gravado. Os dados preenchidos mantêm-se: tente novamente.',
    DOCUMENTO_NAO_ENCONTRADO: 'O documento não existe ou não tem acesso a ele.',
    DOCUMENTO_INDISPONIVEL: 'Não foi possível obter o ficheiro neste momento. Tente novamente.',
  };

  /**
   * Mensagem a mostrar ao Utilizador para um problema.
   *
   * @param problema Problema devolvido pela API.
   * @param doEcra Textos próprios de um ecrã, quando o protótipo usa outro texto para o mesmo código (por exemplo
   * `EMAIL_JA_REGISTADO` em E03).
   */
  mensagem(problema: ProblemaApi, doEcra?: Readonly<Record<string, string>>): string {
    if (problema.codigo === CODIGO_SEM_LIGACAO) {
      return problema.mensagem;
    }

    return doEcra?.[problema.codigo] ?? this.mensagens[problema.codigo] ?? problema.mensagem;
  }
}
