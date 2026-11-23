/// Referência a uma pessoa, por exemplo quem confirmou uma toma (OP-32, OP-33; RF-065).
class PessoaRefDto {
  /// Cria a referência.
  const PessoaRefDto({required this.id, required this.nome});

  /// Lê a referência da API.
  factory PessoaRefDto.fromJson(Map<String, dynamic> json) {
    return PessoaRefDto(id: json['id'] as String, nome: json['nome'] as String);
  }

  /// Identificador do Utilizador.
  final String id;

  /// Nome atual do Utilizador.
  final String nome;
}
