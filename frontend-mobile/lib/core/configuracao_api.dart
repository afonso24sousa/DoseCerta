import 'package:flutter/foundation.dart';

/// Endereço da API, indicado na compilação com `--dart-define=URL_API=<raiz do servidor>` (o mesmo nome usado
/// pela frontend-mobile-ci). Sem `URL_API`, usa o ambiente de demonstração público.
///
/// Só aceita `https://` (RNF-002). A única exceção é `http://10.0.2.2` (a API local vista a partir do emulador
/// Android) e só em builds debug; o Android aplica a mesma regra em `network_security_config.xml`.
class ConfiguracaoApi {
  /// Cria a configuração a partir de um endereço já validado.
  const ConfiguracaoApi._(this.raiz);

  /// Lê `URL_API` e valida-o para o modo de compilação atual.
  factory ConfiguracaoApi.doAmbiente() {
    const definido = String.fromEnvironment('URL_API');
    return ConfiguracaoApi.validar(
      definido.isEmpty ? urlPorOmissao : definido,
      modoDebug: kDebugMode,
    );
  }

  /// Valida o endereço: HTTPS, ou HTTP só para o emulador e só em debug.
  ///
  /// Lança [ArgumentError] se o endereço não for aceite, para a aplicação não arrancar com uma configuração
  /// que enviaria dados em claro.
  factory ConfiguracaoApi.validar(String url, {required bool modoDebug}) {
    final uri = Uri.tryParse(url.trim());
    if (uri == null || !uri.hasScheme || uri.host.isEmpty) {
      throw ArgumentError.value(url, 'URL_API', 'Endereço da API inválido');
    }
    final https = uri.scheme == 'https';
    final emuladorEmDebug =
        modoDebug && uri.scheme == 'http' && uri.host == hostEmulador;
    if (!https && !emuladorEmDebug) {
      throw ArgumentError.value(
        url,
        'URL_API',
        'A API só é acessível por HTTPS (RNF-002)',
      );
    }
    return ConfiguracaoApi._(uri);
  }

  /// Ambiente de demonstração público (I040).
  static const String urlPorOmissao =
      'https://dosecerta.spaincentral.cloudapp.azure.com';

  /// Endereço do computador anfitrião visto a partir do emulador Android.
  static const String hostEmulador = '10.0.2.2';

  /// Raiz do servidor.
  final Uri raiz;

  /// Endereço base das operações da API (`<raiz>/api/v1`).
  String get baseUrl {
    final texto = raiz.toString();
    final semBarra = texto.endsWith('/')
        ? texto.substring(0, texto.length - 1)
        : texto;
    return '$semBarra/api/v1';
  }
}
