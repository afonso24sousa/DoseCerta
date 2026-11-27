import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/formatador_data_hora.dart';
import '../../models/estado_toma.dart';
import '../../models/item_lista_diaria_dto.dart';
import '../../shared/banner_erro.dart';
import '../medicacao/apresentacao_toma.dart';
import 'lista_diaria_controller.dart';

/// Ecrã E04 Lista de tarefas diária (protótipo de baixa fidelidade v02, página 31), o ecrã inicial: as tomas e os
/// compromissos do dia de todas as fichas ativas em que o Utilizador é Cuidador, por hora e com o nome do utente
/// (RF-100, RF-101), com «Ontem», «Hoje» e «Amanhã». O Cuidador confirma as tomas com um toque, sem abrir a ficha
/// (RF-102, RNF-021); os compromissos são só para consulta (RF-087). Puxar para baixo lê a lista de novo e
/// sincroniza os avisos locais (RF-061, RF-104). O botão do topo abre o E26.
class ListaDiariaPage extends ConsumerWidget {
  /// Cria o ecrã.
  const ListaDiariaPage({super.key});

  /// Texto quando o dia não tem tarefas.
  static const String semTarefas = 'Não há tarefas para este dia.';

  /// Texto dos compromissos, que não se confirmam (RF-087).
  static const String compromisso = 'Compromisso, sem confirmação';

  Future<void> _confirmar(
    BuildContext context,
    WidgetRef ref,
    String tomaId,
  ) async {
    final estado = await ref
        .read(listaDiariaControllerProvider.notifier)
        .confirmar(tomaId);
    if (estado == null || !context.mounted) {
      return;
    }
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(content: Text(ApresentacaoToma.mensagemConfirmada(estado))),
      );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // O separador «Hoje» fica aberto em segundo plano: ao voltar à aplicação, a lista é lida de novo, com o dia de
    // hoje e as confirmações dos outros Cuidadores (RF-104). Os avisos são sincronizados pelo observador da I069.
    return _AoVoltar(
      aoVoltar: () => ref
          .read(listaDiariaControllerProvider.notifier)
          .carregar(manterErro: false),
      child: _ecra(context, ref),
    );
  }

  Widget _ecra(BuildContext context, WidgetRef ref) {
    final estado = ref.watch(listaDiariaControllerProvider);
    final controlador = ref.read(listaDiariaControllerProvider.notifier);
    final itens = estado.itens;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lista de tarefas diária'),
        actions: [
          IconButton(
            tooltip: 'Avisos de toma',
            icon: const Icon(Icons.notifications_active_outlined),
            onPressed: () => context.go(AppRouter.avisosLocais),
          ),
        ],
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.atualizar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              Semantics(
                header: true,
                child: Text(
                  '${estado.dia.rotulo}, '
                  '${FormatadorDataHora.deData(estado.data)}',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
              ),
              const SizedBox(height: 16),
              SegmentedButton<DiaLista>(
                segments: [
                  for (final dia in DiaLista.values)
                    ButtonSegment(value: dia, label: Text(dia.rotulo)),
                ],
                selected: {estado.dia},
                showSelectedIcon: false,
                onSelectionChanged: (escolha) =>
                    controlador.mudarDia(escolha.single),
              ),
              const SizedBox(height: 16),
              if (estado.erro != null) BannerErro(estado.erro!),
              if (estado.erroCarregar != null) ...[
                BannerErro(estado.erroCarregar!),
                OutlinedButton(
                  onPressed: estado.aCarregar ? null : controlador.carregar,
                  child: const Text('Tentar novamente'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.aCarregar && itens == null)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: LinearProgressIndicator(),
                ),
              if (itens != null && itens.isEmpty) const Text(semTarefas),
              for (final item in itens ?? const <ItemListaDiariaDto>[])
                _CartaoItem(
                  item: item,
                  dia: estado.data,
                  aConfirmar: estado.aConfirmar.contains(item.id),
                  confirmar: () => _confirmar(context, ref, item.id),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Chama [aoVoltar] quando a aplicação volta do segundo plano.
class _AoVoltar extends StatefulWidget {
  const _AoVoltar({required this.aoVoltar, required this.child});

  final Future<void> Function() aoVoltar;
  final Widget child;

  @override
  State<_AoVoltar> createState() => _AoVoltarState();
}

class _AoVoltarState extends State<_AoVoltar> {
  late final AppLifecycleListener _ouvinte = AppLifecycleListener(
    onResume: () => unawaited(widget.aoVoltar()),
  );

  @override
  void initState() {
    super.initState();
    _ouvinte;
  }

  @override
  void dispose() {
    _ouvinte.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

/// Um item da lista: a hora (com a data completa nas tomas de outro dia, por exemplo as pendentes de ontem; DD03),
/// o utente, o título e o estado por ícone e texto (RNF-027). Tocar no item abre a ficha.
class _CartaoItem extends StatelessWidget {
  const _CartaoItem({
    required this.item,
    required this.dia,
    required this.aConfirmar,
    required this.confirmar,
  });

  final ItemListaDiariaDto item;
  final DateTime dia;
  final bool aConfirmar;
  final VoidCallback confirmar;

  String get _hora {
    final hora = FormatadorDataHora.apresentarHora(item.previstaEm);
    final doDia = item.previstaEm.startsWith(FormatadorDataHora.paraIso(dia));
    return doDia
        ? hora
        : '${FormatadorDataHora.apresentarData(item.previstaEm)} $hora';
  }

  @override
  Widget build(BuildContext context) {
    final estado = item.estado;
    final confirmadaPor = item.confirmadaPor;
    final confirmadaEm = item.confirmadaEm;
    final secundario = Theme.of(context).textTheme.bodyMedium;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        onTap: () => context.go(AppRouter.ficha(item.fichaId)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                '$_hora | ${item.nomeFicha}',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: 4),
              Text(item.titulo),
              const SizedBox(height: 8),
              Row(
                children: [
                  Icon(
                    estado == null
                        ? Icons.event_outlined
                        : ApresentacaoToma.icone(estado),
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      estado == null
                          ? ListaDiariaPage.compromisso
                          : ApresentacaoToma.texto(estado),
                      style: secundario,
                    ),
                  ),
                ],
              ),
              if (confirmadaPor != null && confirmadaEm != null)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    'Confirmada por $confirmadaPor em '
                    '${FormatadorDataHora.apresentarData(confirmadaEm)} às '
                    '${FormatadorDataHora.apresentarHora(confirmadaEm)}',
                    style: secundario,
                  ),
                ),
              if (item.eToma &&
                  item.confirmavel &&
                  estado == EstadoToma.pendente) ...[
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerLeft,
                  child: FilledButton(
                    onPressed: aConfirmar ? null : confirmar,
                    child: Text(
                      'Confirmar',
                      semanticsLabel:
                          'Confirmar ${item.titulo} de ${item.nomeFicha}',
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
