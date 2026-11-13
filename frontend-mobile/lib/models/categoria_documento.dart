/// Categoria de um documento da ficha (RF-022).
enum CategoriaDocumento {
  /// Receita médica.
  receitaMedica('ReceitaMedica', 'Receita médica'),

  /// Relatório clínico.
  relatorioClinico('RelatorioClinico', 'Relatório clínico'),

  /// Documento de identificação.
  documentoIdentificacao(
    'DocumentoIdentificacao',
    'Documento de identificação',
  );

  const CategoriaDocumento(this.valor, this.rotulo);

  /// Valor usado pela API.
  final String valor;

  /// Texto apresentado ao Utilizador (protótipo, E07 e E10).
  final String rotulo;

  /// Lê o valor da API.
  static CategoriaDocumento fromJson(Object? valor) =>
      CategoriaDocumento.values.firstWhere(
        (categoria) => categoria.valor == valor,
        orElse: () => throw FormatException(
          'Categoria de documento desconhecida: $valor',
        ),
      );
}
