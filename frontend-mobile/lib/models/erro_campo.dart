/// Campo inválido e correção necessária, devolvido nos erros de validação da API (RNF-028).
class ErroCampo {
  /// Cria um erro de campo.
  const ErroCampo({required this.campo, required this.regra});

  /// Lê um elemento de `campos` do corpo de erro (documentação da API, secção 3.3).
  factory ErroCampo.fromJson(Map<String, dynamic> json) {
    return ErroCampo(
      campo: json['campo'] as String? ?? '',
      regra: json['regra'] as String? ?? '',
    );
  }

  /// Nome do campo no corpo do pedido (por exemplo `palavraPasse`).
  final String campo;

  /// Regra violada, com o valor ou formato esperado.
  final String regra;
}
