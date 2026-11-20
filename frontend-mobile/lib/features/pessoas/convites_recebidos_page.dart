import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/convite_dto.dart';
import '../../shared/apresentacao_papel.dart';
import '../../shared/banner_erro.dart';
import 'convites_recebidos_controller.dart';

/// Ecrã E14 Convites recebidos (protótipo de baixa fidelidade v02, página 36): cada convite pendente com a ficha,
/// o papel oferecido e quem convidou, com «Aceitar» e «Recusar». Abre-se a partir de E05. A lista volta a ser
/// pedida ao abrir e ao puxar para baixo.
class ConvitesRecebidosPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const ConvitesRecebidosPage({super.key});

  /// Explicação do convite para Cuidador principal (protótipo, E14; RF-026, RF-027).
  static const String notaPrincipal =
      'Aceitar o convite para Cuidador principal transfere esse papel: o '
      'criador deixa de estar associado à ficha. Recusar mantém o criador.';

  @override
  ConsumerState<ConvitesRecebidosPage> createState() =>
      _ConvitesRecebidosPageState();
}

class _ConvitesRecebidosPageState extends ConsumerState<ConvitesRecebidosPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(convitesRecebidosControllerProvider.notifier).carregar(),
    );
  }

  Future<void> _responder(ConviteDto convite, {required bool aceitar}) async {
    final controlador = ref.read(convitesRecebidosControllerProvider.notifier);
    final feito = aceitar
        ? await controlador.aceitar(convite)
        : await controlador.recusar(convite);
    if (!feito || !mounted) {
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          aceitar
              ? 'Convite aceite. A ficha ${convite.nomeFicha} está nas suas '
                    'fichas.'
              : 'Convite recusado.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(convitesRecebidosControllerProvider);
    final controlador = ref.read(convitesRecebidosControllerProvider.notifier);
    return Scaffold(
      appBar: AppBar(title: const Text('Convites recebidos')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.carregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              Text('Convites', style: Theme.of(context).textTheme.titleLarge),
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
              if (estado.carregada && estado.convites.isEmpty)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: Text('Não tem convites pendentes.'),
                ),
              for (final convite in estado.convites)
                _CartaoConvite(
                  convite: convite,
                  emResposta: estado.emResposta.contains(convite.id),
                  aoAceitar: () => _responder(convite, aceitar: true),
                  aoRecusar: () => _responder(convite, aceitar: false),
                ),
              if (estado.convites.any((c) => c.paraPrincipal))
                const Padding(
                  padding: EdgeInsets.only(top: 8),
                  child: Text(ConvitesRecebidosPage.notaPrincipal),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _CartaoConvite extends StatelessWidget {
  const _CartaoConvite({
    required this.convite,
    required this.emResposta,
    required this.aoAceitar,
    required this.aoRecusar,
  });

  final ConviteDto convite;
  final bool emResposta;
  final VoidCallback aoAceitar;
  final VoidCallback aoRecusar;

  @override
  Widget build(BuildContext context) {
    final papel = rotuloPapel(
      convite.papelOferecido,
      principal: convite.paraPrincipal,
    );
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              convite.nomeFicha,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 4),
            Text('$papel | convidado por ${convite.convidadoPor}'),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                FilledButton(
                  onPressed: emResposta ? null : aoAceitar,
                  child: const Text('Aceitar'),
                ),
                OutlinedButton(
                  onPressed: emResposta ? null : aoRecusar,
                  child: const Text('Recusar'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
