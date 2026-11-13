/// Origem do ficheiro a carregar (RF-022, RF-052; decisão D2 da I060). O Ficheiro é preciso para os PDF, que não
/// vêm da câmara nem da galeria.
enum OrigemFicheiro {
  /// Fotografia tirada com a aplicação de câmara do sistema.
  camera('Câmara'),

  /// Imagem escolhida na galeria.
  galeria('Galeria'),

  /// Ficheiro (PDF, JPG ou PNG) escolhido no seletor de ficheiros do sistema.
  ficheiro('Ficheiro');

  const OrigemFicheiro(this.rotulo);

  /// Texto apresentado ao Utilizador.
  final String rotulo;
}
