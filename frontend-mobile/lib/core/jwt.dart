import 'dart:convert';

/// Lê o identificador do Utilizador (`sub`) do conteúdo do token JWT devolvido por OP-02.
///
/// Não verifica a assinatura: quem a verifica é o servidor, em cada pedido (RNF-004, RNF-054). O valor serve só
/// para identificar a sessão no dispositivo. Devolve `null` se o token não tiver o formato esperado.
String? lerSujeitoDoToken(String token) {
  final partes = token.split('.');
  if (partes.length != 3) {
    return null;
  }
  try {
    final conteudo = utf8.decode(
      base64Url.decode(base64Url.normalize(partes[1])),
    );
    final mapa = jsonDecode(conteudo);
    if (mapa is Map<String, dynamic> && mapa['sub'] is String) {
      return mapa['sub'] as String;
    }
  } on FormatException {
    return null;
  }
  return null;
}
