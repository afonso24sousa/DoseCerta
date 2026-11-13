import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Sessão guardada no dispositivo: token, instante de expiração (8 horas, RNF-009) e identificador do Utilizador.
class SessaoGuardada {
  /// Cria a sessão guardada.
  const SessaoGuardada({
    required this.token,
    required this.expiraEm,
    this.utilizadorId,
  });

  /// Token JWT.
  final String token;

  /// Instante de expiração devolvido por OP-02.
  final DateTime expiraEm;

  /// Identificador do Utilizador (`sub` do token); OP-02 não o devolve no corpo.
  final String? utilizadorId;
}

/// Guarda a sessão no armazenamento seguro do dispositivo (`flutter_secure_storage`). A palavra-passe nunca é
/// guardada.
class SessaoStore {
  /// Cria o armazenamento sobre [armazenamento].
  const SessaoStore(this.armazenamento);

  /// Chave do token.
  static const String chaveToken = 'sessao.token';

  /// Chave do instante de expiração (ISO 8601).
  static const String chaveExpiracao = 'sessao.expiraEm';

  /// Chave do identificador do Utilizador.
  static const String chaveUtilizador = 'sessao.utilizadorId';

  /// Armazenamento seguro.
  final FlutterSecureStorage armazenamento;

  /// Lê a sessão guardada; devolve `null` se não existir ou estiver incompleta.
  Future<SessaoGuardada?> ler() async {
    final token = await armazenamento.read(key: chaveToken);
    final expiracao = await armazenamento.read(key: chaveExpiracao);
    final expiraEm = expiracao == null ? null : DateTime.tryParse(expiracao);
    if (token == null || expiraEm == null) {
      return null;
    }
    return SessaoGuardada(
      token: token,
      expiraEm: expiraEm,
      utilizadorId: await armazenamento.read(key: chaveUtilizador),
    );
  }

  /// Guarda a sessão.
  Future<void> guardar(SessaoGuardada sessao) async {
    await armazenamento.write(key: chaveToken, value: sessao.token);
    await armazenamento.write(
      key: chaveExpiracao,
      value: sessao.expiraEm.toUtc().toIso8601String(),
    );
    if (sessao.utilizadorId == null) {
      await armazenamento.delete(key: chaveUtilizador);
    } else {
      await armazenamento.write(
        key: chaveUtilizador,
        value: sessao.utilizadorId,
      );
    }
  }

  /// Apaga a sessão.
  Future<void> limpar() async {
    await armazenamento.delete(key: chaveToken);
    await armazenamento.delete(key: chaveExpiracao);
    await armazenamento.delete(key: chaveUtilizador);
  }
}
