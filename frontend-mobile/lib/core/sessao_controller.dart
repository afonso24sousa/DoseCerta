import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/problema_api.dart';
import 'jwt.dart';
import 'provedores.dart';
import 'sessao_store.dart';

/// Motivo do fim da sessão, para o início de sessão (E02) mostrar a mensagem certa.
enum MotivoFimSessao {
  /// O token expirou (8 horas, RNF-009) ou o servidor recusou-o com `401`.
  expirada,

  /// O Utilizador terminou a sessão em E03 (RF-016).
  terminada,
}

/// Estado de sessão da aplicação.
class SessaoEstado {
  const SessaoEstado._({
    this.token,
    this.utilizadorId,
    this.expiraEm,
    this.motivoFim,
  });

  /// Sem sessão; [motivo] indica porque terminou, se terminou.
  const SessaoEstado.semSessao({MotivoFimSessao? motivo})
    : this._(motivoFim: motivo);

  /// Sessão iniciada.
  SessaoEstado.autenticada(SessaoGuardada sessao)
    : this._(
        token: sessao.token,
        utilizadorId: sessao.utilizadorId,
        expiraEm: sessao.expiraEm,
      );

  /// Token JWT da sessão atual.
  final String? token;

  /// Identificador do Utilizador.
  final String? utilizadorId;

  /// Instante de expiração da sessão.
  final DateTime? expiraEm;

  /// Motivo do fim da sessão anterior.
  final MotivoFimSessao? motivoFim;

  /// Indica se há sessão iniciada.
  bool get autenticada => token != null;
}

/// Estado de sessão (`Notifier<SessaoEstado>`): início e fim de sessão, restauro ao abrir a aplicação e fim de
/// sessão ao expirar (RF-012, RF-016, RF-017, RNF-009). A sessão não é renovada.
class SessaoController extends Notifier<SessaoEstado> {
  Timer? _temporizador;

  @override
  SessaoEstado build() {
    ref.onDispose(() => _temporizador?.cancel());
    return restaurar(ref.read(sessaoInicialProvider));
  }

  /// Restaura a sessão lida do dispositivo ao abrir a aplicação, sem pedidos de rede (RNF-016). Uma sessão já
  /// expirada é apagada e leva ao início de sessão com a mensagem de sessão terminada.
  SessaoEstado restaurar(SessaoGuardada? guardada) {
    if (guardada == null) {
      return const SessaoEstado.semSessao();
    }
    if (!ref.read(relogioProvider).agora().isBefore(guardada.expiraEm)) {
      unawaited(ref.read(sessaoStoreProvider).limpar());
      return const SessaoEstado.semSessao(motivo: MotivoFimSessao.expirada);
    }
    _agendarExpiracao(guardada.expiraEm);
    return SessaoEstado.autenticada(guardada);
  }

  /// OP-02: inicia sessão e guarda o token e o instante de expiração. Lança `ProblemaApi` ou `SemLigacaoErro`.
  Future<void> iniciarSessao(String email, String palavraPasse) async {
    final dto = await ref
        .read(authRepositorioProvider)
        .iniciarSessao(email, palavraPasse);
    final sessao = SessaoGuardada(
      token: dto.token,
      expiraEm: DateTime.parse(dto.expiraEm),
      utilizadorId: lerSujeitoDoToken(dto.token),
    );
    await ref.read(sessaoStoreProvider).guardar(sessao);
    _agendarExpiracao(sessao.expiraEm);
    state = SessaoEstado.autenticada(sessao);
  }

  /// OP-03: termina a sessão no servidor e depois no dispositivo (RF-016). Sem ligação, lança `SemLigacaoErro` e
  /// a sessão mantém-se, porque o servidor ainda aceitaria o token (RNF-033). Se o servidor já não aceitar o
  /// token (`401`), a sessão local termina na mesma.
  Future<void> terminarSessao() async {
    try {
      await ref.read(authRepositorioProvider).terminarSessao();
    } on ProblemaApi catch (e) {
      if (e.estadoHttp != 401) {
        rethrow;
      }
    }
    await _terminarLocalmente(MotivoFimSessao.terminada);
  }

  /// Termina a sessão local quando expira ou quando o servidor responde `401` (RF-017, RNF-009).
  void sessaoExpirada() {
    if (state.autenticada) {
      unawaited(_terminarLocalmente(MotivoFimSessao.expirada));
    }
  }

  Future<void> _terminarLocalmente(MotivoFimSessao motivo) async {
    _temporizador?.cancel();
    state = SessaoEstado.semSessao(motivo: motivo);
    await ref.read(sessaoStoreProvider).limpar();
  }

  void _agendarExpiracao(DateTime expiraEm) {
    _temporizador?.cancel();
    final falta = expiraEm.difference(ref.read(relogioProvider).agora());
    _temporizador = Timer(
      falta.isNegative ? Duration.zero : falta,
      sessaoExpirada,
    );
  }
}
