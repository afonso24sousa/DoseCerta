import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatador_data_hora.dart';
import '../../core/provedores.dart';
import '../../models/estado_ficha.dart';
import '../../models/ficha_resumo_dto.dart';
import '../../models/item_agenda_dto.dart';
import '../../models/papel.dart';
import '../../models/vista_agenda.dart';

/// Vista e dia de referência da agenda (E21). O dia é o de Lisboa, o calendário do servidor (RNF-047).
typedef PeriodoAgenda = ({VistaAgenda vista, DateTime data});

/// Controlador de E21 (OP-45; RF-084 a RF-086): guarda a vista e a data; os eventos vêm de [itensAgendaProvider].
class AgendaController extends Notifier<PeriodoAgenda> {
  static const List<String> _meses = [
    'janeiro',
    'fevereiro',
    'março',
    'abril',
    'maio',
    'junho',
    'julho',
    'agosto',
    'setembro',
    'outubro',
    'novembro',
    'dezembro',
  ];

  @override
  PeriodoAgenda build() => (vista: VistaAgenda.dia, data: _hoje());

  /// Muda a vista, mantendo a data.
  void mudarVista(VistaAgenda vista) =>
      state = (vista: vista, data: state.data);

  /// Avança ([sentido] 1) ou recua ([sentido] -1) um dia, uma semana ou um mês, conforme a vista.
  void mudarData(int sentido) => state = deslocar(state, sentido);

  /// Volta ao dia de hoje.
  void irParaHoje() => state = (vista: state.vista, data: _hoje());

  DateTime _hoje() =>
      FormatadorDataHora.diaDeLisboa(ref.read(relogioProvider).agora());

  /// Período anterior ou seguinte ao [periodo].
  static PeriodoAgenda deslocar(PeriodoAgenda periodo, int sentido) {
    final d = periodo.data;
    final data = switch (periodo.vista) {
      VistaAgenda.dia => DateTime(d.year, d.month, d.day + sentido),
      VistaAgenda.semana => DateTime(d.year, d.month, d.day + 7 * sentido),
      VistaAgenda.mes => DateTime(d.year, d.month + sentido),
    };
    return (vista: periodo.vista, data: data);
  }

  /// Segunda-feira da semana que contém [data] (RF-085).
  static DateTime inicioSemana(DateTime data) =>
      DateTime(data.year, data.month, data.day - (data.weekday - 1));

  /// Texto do período: o dia, a semana de segunda a domingo ou o mês (RNF-029).
  static String rotulo(PeriodoAgenda periodo) {
    final d = periodo.data;
    switch (periodo.vista) {
      case VistaAgenda.dia:
        return FormatadorDataHora.deData(d);
      case VistaAgenda.semana:
        final inicio = inicioSemana(d);
        final fim = DateTime(inicio.year, inicio.month, inicio.day + 6);
        return '${FormatadorDataHora.deData(inicio)} a '
            '${FormatadorDataHora.deData(fim)}';
      case VistaAgenda.mes:
        return '${_meses[d.month - 1]} de ${d.year}';
    }
  }
}

/// Fornecedor do controlador de E21.
final agendaControllerProvider =
    NotifierProvider.autoDispose<AgendaController, PeriodoAgenda>(
      AgendaController.new,
    );

/// Eventos do período (OP-45), pela ordem do servidor. Registar, editar ou remover um compromisso invalida este
/// fornecedor, para a agenda voltar a ser pedida.
final itensAgendaProvider = FutureProvider.autoDispose
    .family<List<ItemAgendaDto>, PeriodoAgenda>(
      (ref, periodo) => ref
          .read(agendaRepositorioProvider)
          .consultar(periodo.vista, FormatadorDataHora.paraIso(periodo.data)),
    );

/// Fichas ativas em que o Utilizador é Cuidador, as únicas onde pode registar compromissos (RF-080, RF-121).
final fichasComRegistoProvider =
    FutureProvider.autoDispose<List<FichaResumoDto>>((ref) async {
      final fichas = await ref
          .read(fichasRepositorioProvider)
          .listar(estado: EstadoFicha.ativa);
      return [
        for (final ficha in fichas)
          if (ficha.meuPapel == Papel.cuidador) ficha,
      ];
    });
