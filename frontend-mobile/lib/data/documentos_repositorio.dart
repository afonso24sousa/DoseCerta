import 'dart:typed_data';

import '../core/api_client.dart';
import '../models/documento_dto.dart';
import '../models/documento_para_envio.dart';
import 'partes_documento.dart';

/// Documentos da ficha (OP-15, OP-16 e OP-17).
class DocumentosRepositorio {
  /// Cria o repositório sobre o [api].
  const DocumentosRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-15: `POST /fichas/{fichaId}/documentos`, com as partes `categoria` e `ficheiro` (API, secção 3.6).
  Future<DocumentoDto> carregar(
    String fichaId,
    DocumentoParaEnvio documento,
  ) async {
    final corpo = await api.enviarMultipart(
      'POST',
      '/fichas/$fichaId/documentos',
      campos: [MapEntry('categoria', documento.categoria.valor)],
      ficheiros: [parteDoDocumento(documento, 'ficheiro')],
    );
    return DocumentoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-16: `GET /fichas/{fichaId}/documentos`.
  Future<List<DocumentoDto>> listar(String fichaId) async {
    final corpo = await api.get('/fichas/$fichaId/documentos');
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(DocumentoDto.fromJson)
        .toList(growable: false);
  }

  /// OP-17: `GET /documentos/{documentoId}`; devolve o conteúdo do ficheiro.
  Future<Uint8List> descarregar(String documentoId) {
    return api.obterBytes('/documentos/$documentoId');
  }
}
