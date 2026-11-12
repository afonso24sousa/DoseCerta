/// Validação prévia da palavra-passe (RNF-003): pelo menos 8 caracteres, com uma letra e um algarismo. Segue a
/// regra do servidor (`ParametrosDeNegocio.PalavraPasseValida`: `char.IsLetter` e `char.IsDigit`); a decisão
/// final é sempre do servidor.
class ValidadorPalavraPasse {
  const ValidadorPalavraPasse._();

  /// Comprimento mínimo.
  static const int comprimentoMinimo = 8;

  /// Regra apresentada ao Utilizador (o mesmo texto do servidor).
  static const String regra =
      'Tem de ter pelo menos 8 caracteres, com uma letra e um algarismo.';

  /// Regra apresentada quando o campo está vazio.
  static const String regraObrigatoria = 'É obrigatória.';

  static final RegExp _letra = RegExp(r'\p{L}', unicode: true);
  static final RegExp _algarismo = RegExp(r'\p{Nd}', unicode: true);

  /// Indica se a palavra-passe cumpre o RNF-003.
  static bool valida(String texto) =>
      texto.length >= comprimentoMinimo &&
      _letra.hasMatch(texto) &&
      _algarismo.hasMatch(texto);

  /// Devolve a regra violada, ou `null` se a palavra-passe for válida.
  static String? validar(String texto) {
    if (texto.isEmpty) {
      return regraObrigatoria;
    }
    return valida(texto) ? null : regra;
  }
}
