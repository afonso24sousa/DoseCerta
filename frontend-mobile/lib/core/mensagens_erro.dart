import '../models/problema_api.dart';
import 'sem_ligacao_erro.dart';

/// Traduz os erros da API e da ligação em mensagens em português europeu (RNF-020), com o campo e a correção
/// nos erros de validação (RNF-028).
class MensagensErro {
  const MensagensErro._();

  /// Mensagem genérica para erros inesperados.
  static const String erroInesperado =
      'Ocorreu um erro inesperado. Tente novamente mais tarde.';

  /// Mensagem quando a sessão terminou ou expirou (protótipo, E02).
  static const String sessaoTerminada =
      'A sessão terminou. Inicie sessão de novo.';

  /// Mensagens por código de erro da API. Os ecrãs podem substituir uma mensagem com `especificas`, quando o
  /// protótipo usa outro texto para o mesmo código (por exemplo `EMAIL_JA_REGISTADO` em E01 e em E03).
  static const Map<String, String> catalogo = {
    'CREDENCIAIS_INVALIDAS': 'E-mail ou palavra-passe incorretos.',
    'EMAIL_JA_REGISTADO': 'Já existe uma conta com este e-mail.',
    'PALAVRA_PASSE_ATUAL_INCORRETA': 'A palavra-passe atual está incorreta. A palavra-passe anterior mantém-se.',
    'NAO_AUTENTICADO': sessaoTerminada,
    'VALIDACAO_INVALIDA': 'Verifique os campos assinalados.',
    'ERRO_INTERNO': erroInesperado,
    // F002 e F003 (I060). Textos do protótipo v02 (E06 a E11) quando existem.
    'ACESSO_PROIBIDO': 'O seu papel nesta ficha não permite esta operação: o Familiar só consulta os dados da ficha.',
    'FICHA_NAO_ENCONTRADA':
        'A ficha não existe ou já não está associado a ela.',
    'FICHA_PROPRIA_JA_EXISTE': 'Já tem uma ficha própria. Só pode ter uma.',
    'FICHA_ARQUIVADA': 'Ficha arquivada: só consulta. Reative-a para registar ou alterar dados.',
    'FICHA_JA_ARQUIVADA': 'A ficha já está arquivada.',
    'FICHA_NAO_ARQUIVADA': 'A ficha não está arquivada.',
    'FICHA_COM_HISTORICO': 'A ficha tem medicação ou histórico e não pode ser eliminada. Arquive-a.',
    'FICHEIRO_DEMASIADO_GRANDE':
        'O ficheiro tem mais de 10 MB; o tamanho máximo é 10 MB.',
    'FORMATO_NAO_SUPORTADO':
        'O formato do ficheiro não é suportado: use PDF, JPG ou PNG.',
    'DOCUMENTO_NAO_GUARDADO': 'O documento não foi guardado e a operação não foi feita. Tente novamente.',
    'DOCUMENTO_NAO_ENCONTRADO':
        'O documento não existe ou já não está associado à ficha.',
    'DOCUMENTO_INDISPONIVEL': 'Não foi possível obter o documento.',
  };

  /// Traduz um erro numa mensagem geral para o `BannerErro`.
  static String traduzir(
    Object erro, {
    Map<String, String> especificas = const {},
  }) {
    if (erro is SemLigacaoErro) {
      return erro.mensagem;
    }
    if (erro is ProblemaApi) {
      return especificas[erro.codigo] ??
          catalogo[erro.codigo] ??
          (erro.mensagem.isNotEmpty ? erro.mensagem : erroInesperado);
    }
    return erroInesperado;
  }

  /// Mensagens por campo, no formato «Rótulo: regra.», a partir de `campos` do erro de validação.
  /// [rotulos] associa o nome do campo na API ao rótulo do ecrã.
  static Map<String, String> mensagensDeCampo(
    ProblemaApi problema,
    Map<String, String> rotulos,
  ) {
    final mensagens = <String, String>{};
    for (final erro in problema.campos) {
      final rotulo = rotulos[erro.campo] ?? erro.campo;
      mensagens.putIfAbsent(erro.campo, () => comRotulo(rotulo, erro.regra));
    }
    return mensagens;
  }

  /// Junta o rótulo do campo à regra, no formato do protótipo («Palavra-passe: tem de ter ...»).
  static String comRotulo(String rotulo, String regra) {
    if (regra.isEmpty) {
      return rotulo;
    }
    return '$rotulo: ${regra[0].toLowerCase()}${regra.substring(1)}';
  }
}
