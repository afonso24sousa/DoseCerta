import 'package:flutter/material.dart';

import 'origem_ficheiro.dart';
import 'selecionador_ficheiros.dart';

/// Folha inferior aberta por cada «Carregar», com Câmara, Galeria e Ficheiro (decisão D2 da I060). Cada opção é
/// uma linha de lista, com área de toque acima de 48 dp (RNF-025).
class FolhaOrigemFicheiro extends StatelessWidget {
  /// Cria a folha com o [titulo] (por exemplo «Carregar: Receita médica»).
  const FolhaOrigemFicheiro({super.key, required this.titulo});

  /// Título da folha.
  final String titulo;

  /// Abre a folha; devolve a origem escolhida ou `null` se o Utilizador a fechar.
  static Future<OrigemFicheiro?> mostrar(
    BuildContext context, {
    required String titulo,
  }) {
    return showModalBottomSheet<OrigemFicheiro>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) => FolhaOrigemFicheiro(titulo: titulo),
    );
  }

  static const Map<OrigemFicheiro, IconData> _icones = {
    OrigemFicheiro.camera: Icons.photo_camera_outlined,
    OrigemFicheiro.galeria: Icons.photo_library_outlined,
    OrigemFicheiro.ficheiro: Icons.description_outlined,
  };

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
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
              child: Text(
                titulo,
                style: Theme.of(context).textTheme.titleMedium,
              ),
            ),
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text(SelecionadorFicheiros.ficheirosAceites),
            ),
            for (final origem in OrigemFicheiro.values)
              ListTile(
                leading: Icon(_icones[origem]),
                title: Text(origem.rotulo),
                onTap: () => Navigator.of(context).pop(origem),
              ),
          ],
        ),
      ),
    );
  }
}
