/// Dados do registo de conta (OP-01, RF-010).
class NovaContaDto {
  /// Cria os dados do registo.
  const NovaContaDto({
    required this.nome,
    required this.email,
    required this.palavraPasse,
  });

  /// Nome da pessoa.
  final String nome;

  /// E-mail da conta.
  final String email;

  /// Palavra-passe (RNF-003); nunca é guardada no dispositivo.
  final String palavraPasse;

  /// Converte para o corpo JSON de OP-01.
  Map<String, dynamic> toJson() => {
    'nome': nome,
    'email': email,
    'palavraPasse': palavraPasse,
  };
}
