/// Credencial devolvida pelo início de sessão (OP-02): JWT com validade de 8 horas (RNF-009).
class SessaoDto {
  /// Cria a credencial.
  const SessaoDto({
    required this.token,
    required this.tipo,
    required this.expiraEm,
  });

  /// Lê a resposta de OP-02.
  factory SessaoDto.fromJson(Map<String, dynamic> json) {
    return SessaoDto(
      token: json['token'] as String,
      tipo: json['tipo'] as String,
      expiraEm: json['expiraEm'] as String,
    );
  }

  /// Token JWT.
  final String token;

  /// Tipo do token (`Bearer`).
  final String tipo;

  /// Instante de expiração, em ISO 8601 com o desvio de Lisboa.
  final String expiraEm;

  /// Converte para o corpo JSON.
  Map<String, dynamic> toJson() => {
    'token': token,
    'tipo': tipo,
    'expiraEm': expiraEm,
  };
}
