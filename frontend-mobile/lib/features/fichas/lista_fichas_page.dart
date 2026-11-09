import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../models/estado_ficha.dart';
import '../../models/ficha_resumo_dto.dart';
import '../../shared/apresentacao_papel.dart';
import '../../shared/banner_erro.dart';
import 'lista_fichas_controller.dart';

/// Ecrã E05 Fichas (protótipo de baixa fidelidade v02, página 32): as fichas a que o Utilizador está associado,
/// com o seu papel em cada uma, nos separadores Ativas e Arquivadas, e o botão «Nova ficha». A lista volta a ser
/// pedida ao abrir e ao puxar para baixo.
class ListaFichasPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const ListaFichasPage({super.key});

  @override
  ConsumerState<ListaFichasPage> createState() => _ListaFichasPageState();
}

class _ListaFichasPageState extends ConsumerState<ListaFichasPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(listaFichasControllerProvider.notifier).carregar(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(listaFichasControllerProvider);
    final controlador = ref.read(listaFichasControllerProvider.notifier);
    final fichas = estado.fichasVisiveis;
    return Scaffold(
      appBar: AppBar(title: const Text('Fichas')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.carregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              SegmentedButton<EstadoFicha>(
                segments: const [
                  ButtonSegment(
                    value: EstadoFicha.ativa,
                    label: Text('Ativas'),
                  ),
                  ButtonSegment(
                    value: EstadoFicha.arquivada,
                    label: Text('Arquivadas'),
                  ),
                ],
                selected: {estado.filtroEstado},
                showSelectedIcon: false,
                onSelectionChanged: (escolha) =>
                    controlador.mudarFiltro(escolha.single),
              ),
              const SizedBox(height: 16),
              if (estado.erro != null) ...[
                BannerErro(estado.erro!),
                OutlinedButton(
                  onPressed: estado.aCarregar ? null : controlador.carregar,
                  child: const Text('Tentar novamente'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.aCarregar && !estado.carregada)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: LinearProgressIndicator(),
                ),
              if (estado.carregada && fichas.isEmpty)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Text(
                    estado.filtroEstado == EstadoFicha.ativa
                        ? 'Ainda não tem fichas ativas.'
                        : 'Não tem fichas arquivadas.',
                  ),
                ),
              for (final ficha in fichas) _LinhaFicha(ficha: ficha),
              const SizedBox(height: 8),
              FilledButton.icon(
                onPressed: () => context.go(AppRouter.novaFicha),
                icon: const Icon(Icons.add),
                label: const Text('Nova ficha'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LinhaFicha extends StatelessWidget {
  const _LinhaFicha({required this.ficha});

  final FichaResumoDto ficha;

  @override
  Widget build(BuildContext context) {
    final nome = ficha.eMinhaFichaPropria
        ? '${ficha.nomeCompleto} (própria)'
        : ficha.nomeCompleto;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        title: Text(nome),
        subtitle: Text(
          rotuloPapel(ficha.meuPapel, principal: ficha.souPrincipal),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => context.go(AppRouter.ficha(ficha.id)),
      ),
    );
  }
}
