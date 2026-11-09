import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Barra inferior da aplicação com sessão iniciada (modelo de classes, secção 9; protótipo, mapa de navegação
/// mobile): Hoje (E04), Fichas (E05), Agenda (E21), Notificações (E25) e Conta (E03).
class EstruturaNavegacao extends StatelessWidget {
  /// Cria a estrutura sobre o [shell] das rotas.
  const EstruturaNavegacao({super.key, required this.shell});

  /// Ramos da navegação, um por entrada da barra.
  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: shell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: shell.currentIndex,
        onDestinationSelected: (indice) => shell.goBranch(
          indice,
          initialLocation: indice == shell.currentIndex,
        ),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.today_outlined),
            label: 'Hoje',
          ),
          NavigationDestination(
            icon: Icon(Icons.folder_shared_outlined),
            label: 'Fichas',
          ),
          NavigationDestination(
            icon: Icon(Icons.calendar_month_outlined),
            label: 'Agenda',
          ),
          NavigationDestination(
            icon: Icon(Icons.notifications_outlined),
            label: 'Notificações',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            label: 'Conta',
          ),
        ],
      ),
    );
  }
}
