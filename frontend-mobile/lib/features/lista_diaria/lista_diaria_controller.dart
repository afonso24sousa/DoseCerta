import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../avisos_locais/avisos_locais_controller.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/estado_toma.dart';
import '../../models/item_lista_diaria_dto.dart';
import '../../models/problema_api.dart';
import '../../models/toma_dto.dart';
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
    DateTime? hoje,
    DiaLista? dia,
    List<ItemListaDiariaDto>? itens,
    bool aCarregar = false,
    String? erroCarregar,
    Set<String>? aConfirmar,
    String? erro,
  }) {
    return ListaDiariaEstado(
      hoje: hoje ?? this.hoje,
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
  /// Número da última leitura pedida: só a resposta da última conta (por exemplo, ao mudar de dia ou ao puxar para
  /// baixo duas vezes).
  int _ultimaLeitura = 0;

  /// Tomas confirmadas nesta sessão, com o número da última leitura pedida no momento da confirmação. Uma leitura
  /// que começou antes da confirmação pode trazer a toma ainda pendente; nesse caso, vale a confirmação.
  final Map<String, ({TomaDto toma, int leitura})> _confirmadas = {};

  @override
  ListaDiariaEstado build() {
    Future.microtask(carregar);
    return ListaDiariaEstado(hoje: _hojeEmLisboa(), aCarregar: true);
  }

  DateTime _hojeEmLisboa() =>
      FormatadorDataHora.diaDeLisboa(ref.read(relogioProvider).agora());

  /// OP-50: lê a lista do dia escolhido, num só pedido (RNF-010). Para hoje não envia a data: o servidor usa o dia
  /// de Lisboa e junta as tomas de ontem ainda pendentes (RF-100, RNF-047). O dia de hoje é lido de novo em cada
  /// leitura, para a lista não ficar no dia em que o ecrã abriu. Com [manterErro], a mensagem da última
  /// confirmação continua visível (é o caso da leitura pedida por uma recusa).
  Future<void> carregar({bool manterErro = true}) async {
    final leitura = ++_ultimaLeitura;
    state = state.copiar(
      hoje: _hojeEmLisboa(),
      aCarregar: true,
      erro: manterErro ? state.erro : null,
    );
    try {
      final itens = await ref
          .read(listaDiariaRepositorioProvider)
          .consultar(
            data: state.dia == DiaLista.hoje
                ? null
                : FormatadorDataHora.paraIso(state.data),
          );
      if (!ref.mounted || leitura != _ultimaLeitura) {
        return;
      }
      _confirmadas.removeWhere((_, c) => c.leitura < leitura);
      state = state.copiar(
        itens: [for (final item in itens) _comConfirmacao(item)],
        erro: state.erro,
      );
    } on Object catch (e) {
      if (!ref.mounted || leitura != _ultimaLeitura) {
        return;
      }
      state = state.copiar(
        erroCarregar: MensagensErro.traduzir(e),
        erro: state.erro,
      );
    }
  }

  /// Ao puxar para baixo ou ao voltar à aplicação: lê a lista de novo, com as confirmações dos outros Cuidadores
  /// (RF-104), e sincroniza as tomas das 48 horas seguintes para os avisos locais (RF-061).
  Future<void> atualizar() async {
    unawaited(ref.read(avisosLocaisControllerProvider.notifier).sincronizar());
    await carregar(manterErro: false);
  }

  /// Mostra o [dia] escolhido. As confirmações em curso continuam (o botão não volta a ficar ativo).
  Future<void> mudarDia(DiaLista dia) {
    if (dia == state.dia) {
      return Future.value();
    }
    state = ListaDiariaEstado(
      hoje: state.hoje,
      dia: dia,
      aCarregar: true,
      aConfirmar: state.aConfirmar,
    );
    return carregar();
  }

  /// OP-33: confirma a toma [tomaId] a partir da lista, sem abrir a ficha (RF-102), com um toque (RNF-021). Com
  /// sucesso, atualiza o item com a resposta, sem ler a lista de novo (RNF-011), cancela o aviso local da toma e
  /// devolve o novo estado (RF-064, RF-067). Numa recusa (`409`, `403` ou `404`) mostra a mensagem e lê a lista de
  /// novo, para mostrar o estado atual da toma (RF-105); devolve `null`. Uma leitura em curso não é interrompida.
  Future<EstadoToma?> confirmar(String tomaId) async {
    if (state.aConfirmar.contains(tomaId)) {
      return null;
    }
    state = _manterLeitura(aConfirmar: {...state.aConfirmar, tomaId});
    try {
      final toma = await ref.read(tomasRepositorioProvider).confirmar(tomaId);
      if (!ref.mounted) {
        return toma.estado;
      }
      _confirmadas[tomaId] = (toma: toma, leitura: _ultimaLeitura);
      final itens = state.itens;
      state = _manterLeitura(
        itens: itens == null
            ? null
            : [for (final item in itens) _comConfirmacao(item)],
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
      state = _manterLeitura(
        aConfirmar: {...state.aConfirmar}..remove(tomaId),
        erro: ListaTomasController.mensagemRecusa(e),
      );
      if (e is ProblemaApi &&
          (e.estadoHttp == 409 || e.estadoHttp == 403 || e.estadoHttp == 404)) {
        await carregar();
      }
      return null;
    }
  }

  /// Copia o estado sem mexer na leitura em curso nem no seu erro.
  ListaDiariaEstado _manterLeitura({
    List<ItemListaDiariaDto>? itens,
    required Set<String> aConfirmar,
    String? erro,
  }) {
    return state.copiar(
      itens: itens,
      aCarregar: state.aCarregar,
      erroCarregar: state.erroCarregar,
      aConfirmar: aConfirmar,
      erro: erro,
    );
  }

  /// O [item] com a confirmação feita nesta sessão, se a lista ainda o trouxer por confirmar.
  ItemListaDiariaDto _comConfirmacao(ItemListaDiariaDto item) {
    final confirmada = _confirmadas[item.id]?.toma;
    if (!item.eToma || confirmada == null || !item.confirmavel) {
      return item;
    }
    return item.confirmado(
      estado: confirmada.estado,
      confirmadaPor: confirmada.confirmadaPor?.nome,
      confirmadaEm: confirmada.confirmadaEm,
    );
  }
}

/// Controlador do ecrã E04.
final listaDiariaControllerProvider =
    NotifierProvider.autoDispose<ListaDiariaController, ListaDiariaEstado>(
      ListaDiariaController.new,
    );
