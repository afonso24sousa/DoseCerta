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
