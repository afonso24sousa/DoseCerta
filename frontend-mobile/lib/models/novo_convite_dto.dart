import 'papel.dart';

/// Dados para convidar uma pessoa para a ficha (OP-19).
class NovoConviteDto {
  /// Cria os dados do convite.
  const NovoConviteDto({
    required this.email,
    required this.papel,
    this.paraPrincipal = false,
  });

  /// E-mail da pessoa convidada (RF-040).
  final String email;

  /// Papel oferecido, Cuidador ou Familiar (RF-040).
  final Papel papel;

  /// Oferece o papel de Cuidador principal; o [papel] tem de ser Cuidador (RF-025, RF-027).
  final bool paraPrincipal;

  /// Converte para o corpo JSON.
  Map<String, dynamic> toJson() => {
    'email': email,
    'papel': papel.valor,
    'paraPrincipal': paraPrincipal,
  };
}
