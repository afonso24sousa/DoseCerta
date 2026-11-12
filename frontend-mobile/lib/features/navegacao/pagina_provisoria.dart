import 'package:flutter/material.dart';

/// Página provisória de uma entrada da barra inferior ainda não implementada. Existe só para a navegação depois
/// do início de sessão funcionar (I058); cada página será substituída pelo ecrã da sua Issue.
class PaginaProvisoria extends StatelessWidget {
  /// Cria a página provisória do ecrã [ecra] com o [titulo] indicado.
  const PaginaProvisoria({super.key, required this.titulo, required this.ecra});

  /// Título do ecrã (por exemplo «Lista de tarefas diária»).
  final String titulo;

  /// Identificador do ecrã no protótipo (por exemplo «E04»).
  final String ecra;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(titulo)),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.construction_outlined),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Página provisória',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'O ecrã $ecra ($titulo) ainda não está implementado nesta versão da aplicação. '
                          'Será disponibilizado numa próxima entrega.',
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
