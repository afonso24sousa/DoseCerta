import 'package:flutter/material.dart';

import '../models/categoria_documento.dart';

/// Folha inferior para escolher a categoria de um documento a acrescentar, antes do envio (decisão D3 da I060): a
/// API exige uma das três categorias (RF-022).
class FolhaCategoriaDocumento extends StatelessWidget {
  /// Cria a folha.
  const FolhaCategoriaDocumento({super.key});

  /// Abre a folha; devolve a categoria escolhida ou `null` se o Utilizador a fechar.
  static Future<CategoriaDocumento?> mostrar(BuildContext context) {
    return showModalBottomSheet<CategoriaDocumento>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) => const FolhaCategoriaDocumento(),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.only(bottom: 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text(
                'Categoria do documento',
                style: Theme.of(context).textTheme.titleMedium,
              ),
            ),
            for (final categoria in CategoriaDocumento.values)
              ListTile(
                title: Text(categoria.rotulo),
                onTap: () => Navigator.of(context).pop(categoria),
              ),
          ],
        ),
      ),
    );
  }
}
