/// O ficheiro escolhido não pode ser enviado (formato, tamanho ou falha da câmara ou do seletor). É detetado no
/// dispositivo, antes de qualquer pedido; a decisão final continua a ser do servidor (RNF-008).
class FicheiroRecusadoErro implements Exception {
  /// Cria o erro com a [mensagem] a apresentar.
  const FicheiroRecusadoErro(this.mensagem);

  /// Formato diferente de PDF, JPG ou PNG.
  static const String formatoInvalido =
      'O formato do ficheiro não é suportado: use PDF, JPG ou PNG.';

  /// Ficheiro com mais de 10 MB.
  static const String demasiadoGrande =
      'O ficheiro tem mais de 10 MB; o tamanho máximo é 10 MB.';

  /// Falha da câmara, da galeria ou do seletor de ficheiros.
  static const String origemIndisponivel =
      'Não foi possível obter o ficheiro. Tente novamente ou escolha outra origem.';

  /// Mensagem apresentada ao Utilizador.
  final String mensagem;

  @override
  String toString() => 'FicheiroRecusadoErro: $mensagem';
}
