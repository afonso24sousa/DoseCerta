import '../core/parte_ficheiro.dart';
import '../models/documento_para_envio.dart';

/// Converte um documento a enviar na parte de ficheiro do pedido multipart, com o nome da parte que a operação
/// usa (`documento` em OP-09 e OP-11, `ficheiro` em OP-15; documentação da API, secção 3.6).
ParteFicheiro parteDoDocumento(DocumentoParaEnvio documento, String nomeParte) {
  final ficheiro = documento.ficheiro;
  return ParteFicheiro(
    nomeParte: nomeParte,
    nomeFicheiro: ficheiro.nome,
    tipoConteudo: ficheiro.tipoConteudo,
    caminho: ficheiro.caminho,
    bytes: ficheiro.caminho == null ? ficheiro.bytes : null,
  );
}
