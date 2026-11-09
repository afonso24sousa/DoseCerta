import 'dart:convert';

import 'package:dio/dio.dart';

import '../models/problema_api.dart';
import 'sem_ligacao_erro.dart';

/// Converte a resposta de erro da API em [ProblemaApi] e a falha de ligação ou o tempo limite em
/// [SemLigacaoErro]. O erro convertido segue em `DioException.error` e o `ApiClient` lança-o.
class ErroInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    handler.next(err.copyWith(error: converter(err)));
  }

  /// Converte uma falha do Dio no erro da aplicação.
  static Exception converter(DioException err) {
    final resposta = err.response;
    if (resposta != null) {
      return ProblemaApi.daResposta(
        corpoJson(resposta.data),
        estadoHttp: resposta.statusCode,
      );
    }
    switch (err.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
      case DioExceptionType.unknown:
        return const SemLigacaoErro();
      case DioExceptionType.badCertificate:
      case DioExceptionType.badResponse:
      case DioExceptionType.transformTimeout:
      case DioExceptionType.cancel:
        return ProblemaApi.daResposta(null);
    }
  }

  /// Corpo de erro como JSON. Nos pedidos de ficheiros (OP-17) a resposta é lida em bytes, também quando é um
  /// erro; sem a descodificar perdia-se o código, por exemplo `DOCUMENTO_INDISPONIVEL` (RF-124). Um corpo que não
  /// é JSON fica como está e dá `ERRO_INTERNO`.
  static Object? corpoJson(Object? dados) {
    if (dados is! List<int>) {
      return dados;
    }
    try {
      return jsonDecode(utf8.decode(dados));
    } on FormatException {
      return null;
    }
  }
}
