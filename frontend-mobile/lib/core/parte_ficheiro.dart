import 'package:dio/dio.dart';

/// Parte de ficheiro de um pedido `multipart/form-data` (documentação da API, secção 3.6): o nome da parte
/// (`documento` em OP-09 e OP-11, `ficheiro` em OP-15), o nome do ficheiro e o tipo de conteúdo.
///
/// O conteúdo vem do [caminho] no dispositivo (lido em blocos, sem carregar o ficheiro inteiro na memória) ou de
/// [bytes]; tem de existir exatamente um dos dois.
class ParteFicheiro {
  /// Cria a parte.
  const ParteFicheiro({
    required this.nomeParte,
    required this.nomeFicheiro,
    required this.tipoConteudo,
    this.caminho,
    this.bytes,
  }) : assert(
         (caminho == null) != (bytes == null),
         'Indique o caminho ou os bytes do ficheiro, não os dois.',
       );

  /// Nome da parte no pedido.
  final String nomeParte;

  /// Nome do ficheiro enviado ao servidor (guardado como `nomeOriginal`).
  final String nomeFicheiro;

  /// Tipo de conteúdo (`application/pdf`, `image/jpeg` ou `image/png`).
  final String tipoConteudo;

  /// Caminho do ficheiro no dispositivo.
  final String? caminho;

  /// Conteúdo do ficheiro.
  final List<int>? bytes;

  /// Converte a parte no ficheiro do Dio.
  Future<MultipartFile> paraMultipart() async {
    final tipo = DioMediaType.parse(tipoConteudo);
    final origem = caminho;
    if (origem != null) {
      return MultipartFile.fromFile(
        origem,
        filename: nomeFicheiro,
        contentType: tipo,
      );
    }
    return MultipartFile.fromBytes(
      bytes!,
      filename: nomeFicheiro,
      contentType: tipo,
    );
  }
}
