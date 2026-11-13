import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

/// O documento foi obtido, mas o dispositivo não o conseguiu abrir.
class DocumentoNaoAbertoErro implements Exception {
  /// Cria o erro com a [mensagem] a apresentar.
  const DocumentoNaoAbertoErro(this.mensagem);

  /// Não há no dispositivo uma aplicação para este tipo de ficheiro.
  static const String semAplicacao =
      'Não há no dispositivo uma aplicação para abrir este ficheiro.';

  /// Outra falha ao abrir.
  static const String falhou = 'Não foi possível abrir o documento.';

  /// Mensagem apresentada ao Utilizador.
  final String mensagem;

  @override
  String toString() => 'DocumentoNaoAbertoErro: $mensagem';
}

/// Abre um documento descarregado (OP-17) no visualizador do dispositivo (protótipo, E10: «abertura do ficheiro no
/// visualizador do dispositivo»). Não existe no modelo de classes; separa a operação do sistema para os testes.
abstract interface class AbridorDocumentos {
  /// Abre o conteúdo [bytes] do ficheiro [nome], com o [tipoConteudo] indicado. Lança [DocumentoNaoAbertoErro] se
  /// o dispositivo não o conseguir abrir.
  Future<void> abrir(
    List<int> bytes, {
    required String nome,
    required String tipoConteudo,
  });
}

/// Guarda o ficheiro na pasta de cache privada da aplicação e entrega-o ao visualizador pelo `FileProvider` do
/// `open_filex`, sem permissões de armazenamento (passo 1 da I060). A pasta é esvaziada antes de cada abertura,
/// para não se acumularem documentos de saúde no dispositivo.
class AbridorDocumentosDispositivo implements AbridorDocumentos {
  /// Cria o abridor.
  const AbridorDocumentosDispositivo();

  @override
  Future<void> abrir(
    List<int> bytes, {
    required String nome,
    required String tipoConteudo,
  }) async {
    final cache = await getTemporaryDirectory();
    final pasta = Directory('${cache.path}/documentos');
    if (pasta.existsSync()) {
      await pasta.delete(recursive: true);
    }
    await pasta.create(recursive: true);
    final simples = nome.split(RegExp(r'[/\\]')).last.trim();
    final ficheiro = File(
      '${pasta.path}/${simples.isEmpty ? 'documento' : simples}',
    );
    await ficheiro.writeAsBytes(bytes, flush: true);
    final resultado = await OpenFilex.open(ficheiro.path, type: tipoConteudo);
    switch (resultado.type) {
      case ResultType.done:
        return;
      case ResultType.noAppToOpen:
        throw const DocumentoNaoAbertoErro(DocumentoNaoAbertoErro.semAplicacao);
      case ResultType.fileNotFound:
      case ResultType.permissionDenied:
      case ResultType.error:
        throw const DocumentoNaoAbertoErro(DocumentoNaoAbertoErro.falhou);
    }
  }
}

/// Abridor de documentos da aplicação (substituído nos testes).
final abridorDocumentosProvider = Provider<AbridorDocumentos>(
  (ref) => const AbridorDocumentosDispositivo(),
);
