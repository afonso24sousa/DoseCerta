/// Ficheiro escolhido no dispositivo (câmara, galeria ou seletor de ficheiros) e já validado: PDF, JPG ou PNG,
/// até 10 MB (RNF-008). Não existe no modelo de classes do mobile; corresponde ao ficheiro do
/// `DocumentoParaEnvio` do modelo do frontend web.
class FicheiroEscolhido {
  /// Cria o ficheiro. Tem de ter o [caminho] ou os [bytes].
  const FicheiroEscolhido({
    required this.nome,
    required this.tipoConteudo,
    required this.tamanhoBytes,
    this.caminho,
    this.bytes,
  }) : assert(
         caminho != null || bytes != null,
         'Indique o caminho ou os bytes do ficheiro.',
       );

  /// Nome do ficheiro, enviado ao servidor.
  final String nome;

  /// Tipo de conteúdo (`application/pdf`, `image/jpeg` ou `image/png`).
  final String tipoConteudo;

  /// Tamanho em bytes.
  final int tamanhoBytes;

  /// Caminho no dispositivo.
  final String? caminho;

  /// Conteúdo, quando não há caminho.
  final List<int>? bytes;
}
