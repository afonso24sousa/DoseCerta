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
        resposta.data,
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
}
