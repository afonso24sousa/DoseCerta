import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../shared/banner_erro.dart';
import '../../shared/dialogo_confirmacao.dart';
import 'apresentacao_tratamento.dart';
import 'tratamento_controller.dart';

/// Ecrã E17 Detalhe do tratamento: Cuidadores e Familiares consultam os dados, o estado e a receita (RF-054,
/// RF-057). «Editar» só aparece ao Cuidador, num tratamento em curso de uma ficha ativa (RF-055, RF-120);
/// «Terminar tratamento» só ao Cuidador, num tratamento em curso, também numa ficha arquivada (RF-056; a OP-31 não
/// devolve `FICHA_ARQUIVADA`). O Familiar só consulta (RF-048).
class TratamentoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã do tratamento [tratamentoId] da ficha [fichaId].
  const TratamentoPage({
    super.key,
    required this.fichaId,
    required this.tratamentoId,
  });

  /// Identificador da ficha.
  final String fichaId;

  /// Identificador do tratamento.
  final String tratamentoId;

  @override
  ConsumerState<TratamentoPage> createState() => _TratamentoPageState();
}

class _TratamentoPageState extends ConsumerState<TratamentoPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(tratamentoControllerProvider(widget.tratamentoId).notifier)
          .carregar(),
    );
  }

  Future<void> _terminar() async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: 'Terminar o tratamento?',
      mensagem:
          'As tomas previstas a partir de agora deixam de existir. Os dados do tratamento e as tomas registadas '
          'continuam disponíveis para consulta.',
      acao: 'Sim, terminar o tratamento',
    );
    if (!confirmado || !mounted) {
      return;
    }
    final terminado = await ref
        .read(tratamentoControllerProvider(widget.tratamentoId).notifier)
        .terminar();
    if (terminado && mounted) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(content: Text(TratamentoController.terminado)),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(tratamentoControllerProvider(widget.tratamentoId));
    final controlador = ref.read(
      tratamentoControllerProvider(widget.tratamentoId).notifier,
    );
    final tratamento = estado.tratamento;
    final cuidador = acesso != null && acesso.ficha.souCuidador;
    final podeEditar = acesso != null && acesso.podeEscrever;

    final List<Widget> conteudo;
    if (estado.erroCarregar != null) {
      conteudo = [
        BannerErro(estado.erroCarregar!),
        OutlinedButton(
          onPressed: estado.aCarregar ? null : controlador.carregar,
          child: const Text('Tentar novamente'),
        ),
      ];
    } else if (tratamento == null) {
      conteudo = const [LinearProgressIndicator()];
    } else {
      final tema = Theme.of(context).textTheme;
      conteudo = [
        Text(ApresentacaoTratamento.titulo(tratamento), style: tema.titleLarge),
        const SizedBox(height: 4),
        Text('Estado: ${tratamento.estado.rotulo}', style: tema.titleSmall),
        const SizedBox(height: 16),
        if (acesso != null && !acesso.ativa)
          const BannerErro(
            'Ficha arquivada: só consulta. Ainda pode terminar os tratamentos em curso.',
            informativo: true,
          ),
        _Dado('Grau de importância', tratamento.grauImportancia.valor),
        _Dado('Horários', ApresentacaoTratamento.horarios(tratamento.horarios)),
        _Dado('Período', ApresentacaoTratamento.periodo(tratamento)),
        Text('Receita médica', style: tema.titleSmall),
        const SizedBox(height: 4),
        if (tratamento.receitaDocumentoId == null)
          const Text('Sem receita.')
        else if (estado.erroReceita != null) ...[
          BannerErro(estado.erroReceita!),
          OutlinedButton(
            onPressed: estado.aAbrirReceita ? null : controlador.abrirReceita,
            child: const Text('Tentar novamente'),
          ),
        ] else
          Align(
            alignment: Alignment.centerLeft,
            child: OutlinedButton.icon(
              onPressed: estado.aAbrirReceita ? null : controlador.abrirReceita,
              icon: const Icon(Icons.description_outlined),
              label: Text(
                estado.aAbrirReceita ? 'A abrir…' : 'Abrir a receita',
              ),
            ),
          ),
        const SizedBox(height: 24),
        if (estado.erro != null) BannerErro(estado.erro!),
        if (estado.aTerminar)
          const Padding(
            padding: EdgeInsets.only(bottom: 8),
            child: LinearProgressIndicator(),
          ),
        if (podeEditar && tratamento.emCurso) ...[
          FilledButton.icon(
            onPressed: estado.aTerminar
                ? null
                : () => context.go(
                    AppRouter.editarTratamento(widget.fichaId, tratamento.id),
                  ),
            icon: const Icon(Icons.edit_outlined),
            label: const Text('Editar'),
          ),
          const SizedBox(height: 8),
        ],
        if (cuidador && tratamento.emCurso)
          OutlinedButton.icon(
            onPressed: estado.aTerminar ? null : _terminar,
            icon: const Icon(Icons.stop_circle_outlined),
            label: const Text('Terminar tratamento'),
          ),
      ];
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Tratamento')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.carregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: conteudo,
          ),
        ),
      ),
    );
  }
}

class _Dado extends StatelessWidget {
  const _Dado(this.rotulo, this.valor);

  final String rotulo;
  final String valor;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(rotulo, style: Theme.of(context).textTheme.titleSmall),
          Text(valor),
        ],
      ),
    );
  }
}
