import 'categoria_documento.dart';
import 'ficheiro_escolhido.dart';

/// Documento a enviar numa criação ou edição de ficha (OP-09, OP-11) ou num carregamento (OP-15): o ficheiro e a
/// categoria (RF-022). Corresponde ao `DocumentoParaEnvio` do modelo do frontend web.
class DocumentoParaEnvio {
  /// Cria o documento.
  const DocumentoParaEnvio({required this.categoria, required this.ficheiro});

  /// Categoria do documento.
  final CategoriaDocumento categoria;

  /// Ficheiro a enviar.
  final FicheiroEscolhido ficheiro;
}
