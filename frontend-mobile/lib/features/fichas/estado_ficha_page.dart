import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../shared/banner_erro.dart';
import '../../shared/dialogo_confirmacao.dart';
import 'estado_ficha_controller.dart';

/// Ecrã E11 Arquivar, reativar ou eliminar ficha (protótipo de baixa fidelidade v02, página 35): só o Cuidador
/// (RF-120 a RF-123). «Arquivar» só numa ficha ativa e «Reativar» só numa arquivada; cada ação é confirmada numa
/// folha inferior (decisão D8). Eliminar só é aceite sem registos; noutro caso o servidor recusa e propõe-se
/// arquivar (RF-123).
class EstadoFichaPage extends ConsumerWidget {
  /// Cria o ecrã da ficha [fichaId].
  const EstadoFichaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  /// Texto do protótipo sobre o arquivo.
  static const String textoArquivo =
      'Ao arquivar, a ficha deixa de gerar tomas, avisos e notificações. O histórico é preservado e a ficha passa '
      'a só leitura.';

  /// Texto sobre a reativação (RF-121, RF-122).
  static const String textoReativacao =
      'Ao reativar, a ficha volta a gerar tomas, avisos e notificações a partir deste momento, sem criar os do '
      'período em que esteve arquivada.';

  /// Texto do protótipo sobre a eliminação.
  static const String textoEliminacao =
      'Eliminar só é possível se a ficha não tiver registos.';

  void _informar(BuildContext context, String mensagem) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(mensagem)));
  }

  void _voltarAFicha(BuildContext context) {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRouter.ficha(fichaId));
    }
  }

  Future<void> _arquivar(BuildContext context, WidgetRef ref) async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: 'Arquivar ficha',
      mensagem: textoArquivo,
      acao: 'Arquivar',
    );
    if (!confirmado || !context.mounted) {
      return;
    }
    final feito = await ref
        .read(estadoFichaControllerProvider(fichaId).notifier)
        .arquivar();
    if (feito && context.mounted) {
      _informar(context, 'Ficha arquivada.');
      _voltarAFicha(context);
    }
  }

  Future<void> _reativar(BuildContext context, WidgetRef ref) async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: 'Reativar ficha',
      mensagem: textoReativacao,
      acao: 'Reativar',
    );
    if (!confirmado || !context.mounted) {
      return;
    }
    final feito = await ref
        .read(estadoFichaControllerProvider(fichaId).notifier)
        .reativar();
    if (feito && context.mounted) {
      _informar(context, 'Ficha reativada.');
      _voltarAFicha(context);
    }
  }

  Future<void> _eliminar(BuildContext context, WidgetRef ref) async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: 'Eliminar ficha',
      mensagem:
          'A ficha, os compromissos, os documentos, os convites e as associações são removidos e não podem ser '
          'recuperados. $textoEliminacao',
      acao: 'Eliminar',
    );
    if (!confirmado || !context.mounted) {
      return;
    }
    final feito = await ref
        .read(estadoFichaControllerProvider(fichaId).notifier)
        .eliminar();
    if (feito && context.mounted) {
      _informar(context, 'Ficha eliminada.');
      context.go(AppRouter.fichas);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final acesso = ref.watch(fichaAcessoProvider(fichaId));
    final estado = ref.watch(estadoFichaControllerProvider(fichaId));
    final ocupado = estado.emCurso != null;
    final tema = Theme.of(context);

    final List<Widget> conteudo;
    switch (acesso) {
      case AsyncError(:final error):
        conteudo = [BannerErro(MensagensErro.traduzir(error))];
      case AsyncData(:final value) when !value.podeGerirEstado:
        conteudo = [BannerErro(MensagensErro.catalogo['ACESSO_PROIBIDO']!)];
      case AsyncData(:final value):
        final ativa = value.ativa;
        conteudo = [
          Text(
            ativa ? 'Arquivar ficha' : 'Reativar ficha',
            style: tema.textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Text(ativa ? textoArquivo : textoReativacao),
          const SizedBox(height: 16),
          if (ativa)
            FilledButton(
              onPressed: ocupado ? null : () => _arquivar(context, ref),
              child: const Text('Arquivar'),
            )
          else
            FilledButton(
              onPressed: ocupado ? null : () => _reativar(context, ref),
              child: const Text('Reativar'),
            ),
          const SizedBox(height: 24),
          Text('Eliminar ficha', style: tema.textTheme.titleMedium),
          const SizedBox(height: 8),
          const Text(textoEliminacao),
          const SizedBox(height: 16),
          OutlinedButton(
            style: OutlinedButton.styleFrom(
              foregroundColor: tema.colorScheme.error,
            ),
            onPressed: ocupado ? null : () => _eliminar(context, ref),
            child: const Text('Eliminar'),
          ),
          const SizedBox(height: 16),
          if (ocupado) const LinearProgressIndicator(),
          if (estado.erro != null) BannerErro(estado.erro!),
        ];
      default:
        conteudo = const [LinearProgressIndicator()];
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Arquivar, reativar ou eliminar ficha')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: conteudo,
          ),
        ),
      ),
    );
  }
}
