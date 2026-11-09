/// Quem é o Cuidador principal da ficha de terceiro a criar (OP-09): o próprio criador (RF-024) ou a pessoa
/// convidada pelo [email], ficando o criador como Cuidador principal provisório até à resposta (RF-025).
class DefinicaoPrincipalDto {
  /// O criador é o Cuidador principal.
  const DefinicaoPrincipalDto.eu() : eu = true, email = null;

  /// Convida a pessoa com o [email] para Cuidador principal.
  const DefinicaoPrincipalDto.convidar(String this.email) : eu = false;

  /// Indica se o criador é o Cuidador principal.
  final bool eu;

  /// E-mail da pessoa a convidar, quando [eu] é falso.
  final String? email;

  /// Converte para o corpo JSON.
  Map<String, dynamic> toJson() =>
      eu ? {'eu': true} : {'eu': false, 'email': email};
}
