import 'dart:typed_data';

import 'package:dio/dio.dart';

import 'auth_interceptor.dart';
import 'configuracao_api.dart';
import 'erro_interceptor.dart';
import 'parte_ficheiro.dart';

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

  /// Tempo limite para enviar ou receber um ficheiro de até 10 MB (RNF-008) numa rede lenta. O RNF-014 pede um
  /// documento de 5 MB guardado em 15 segundos nas condições de referência; este limite só evita esperas sem fim.
  static const Duration tempoLimiteFicheiros = Duration(seconds: 90);

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

  /// Pedido `multipart/form-data` com [metodo] `POST` ou `PUT` (documentação da API, secção 3.6); devolve o corpo
  /// da resposta.
  ///
  /// Os [campos] de texto e os [ficheiros] seguem pela ordem das listas: o servidor emparelha as partes
  /// `documento` e `categoria` pela ordem (a primeira categoria é a do primeiro documento).
  Future<Object?> enviarMultipart(
    String metodo,
    String caminho, {
    List<MapEntry<String, String>> campos = const [],
    List<ParteFicheiro> ficheiros = const [],
  }) async {
    assert(
      metodo == 'POST' || metodo == 'PUT',
      'Só POST e PUT enviam ficheiros.',
    );
    final formulario = FormData()..fields.addAll(campos);
    for (final parte in ficheiros) {
      formulario.files.add(
        MapEntry(parte.nomeParte, await parte.paraMultipart()),
      );
    }
    return _executar(
      () => dio.request<Object?>(
        caminho,
        data: formulario,
        options: Options(
          method: metodo,
          sendTimeout: tempoLimiteFicheiros,
          receiveTimeout: tempoLimiteFicheiros,
        ),
      ),
    );
  }

  /// Pedido `GET` de um ficheiro (OP-17); devolve o conteúdo em bytes.
  Future<Uint8List> obterBytes(String caminho) async {
    final corpo = await _executar(
      () => dio.get<Object?>(
        caminho,
        options: Options(
          responseType: ResponseType.bytes,
          receiveTimeout: tempoLimiteFicheiros,
        ),
      ),
    );
    return Uint8List.fromList(corpo! as List<int>);
  }

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
