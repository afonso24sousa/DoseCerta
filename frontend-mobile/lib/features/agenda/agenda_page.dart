import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../models/ficha_resumo_dto.dart';
import '../../models/item_agenda_dto.dart';
import '../../models/tipo_item_agenda.dart';
import '../../models/vista_agenda.dart';
import '../../shared/banner_erro.dart';
import '../medicacao/apresentacao_toma.dart';
import 'agenda_controller.dart';

/// Ecrã E21 Agenda: compromissos e tomas previstas das fichas ativas do Utilizador, nas vistas dia, semana e mês,
/// com o nome do utente em cada evento (RF-083 a RF-089). Os compromissos abrem E22 (o Familiar só consulta); as
/// tomas não se alteram nem confirmam a partir da agenda (RF-087, RF-088).
class AgendaPage extends ConsumerWidget {
  /// Cria o ecrã.
  const AgendaPage({super.key});

  Future<void> _registar(
    BuildContext context,
    List<FichaResumoDto> fichas,
  ) async {
    final ficha = fichas.length == 1
        ? fichas.single
        : await showModalBottomSheet<FichaResumoDto>(
            context: context,
            showDragHandle: true,
            builder: (context) => SafeArea(
              child: ListView(
                shrinkWrap: true,
                children: [
                  const ListTile(title: Text('Escolha a ficha')),
                  for (final f in fichas)
                    ListTile(
                      leading: const Icon(Icons.person_outline),
                      title: Text(f.nomeCompleto),
                      onTap: () => Navigator.of(context).pop(f),
                    ),
                ],
              ),
            ),
          );
    if (ficha != null && context.mounted) {
      await context.push(AppRouter.novoCompromisso(ficha.id));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final periodo = ref.watch(agendaControllerProvider);
    final controlador = ref.read(agendaControllerProvider.notifier);
    final itens = ref.watch(itensAgendaProvider(periodo));
    final fichas = ref.watch(fichasComRegistoProvider).value ?? const [];
    Future<void> recarregar() =>
        ref.refresh(itensAgendaProvider(periodo).future);

    final List<Widget> conteudo;
    switch (itens) {
      case AsyncData(:final value) when value.isEmpty:
        conteudo = const [Text('Não há eventos neste período.')];
      case AsyncData(:final value):
        conteudo = _porDia(value, comDia: periodo.vista != VistaAgenda.dia);
      case AsyncError(:final error):
        conteudo = [
          BannerErro(MensagensErro.traduzir(error)),
          OutlinedButton(
            onPressed: recarregar,
            child: const Text('Tentar novamente'),
          ),
        ];
      default:
        conteudo = const [LinearProgressIndicator()];
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Agenda')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: recarregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              SegmentedButton<VistaAgenda>(
                segments: [
                  for (final vista in VistaAgenda.values)
                    ButtonSegment(value: vista, label: Text(vista.rotulo)),
                ],
                selected: {periodo.vista},
                showSelectedIcon: false,
                onSelectionChanged: (escolha) =>
                    controlador.mudarVista(escolha.single),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  IconButton(
                    tooltip: 'Período anterior',
                    icon: const Icon(Icons.chevron_left),
                    onPressed: () => controlador.mudarData(-1),
                  ),
                  Expanded(
                    child: Semantics(
                      header: true,
                      child: Text(
                        AgendaController.rotulo(periodo),
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Período seguinte',
                    icon: const Icon(Icons.chevron_right),
                    onPressed: () => controlador.mudarData(1),
                  ),
                ],
              ),
              Align(
                child: TextButton(
                  onPressed: controlador.irParaHoje,
                  child: const Text('Hoje'),
                ),
              ),
              if (fichas.isNotEmpty) ...[
                FilledButton.icon(
                  onPressed: () => _registar(context, fichas),
                  icon: const Icon(Icons.add),
                  label: const Text('Registar compromisso'),
                ),
                const SizedBox(height: 16),
              ],
              ...conteudo,
            ],
          ),
        ),
      ),
    );
  }

  /// Eventos pela ordem do servidor; nas vistas semana e mês, com o dia antes do primeiro evento de cada dia.
  static List<Widget> _porDia(
    List<ItemAgendaDto> itens, {
    required bool comDia,
  }) {
    final widgets = <Widget>[];
    String? diaAnterior;
    for (final item in itens) {
      final dia = FormatadorDataHora.apresentarData(item.inicio);
      if (comDia && dia != diaAnterior) {
        widgets.add(
          Padding(
            padding: const EdgeInsets.only(top: 8, bottom: 4),
            child: Semantics(header: true, child: Text(dia)),
          ),
        );
        diaAnterior = dia;
      }
      widgets.add(_Evento(item));
    }
    return widgets;
  }
}

class _Evento extends StatelessWidget {
  const _Evento(this.item);

  final ItemAgendaDto item;

  @override
  Widget build(BuildContext context) {
    final compromisso = item.tipo == TipoItemAgenda.compromisso;
    final estado = item.estado;
    final tipo = compromisso
        ? 'Compromisso'
        : 'Toma${estado == null ? '' : ' · ${ApresentacaoToma.texto(estado)}'}';
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(
          compromisso ? Icons.event_outlined : Icons.medication_outlined,
        ),
        title: Text(item.titulo),
        subtitle: Text(
          '${FormatadorDataHora.apresentarHora(item.inicio)} · $tipo\n'
          '${item.nomeFicha}',
        ),
        isThreeLine: true,
        trailing: compromisso ? const Icon(Icons.chevron_right) : null,
        onTap: compromisso
            ? () => context.push(
                AppRouter.editarCompromisso(item.fichaId, item.id),
              )
            : null,
      ),
    );
  }
}
