import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';

/// Adaptador HTTP falso: guarda o último pedido e o corpo enviado e devolve [resposta].
class AdaptadorQueGuarda implements HttpClientAdapter {
  RequestOptions? pedido;
  List<int> corpo = const [];
  ResponseBody? resposta;

  /// Responde com o [json] e o código HTTP [estado].
  void responderJson(String json, [int estado = 200]) {
    resposta = ResponseBody.fromString(
      json,
      estado,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  /// Corpo enviado, lido como texto.
  String get corpoTexto => utf8.decode(corpo, allowMalformed: true);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    pedido = options;
    final bytes = <int>[];
    if (requestStream != null) {
      await for (final bloco in requestStream) {
        bytes.addAll(bloco);
      }
    }
    corpo = bytes;
    return resposta ??
        ResponseBody.fromString(
          '{}',
          200,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
  }

  @override
  void close({bool force = false}) {}
}
