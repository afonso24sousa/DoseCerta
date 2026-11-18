import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../avisos_locais/avisos_locais_controller.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/estado_toma.dart';
import '../../models/problema_api.dart';
import '../../models/toma_dto.dart';

/// Estado do calendário de tomas de uma ficha.
class ListaTomasEstado {
  /// Cria o estado.
  const ListaTomasEstado({
    required this.inicio,
    this.tomas,
    this.aCarregar = false,
    this.erroCarregar,
    this.aConfirmar = const {},
    this.erro,
  });

  /// Primeiro dia do período (7 dias).
  final DateTime inicio;

  /// Último dia do período.
  DateTime get fim => DateTime(inicio.year, inicio.month, inicio.day + 6);

  /// Tomas do período (OP-32), por ordem da hora prevista; `null` antes da primeira leitura.
  final List<TomaDto>? tomas;

  /// Indica que a OP-32 está em curso.
  final bool aCarregar;

  /// Erro ao ler as tomas.
  final String? erroCarregar;

  /// Tomas com a confirmação (OP-33) em curso, cujo botão fica desativado.
  final Set<String> aConfirmar;

  /// Erro da última confirmação.
  final String? erro;

  /// Copia o estado, alterando os valores indicados. Os erros e o indicador de leitura não indicados são limpos.
  ListaTomasEstado copiar({
    DateTime? inicio,
    List<TomaDto>? tomas,
    bool aCarregar = false,
    String? erroCarregar,
    Set<String>? aConfirmar,
    String? erro,
  }) {
    return ListaTomasEstado(
      inicio: inicio ?? this.inicio,
      tomas: tomas ?? this.tomas,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      aConfirmar: aConfirmar ?? this.aConfirmar,
      erro: erro,
    );
  }
}

/// Controlador do calendário de tomas da ficha, no separador «Calendário de tomas» do E15 (OP-32, OP-33; RF-063 a
/// RF-065, RF-067, RF-068, RF-105). O servidor decide a janela e o estado da confirmação; o ecrã só mostra o
/// resultado.
class ListaTomasController extends Notifier<ListaTomasEstado> {
  /// Cria o controlador da ficha [fichaId].
  ListaTomasController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Mensagens das recusas da OP-33 neste ecrã (contrato de interface da F006, secção 6).
  static const Map<String, String> mensagens = {
    'TOMA_FORA_DA_JANELA':
        'A toma só pode ser confirmada a partir das 00:00 do dia previsto.',
    'TOMA_NAO_CONFIRMAVEL':
        'A toma ficou não confirmada e já não pode ser confirmada.',
  };

  @override
  ListaTomasEstado build() {
    // Por omissão, de ontem a hoje + 5 dias, no calendário de Lisboa: uma toma de ontem ainda pode ser confirmada
    // até 24 horas depois da hora prevista (RF-068).
    final hoje = FormatadorDataHora.diaDeLisboa(
      ref.read(relogioProvider).agora(),
    );
    Future.microtask(carregar);
    return ListaTomasEstado(
      inicio: DateTime(hoje.year, hoje.month, hoje.day - 1),
      aCarregar: true,
    );
  }

  /// OP-32: lê as tomas do período atual.
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true, erro: state.erro);
    try {
      final tomas = await ref
          .read(tomasRepositorioProvider)
          .listar(
            fichaId,
            FormatadorDataHora.paraIso(state.inicio),
            FormatadorDataHora.paraIso(state.fim),
          );
      if (!ref.mounted) {
        return;
      }
      final ordenadas = List.of(tomas)
        ..sort(
          (a, b) =>
              DateTime.parse(a.previstaEm)
                  .compareTo(DateTime.parse(b.previstaEm)),
        );
      state = state.copiar(tomas: ordenadas, erro: state.erro);
    } on Object catch (e) {
      if (!ref.mounted) {
        return;
      }
      state = state.copiar(erroCarregar: MensagensErro.traduzir(e));
    }
  }

  /// Ao atualizar o ecrã: lê as tomas de novo e sincroniza os avisos locais (RF-061).
  Future<void> atualizar() async {
    unawaited(ref.read(avisosLocaisControllerProvider.notifier).sincronizar());
    state = state.copiar(aCarregar: true);
    await carregar();
  }

  /// Período anterior (7 dias).
  Future<void> periodoAnterior() => _mudarPeriodo(-7);

  /// Período seguinte (7 dias).
  Future<void> periodoSeguinte() => _mudarPeriodo(7);

  Future<void> _mudarPeriodo(int dias) {
    final inicio = state.inicio;
    state = ListaTomasEstado(
      inicio: DateTime(inicio.year, inicio.month, inicio.day + dias),
      aCarregar: true,
    );
    return carregar();
  }

  /// OP-33: confirma a toma [tomaId] com um só toque (RNF-021). Com sucesso, substitui a toma pela resposta, sem
  /// ler a lista de novo (RNF-011), cancela o aviso local da toma e devolve o novo estado. Numa recusa (`409` ou
  /// `403`) mostra a mensagem e lê a lista de novo, para mostrar o estado atual da toma; devolve `null`.
  Future<EstadoToma?> confirmar(String tomaId) async {
    if (state.aConfirmar.contains(tomaId)) {
      return null;
    }
    state = state.copiar(aConfirmar: {...state.aConfirmar, tomaId});
    try {
      final confirmada = await ref
          .read(tomasRepositorioProvider)
          .confirmar(tomaId);
      if (!ref.mounted) {
        return confirmada.estado;
      }
      state = state.copiar(
        tomas: [
          for (final toma in state.tomas ?? const <TomaDto>[])
            toma.id == tomaId ? confirmada : toma,
        ],
        aConfirmar: {...state.aConfirmar}..remove(tomaId),
      );
      unawaited(
        ref.read(avisosLocaisControllerProvider.notifier).cancelarAviso(tomaId),
      );
      return confirmada.estado;
    } on Object catch (e) {
      if (!ref.mounted) {
        return null;
      }
      state = state.copiar(
        aConfirmar: {...state.aConfirmar}..remove(tomaId),
        erro: _mensagem(e),
      );
      if (e is ProblemaApi && (e.estadoHttp == 409 || e.estadoHttp == 403)) {
        await carregar();
      }
      return null;
    }
  }

  static String _mensagem(Object erro) {
    // A mensagem de TOMA_JA_CONFIRMADA vem do servidor, com quem confirmou e quando (RF-105).
    if (erro is ProblemaApi &&
        erro.codigo == 'TOMA_JA_CONFIRMADA' &&
        erro.mensagem.isNotEmpty) {
      return erro.mensagem;
    }
    return MensagensErro.traduzir(erro, especificas: mensagens);
  }
}

/// Controlador do calendário de tomas, por `fichaId`.
final listaTomasProvider = NotifierProvider.autoDispose
    .family<ListaTomasController, ListaTomasEstado, String>(
      ListaTomasController.new,
    );
