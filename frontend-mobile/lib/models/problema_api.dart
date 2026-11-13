import 'erro_campo.dart';

/// Corpo de erro devolvido pela API: código, mensagem e, nos erros de validação, os campos
/// (documentação da API, secção 3.3). O estado HTTP vem da resposta.
///
/// O campo `confirmacao` (só em `TOMA_JA_CONFIRMADA`, OP-33) será acrescentado com a F006.
class ProblemaApi implements Exception {
  /// Cria um problema da API.
  const ProblemaApi({
    required this.codigo,
    required this.mensagem,
    this.campos = const [],
    this.estadoHttp,
  });

  /// Lê o corpo de erro. Se o corpo não tiver o formato da API (por exemplo, uma página de erro do
  /// servidor intermédio), devolve `ERRO_INTERNO` com a mensagem genérica.
  factory ProblemaApi.daResposta(Object? corpo, {int? estadoHttp}) {
    if (corpo is Map<String, dynamic> && corpo['codigo'] is String) {
      final campos = corpo['campos'];
      return ProblemaApi(
        codigo: corpo['codigo'] as String,
        mensagem: corpo['mensagem'] as String? ?? '',
        campos: campos is List
            ? campos
                  .whereType<Map<String, dynamic>>()
                  .map(ErroCampo.fromJson)
                  .toList(growable: false)
            : const [],
        estadoHttp: estadoHttp,
      );
    }
    return ProblemaApi(
      codigo: codigoErroInterno,
      mensagem: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
      estadoHttp: estadoHttp,
    );
  }

  /// Código de erro usado quando a resposta não tem o formato da API.
  static const String codigoErroInterno = 'ERRO_INTERNO';

  /// Código de erro estável da API (por exemplo `EMAIL_JA_REGISTADO`).
  final String codigo;

  /// Mensagem em português europeu devolvida pelo servidor.
  final String mensagem;

  /// Campos em erro; vazio quando o erro não é de validação.
  final List<ErroCampo> campos;

  /// Código HTTP da resposta (400, 401, 403, 404, 409, ...).
  final int? estadoHttp;

  @override
  String toString() => 'ProblemaApi($estadoHttp $codigo: $mensagem)';
}
