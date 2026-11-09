import 'package:dio/dio.dart';

import 'auth_interceptor.dart';
import 'configuracao_api.dart';
import 'erro_interceptor.dart';

/// Cliente HTTP (Dio) com o endereço base HTTPS (RNF-002), o tempo limite de ligação de 8 segundos (RNF-033) e
/// os interceptores de autenticação e de erros. Os repositórios só fazem pedidos através desta classe.
///
/// Em caso de erro lança `ProblemaApi` (resposta de erro da API) ou `SemLigacaoErro` (sem ligação).
class ApiClient {
  /// Cria o cliente para a [configuracao] indicada.
  ApiClient({
    required ConfiguracaoApi configuracao,
    required AuthInterceptor authInterceptor,
    Dio? dio,
  }) : baseUrl = configuracao.baseUrl,
       dio = dio ?? Dio() {
    this.dio.options
      ..baseUrl = baseUrl
      ..connectTimeout = tempoLimiteLigacao
      ..sendTimeout = tempoLimiteTransferencia
      ..receiveTimeout = tempoLimiteTransferencia
      ..contentType = Headers.jsonContentType
      ..responseType = ResponseType.json;
    this.dio.interceptors.addAll([authInterceptor, ErroInterceptor()]);
  }

  /// Tempo limite para estabelecer a ligação (modelo de classes, secção 6.1).
  static const Duration tempoLimiteLigacao = Duration(seconds: 8);

  /// Tempo limite para enviar ou receber uma resposta.
  static const Duration tempoLimiteTransferencia = Duration(seconds: 20);

  /// Endereço base (`<raiz>/api/v1`).
  final String baseUrl;

  /// Cliente Dio subjacente.
  final Dio dio;

  /// Pedido `GET`; devolve o corpo da resposta.
  Future<Object?> get(String caminho, {Map<String, Object?>? parametros}) =>
      _executar(() => dio.get<Object?>(caminho, queryParameters: parametros));

  /// Pedido `POST`; devolve o corpo da resposta (`null` em `204`).
  Future<Object?> post(String caminho, {Object? corpo}) =>
      _executar(() => dio.post<Object?>(caminho, data: corpo));

  /// Pedido `PUT`; devolve o corpo da resposta (`null` em `204`).
  Future<Object?> put(String caminho, {Object? corpo}) =>
      _executar(() => dio.put<Object?>(caminho, data: corpo));

  /// Pedido `DELETE`; devolve o corpo da resposta (`null` em `204`).
  Future<Object?> delete(String caminho) =>
      _executar(() => dio.delete<Object?>(caminho));

  Future<Object?> _executar(Future<Response<Object?>> Function() pedido) async {
    try {
      final resposta = await pedido();
      return resposta.data;
    } on DioException catch (e, pilha) {
      final erro = e.error;
      if (erro is Exception) {
        Error.throwWithStackTrace(erro, pilha);
      }
      Error.throwWithStackTrace(ErroInterceptor.converter(e), pilha);
    }
  }
}
