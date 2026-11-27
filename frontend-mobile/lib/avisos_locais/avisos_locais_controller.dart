import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/provedores.dart';
import '../core/sessao_controller.dart';
import 'sincronizador_calendario.dart';
import 'tomas_sincronizadas_store.dart';

/// Estado dos avisos locais: autorizações e última sincronização.
class AvisosLocaisEstado {
  /// Cria o estado.
  const AvisosLocaisEstado({
    this.notificacoesAutorizadas,
    this.alarmesExatosAutorizados,
    this.avisoFechado = false,
    this.aSincronizar = false,
    this.ultimoResultado,
    this.sincronizadas,
  });

  /// Notificações autorizadas; `null` enquanto não se sabe.
  final bool? notificacoesAutorizadas;

  /// Alarmes exatos autorizados; `null` enquanto não se sabe.
  final bool? alarmesExatosAutorizados;

  /// O Utilizador fechou o aviso das autorizações nesta abertura da aplicação.
  final bool avisoFechado;

  /// Há uma sincronização em curso.
  final bool aSincronizar;

  /// Resultado da última sincronização.
  final ResultadoSincronizacao? ultimoResultado;

  /// Tomas guardadas neste dispositivo e instante da última sincronização bem-sucedida (E26; RF-061); `null` se
  /// ainda não houve nenhuma.
  final TomasSincronizadas? sincronizadas;

  /// Mostrar o aviso de notificações desativadas (RF-062).
  bool get mostrarNotificacoesDesativadas =>
      !avisoFechado && notificacoesAutorizadas == false;

  /// Mostrar o aviso de alarmes exatos não autorizados (RNF-041): só quando as notificações já estão autorizadas.
  bool get mostrarAlarmesExatos =>
      !avisoFechado &&
      notificacoesAutorizadas == true &&
      alarmesExatosAutorizados == false;

  /// Devolve uma cópia com os campos indicados alterados.
  AvisosLocaisEstado copiar({
    bool? notificacoesAutorizadas,
    bool? alarmesExatosAutorizados,
    bool? avisoFechado,
    bool? aSincronizar,
    ResultadoSincronizacao? ultimoResultado,
  }) {
    return AvisosLocaisEstado(
      notificacoesAutorizadas:
          notificacoesAutorizadas ?? this.notificacoesAutorizadas,
      alarmesExatosAutorizados:
          alarmesExatosAutorizados ?? this.alarmesExatosAutorizados,
      avisoFechado: avisoFechado ?? this.avisoFechado,
      aSincronizar: aSincronizar ?? this.aSincronizar,
      ultimoResultado: ultimoResultado ?? this.ultimoResultado,
      // Mantém as tomas lidas; só `comSincronizadas` as muda.
      sincronizadas: sincronizadas,
    );
  }

  /// Devolve uma cópia com as [sincronizadas] lidas do dispositivo (podem ser `null`, depois de terminar a sessão).
  AvisosLocaisEstado comSincronizadas(TomasSincronizadas? sincronizadas) {
    return AvisosLocaisEstado(
      notificacoesAutorizadas: notificacoesAutorizadas,
      alarmesExatosAutorizados: alarmesExatosAutorizados,
      avisoFechado: avisoFechado,
      aSincronizar: aSincronizar,
      ultimoResultado: ultimoResultado,
      sincronizadas: sincronizadas,
    );
  }
}

/// Avisos locais da aplicação (modelo de classes, secção 7; RF-061, RF-062): sincroniza ao abrir a aplicação, ao
/// iniciar sessão, ao voltar à aplicação e ao atualizar o calendário; cancela todos os avisos quando o Utilizador
/// termina a sessão. Nunca pede autorizações sem um toque do Utilizador.
class AvisosLocaisController extends Notifier<AvisosLocaisEstado> {
  @override
  AvisosLocaisEstado build() {
    ref.listen(sessaoControllerProvider, (anterior, atual) {
      if (atual.autenticada && anterior?.utilizadorId != atual.utilizadorId) {
        unawaited(sincronizar());
      } else if (!atual.autenticada &&
          (anterior?.autenticada ?? false) &&
          atual.motivoFim == MotivoFimSessao.terminada) {
        unawaited(ref.read(sincronizadorCalendarioProvider).terminarSessao());
      }
    });
    Future.microtask(aoAbrir);
    return const AvisosLocaisEstado();
  }

  /// Ao abrir ou voltar à aplicação: lê as autorizações (podem ter mudado nas definições) e sincroniza.
  Future<void> aoAbrir() async {
    await carregarEstado();
    await sincronizar();
  }

  /// Lê o estado das autorizações. Não faz nada se o controlador já tiver sido descartado (por exemplo, a aplicação
  /// foi fechada durante um `await` anterior).
  Future<void> carregarEstado() async {
    if (!ref.mounted) {
      return;
    }
    final permissao = ref.read(permissaoNotificacoesProvider);
    final notificacoes = await _semFalhar(permissao.estado);
    final alarmes = await _semFalhar(permissao.estadoAlarmesExatos);
    if (!ref.mounted) {
      return;
    }
    state = state.copiar(
      notificacoesAutorizadas: notificacoes,
      alarmesExatosAutorizados: alarmes,
    );
    await _lerSincronizadas();
  }

  /// Lê do dispositivo as tomas guardadas e o instante da última sincronização, para o E26.
  Future<void> _lerSincronizadas() async {
    TomasSincronizadas? lidas;
    try {
      lidas = await ref.read(tomasSincronizadasStoreProvider).ler();
    } on Object {
      lidas = null;
    }
    if (ref.mounted) {
      state = state.comSincronizadas(lidas);
    }
  }

  /// Sincroniza as tomas do Utilizador com sessão; sem sessão, ou com o controlador já descartado, não faz nada.
  Future<void> sincronizar() async {
    if (!ref.mounted) {
      return;
    }
    final sessao = ref.read(sessaoControllerProvider);
    final utilizadorId = sessao.utilizadorId;
    if (!sessao.autenticada || utilizadorId == null || state.aSincronizar) {
      return;
    }
    state = state.copiar(aSincronizar: true);
    final resultado = await ref
        .read(sincronizadorCalendarioProvider)
        .sincronizar(utilizadorId);
    if (!ref.mounted) {
      return;
    }
    state = state.copiar(aSincronizar: false, ultimoResultado: resultado);
    await _lerSincronizadas();
  }

  /// Pede as autorizações em falta, a pedido do Utilizador: primeiro as notificações e depois os alarmes exatos.
  /// No fim volta a sincronizar, para os avisos ficarem agendados no modo exato.
  Future<void> pedirPermissao() async {
    final permissao = ref.read(permissaoNotificacoesProvider);
    if (state.notificacoesAutorizadas != true) {
      final autorizadas = await _semFalhar(permissao.pedir);
      if (autorizadas != true) {
        await carregarEstado();
        return;
      }
    }
    if (await _semFalhar(permissao.estadoAlarmesExatos) == false) {
      await _semFalhar(permissao.pedirAlarmesExatos);
    }
    await carregarEstado();
    await sincronizar();
  }

  /// Fecha o aviso das autorizações até à próxima abertura da aplicação.
  void fecharAviso() => state = state.copiar(avisoFechado: true);

  /// Cancela o aviso da toma [tomaId], depois de confirmada (protótipo, nota do E18), e retira-a das tomas guardadas,
  /// para as contagens do E26 ficarem certas logo.
  Future<void> cancelarAviso(String tomaId) async {
    try {
      await ref.read(avisosLocaisServiceProvider).cancelarAviso(tomaId);
      await ref.read(tomasSincronizadasStoreProvider).retirar(tomaId);
    } on Object {
      // O aviso desaparece na sincronização seguinte, porque a OP-34 já não devolve a toma confirmada.
    }
    await _lerSincronizadas();
  }

  static Future<bool?> _semFalhar(Future<bool?> Function() pedido) async {
    try {
      return await pedido();
    } on Object {
      return null;
    }
  }
}

/// Avisos locais da aplicação.
final avisosLocaisControllerProvider =
    NotifierProvider<AvisosLocaisController, AvisosLocaisEstado>(
      AvisosLocaisController.new,
    );
