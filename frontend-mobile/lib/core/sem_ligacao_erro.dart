/// Erro local, sem código HTTP: o pedido falhou por falta de ligação ao servidor ou por tempo limite.
/// Nada é guardado nem posto em fila no dispositivo (RNF-033).
class SemLigacaoErro implements Exception {
  /// Cria o erro com a mensagem a apresentar.
  const SemLigacaoErro([this.mensagem = mensagemPadrao]);

  /// Mensagem apresentada ao Utilizador.
  static const String mensagemPadrao =
      'Sem ligação ao servidor. A operação não foi feita.';

  /// Mensagem apresentada ao Utilizador.
  final String mensagem;

  @override
  String toString() => 'SemLigacaoErro: $mensagem';
}
