import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../avisos_locais/avisos_locais_controller.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/estado_toma.dart';
import '../../models/item_lista_diaria_dto.dart';
import '../../models/problema_api.dart';
import '../medicacao/lista_tomas_controller.dart';

/// Dia mostrado na lista (protótipo, E04: «Ontem», «Hoje», «Amanhã»).
enum DiaLista {
  /// Ontem.
  ontem(-1, 'Ontem'),

  /// Hoje (por omissão).
  hoje(0, 'Hoje'),

  /// Amanhã.
  amanha(1, 'Amanhã');

  const DiaLista(this.desvio, this.rotulo);

  /// Dias em relação a hoje.
  final int desvio;

  /// Texto do botão.
  final String rotulo;
}

/// Estado do ecrã E04 Lista de tarefas diária.
class ListaDiariaEstado {
  /// Cria o estado.
  const ListaDiariaEstado({
    required this.hoje,
    this.dia = DiaLista.hoje,
    this.itens,
    this.aCarregar = false,
    this.erroCarregar,
    this.aConfirmar = const {},
    this.erro,
  });

  /// Dia de hoje no calendário de Lisboa.
  final DateTime hoje;

  /// Dia escolhido.
  final DiaLista dia;

  /// Data do dia escolhido, no calendário de Lisboa.
  DateTime get data => DateTime(hoje.year, hoje.month, hoje.day + dia.desvio);

  /// Itens do dia (OP-50), pela ordem do servidor; `null` antes da primeira leitura.
  final List<ItemListaDiariaDto>? itens;

  /// Indica que a OP-50 está em curso.
  final bool aCarregar;

  /// Erro ao ler a lista.
  final String? erroCarregar;

  /// Tomas com a confirmação (OP-33) em curso, cujo botão fica desativado.
  final Set<String> aConfirmar;

  /// Erro da última confirmação.
  final String? erro;

  /// Copia o estado, alterando os valores indicados. Os erros e o indicador de leitura não indicados são limpos.
  ListaDiariaEstado copiar({
    DiaLista? dia,
    List<ItemListaDiariaDto>? itens,
    bool aCarregar = false,
    String? erroCarregar,
    Set<String>? aConfirmar,
    String? erro,
  }) {
    return ListaDiariaEstado(
      hoje: hoje,
      dia: dia ?? this.dia,
      itens: itens ?? this.itens,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      aConfirmar: aConfirmar ?? this.aConfirmar,
      erro: erro,
    );
  }
}

/// Controlador do ecrã E04 Lista de tarefas diária (OP-50, OP-33; RF-061, RF-064, RF-067, RF-087, RF-100 a
/// RF-102, RF-104, RF-105, RNF-010, RNF-011, RNF-021). O servidor escolhe os itens e decide o que é confirmável; o
/// ecrã só mostra o resultado.
class ListaDiariaController extends Notifier<ListaDiariaEstado> {
  @override
  ListaDiariaEstado build() {
    final hoje = FormatadorDataHora.diaDeLisboa(
      ref.read(relogioProvider).agora(),
    );
    Future.microtask(carregar);
    return ListaDiariaEstado(hoje: hoje, aCarregar: true);
  }

  /// OP-50: lê a lista do dia escolhido, num só pedido (RNF-010). Para hoje não envia a data: o servidor usa o dia
  /// de Lisboa e junta as tomas de ontem ainda pendentes (RF-100, RNF-047).
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true, erro: state.erro);
    final dia = state.dia;
    try {
      final itens = await ref
          .read(listaDiariaRepositorioProvider)
          .consultar(
            data: dia == DiaLista.hoje
                ? null
                : FormatadorDataHora.paraIso(state.data),
          );
      if (!ref.mounted || state.dia != dia) {
        return;
      }
      state = state.copiar(itens: itens, erro: state.erro);
    } on Object catch (e) {
      if (!ref.mounted || state.dia != dia) {
        return;
      }
      state = state.copiar(erroCarregar: MensagensErro.traduzir(e));
    }
  }

  /// Ao puxar para baixo: lê a lista de novo, com as confirmações dos outros Cuidadores (RF-104), e sincroniza as
  /// tomas das 48 horas seguintes para os avisos locais (RF-061).
  Future<void> atualizar() async {
    unawaited(ref.read(avisosLocaisControllerProvider.notifier).sincronizar());
    await carregar();
  }

  /// Mostra o [dia] escolhido.
  Future<void> mudarDia(DiaLista dia) {
    if (dia == state.dia) {
      return Future.value();
    }
    state = ListaDiariaEstado(hoje: state.hoje, dia: dia, aCarregar: true);
    return carregar();
  }

  /// OP-33: confirma a toma [tomaId] a partir da lista, sem abrir a ficha (RF-102), com um toque (RNF-021). Com
  /// sucesso, atualiza o item com a resposta, sem ler a lista de novo (RNF-011), cancela o aviso local da toma e
  /// devolve o novo estado (RF-064, RF-067). Numa recusa (`409` ou `403`) mostra a mensagem e lê a lista de novo,
  /// para mostrar o estado atual da toma (RF-105); devolve `null`.
  Future<EstadoToma?> confirmar(String tomaId) async {
    if (state.aConfirmar.contains(tomaId)) {
      return null;
    }
    state = state.copiar(aConfirmar: {...state.aConfirmar, tomaId});
    try {
      final toma = await ref.read(tomasRepositorioProvider).confirmar(tomaId);
      if (!ref.mounted) {
        return toma.estado;
      }
      state = state.copiar(
        itens: [
          for (final item in state.itens ?? const <ItemListaDiariaDto>[])
            item.eToma && item.id == tomaId
                ? item.confirmado(
                    estado: toma.estado,
                    confirmadaPor: toma.confirmadaPor?.nome,
                    confirmadaEm: toma.confirmadaEm,
                  )
                : item,
        ],
        aConfirmar: {...state.aConfirmar}..remove(tomaId),
      );
      unawaited(
        ref.read(avisosLocaisControllerProvider.notifier).cancelarAviso(tomaId),
      );
      return toma.estado;
    } on Object catch (e) {
      if (!ref.mounted) {
        return null;
      }
      state = state.copiar(
        aConfirmar: {...state.aConfirmar}..remove(tomaId),
        erro: ListaTomasController.mensagemRecusa(e),
      );
      if (e is ProblemaApi && (e.estadoHttp == 409 || e.estadoHttp == 403)) {
        await carregar();
      }
      return null;
    }
  }
}

/// Controlador do ecrã E04.
final listaDiariaControllerProvider =
    NotifierProvider.autoDispose<ListaDiariaController, ListaDiariaEstado>(
      ListaDiariaController.new,
    );
