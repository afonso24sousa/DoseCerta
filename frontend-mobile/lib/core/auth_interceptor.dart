import 'package:dio/dio.dart';

/// Junta `Authorization: Bearer <token>` a cada pedido (RNF-054), exceto ao registo (OP-01) e ao início de sessão
/// (OP-02), que são públicos (RF-017). Perante `401` num pedido autenticado, termina a sessão local, o que leva a
/// aplicação ao início de sessão (RF-017, RNF-009).
class AuthInterceptor extends Interceptor {
  /// Cria o interceptor.
  ///
  /// [obterToken] devolve o token da sessão atual (ou `null`); [aoRecusarSessao] é chamado quando o servidor
  /// responde `401` a um pedido autenticado.
  AuthInterceptor({required this.obterToken, required this.aoRecusarSessao});

  /// Caminhos que não exigem sessão (OP-01 e OP-02).
  static const Set<String> caminhosPublicos = {'/auth/registo', '/auth/login'};

  /// Devolve o token da sessão atual.
  final String? Function() obterToken;

  /// Termina a sessão local.
  final void Function() aoRecusarSessao;

  /// Indica se o pedido é público. O `401` de OP-02 significa credenciais inválidas, não sessão terminada.
  static bool ePublico(RequestOptions pedido) =>
      caminhosPublicos.contains(pedido.path);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (!ePublico(options)) {
      final token = obterToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.response?.statusCode == 401 && !ePublico(err.requestOptions)) {
      aoRecusarSessao();
    }
    handler.next(err);
  }
}
