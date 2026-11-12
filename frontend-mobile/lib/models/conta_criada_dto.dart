/// Conta criada e número de convites pendentes que a esperam (OP-01, RF-044).
class ContaCriadaDto {
  /// Cria a resposta do registo.
  const ContaCriadaDto({
    required this.id,
    required this.nome,
    required this.email,
    required this.convitesPendentes,
  });

  /// Lê a resposta de OP-01.
  factory ContaCriadaDto.fromJson(Map<String, dynamic> json) {
    return ContaCriadaDto(
      id: json['id'] as String,
      nome: json['nome'] as String,
      email: json['email'] as String,
      convitesPendentes: json['convitesPendentes'] as int,
    );
  }

  /// Identificador do Utilizador.
  final String id;

  /// Nome da pessoa.
  final String nome;

  /// E-mail da conta.
  final String email;

  /// Convites pendentes dirigidos a este e-mail.
  final int convitesPendentes;

  /// Converte para o corpo JSON.
  Map<String, dynamic> toJson() => {
    'id': id,
    'nome': nome,
    'email': email,
    'convitesPendentes': convitesPendentes,
  };
}
