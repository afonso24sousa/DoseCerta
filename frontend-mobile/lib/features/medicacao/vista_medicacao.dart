/// Separadores do E15 Medicação (protótipo v02; contrato de interface da F006, secção 2).
enum VistaMedicacao {
  /// Tratamentos em curso e terminados (I067).
  tratamentos('Tratamentos'),

  /// Calendário de tomas (I069).
  calendario('Calendário de tomas');

  const VistaMedicacao(this.rotulo);

  /// Texto do separador.
  final String rotulo;

  /// Lê o parâmetro `vista` da rota (`?vista=calendario`); sem ele, ou com outro valor, abre nos tratamentos.
  static VistaMedicacao daRota(String? valor) =>
      valor == 'calendario' ? calendario : tratamentos;
}
