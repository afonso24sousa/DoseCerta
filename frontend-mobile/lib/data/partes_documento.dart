import '../core/parte_ficheiro.dart';
import '../models/documento_para_envio.dart';
import '../models/ficheiro_escolhido.dart';

/// Converte um documento a enviar na parte de ficheiro do pedido multipart, com o nome da parte que a operação
/// usa (`documento` em OP-09 e OP-11, `ficheiro` em OP-15; documentação da API, secção 3.6).
ParteFicheiro parteDoDocumento(
  DocumentoParaEnvio documento,
  String nomeParte,
) => parteDoFicheiro(documento.ficheiro, nomeParte);

/// Converte um ficheiro escolhido na parte [nomeParte] do pedido multipart (por exemplo `receita` em OP-27 e OP-30).
ParteFicheiro parteDoFicheiro(FicheiroEscolhido ficheiro, String nomeParte) {
  return ParteFicheiro(
    nomeParte: nomeParte,
    nomeFicheiro: ficheiro.nome,
    tipoConteudo: ficheiro.tipoConteudo,
    caminho: ficheiro.caminho,
    bytes: ficheiro.caminho == null ? ficheiro.bytes : null,
  );
}
