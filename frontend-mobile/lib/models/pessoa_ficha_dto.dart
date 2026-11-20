import 'papel.dart';

/// Pessoa associada a uma ficha (OP-18, OP-21, OP-23, OP-24 e OP-26).
class PessoaFichaDto {
  /// Cria a pessoa.
  const PessoaFichaDto({
    required this.utilizadorId,
    required this.nome,
    required this.email,
    required this.papel,
    required this.principal,
    required this.provisorio,
  });

  /// Lê uma pessoa da API.
  factory PessoaFichaDto.fromJson(Map<String, dynamic> json) {
    return PessoaFichaDto(
      utilizadorId: json['utilizadorId'] as String,
      nome: json['nome'] as String,
      email: json['email'] as String,
      papel: Papel.fromJson(json['papel']),
      principal: json['principal'] as bool,
      provisorio: json['provisorio'] as bool,
    );
  }

  /// Identificador do Utilizador.
  final String utilizadorId;

  /// Nome do Utilizador.
  final String nome;

  /// E-mail do Utilizador.
  final String email;

  /// Papel na ficha.
  final Papel papel;

  /// Indica se é o Cuidador principal da ficha.
  final bool principal;

  /// Indica se é Cuidador principal só a título provisório, enquanto o convite para principal não tem resposta
  /// (RF-025).
  final bool provisorio;
}
