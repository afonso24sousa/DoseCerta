/// Dados da conta, sem palavra-passe (OP-04 e OP-05, RF-013).
class UtilizadorDto {
  /// Cria os dados da conta.
  const UtilizadorDto({
    required this.id,
    required this.nome,
    required this.email,
  });

  /// Lê a resposta de OP-04 ou OP-05.
  factory UtilizadorDto.fromJson(Map<String, dynamic> json) {
    return UtilizadorDto(
      id: json['id'] as String,
      nome: json['nome'] as String,
      email: json['email'] as String,
    );
  }

  /// Identificador do Utilizador.
  final String id;

  /// Nome da pessoa.
  final String nome;

  /// E-mail da conta.
  final String email;

  /// Converte para o corpo JSON.
  Map<String, dynamic> toJson() => {'id': id, 'nome': nome, 'email': email};
}
