import 'package:flutter/material.dart';

/// Confirmação de ações destrutivas ou com efeitos importantes (arquivar, reativar, eliminar), numa folha inferior
/// (protótipo, E11).
class DialogoConfirmacao extends StatelessWidget {
  /// Cria a confirmação.
  const DialogoConfirmacao({
    super.key,
    required this.titulo,
    required this.mensagem,
    required this.acao,
  });

  /// Título.
  final String titulo;

  /// Explicação do efeito da ação.
  final String mensagem;

  /// Texto do botão que confirma (por exemplo «Arquivar»).
  final String acao;

  /// Abre a confirmação; devolve `true` só se o Utilizador confirmar.
  static Future<bool> confirmar(
    BuildContext context, {
    required String titulo,
    required String mensagem,
    required String acao,
  }) async {
    final resposta = await showModalBottomSheet<bool>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) =>
          DialogoConfirmacao(titulo: titulo, mensagem: mensagem, acao: acao),
    );
    return resposta ?? false;
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(titulo, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Text(mensagem),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () => Navigator.of(context).pop(true),
              child: Text(acao),
            ),
            const SizedBox(height: 8),
            OutlinedButton(
              onPressed: () => Navigator.of(context).pop(false),
              child: const Text('Cancelar'),
            ),
          ],
        ),
      ),
    );
  }
}
