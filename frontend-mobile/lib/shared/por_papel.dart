import 'package:flutter/widgets.dart';

import '../models/papel.dart';

/// Só mostra o [filho] quando o [papel] do Utilizador na ficha é o [exigido] (por omissão, Cuidador) e, se
/// [soFichaAtiva], a ficha está ativa. É só apresentação: a autorização é decidida pelo servidor em cada pedido
/// (RNF-004; matriz de permissões).
class PorPapel extends StatelessWidget {
  /// Cria o widget.
  const PorPapel({
    super.key,
    required this.papel,
    required this.filho,
    this.exigido = Papel.cuidador,
    this.soFichaAtiva = false,
    this.fichaAtiva = true,
  });

  /// Papel do Utilizador na ficha.
  final Papel papel;

  /// Papel exigido.
  final Papel exigido;

  /// Indica se a ação exige a ficha ativa (RF-120: a ficha arquivada é só de consulta).
  final bool soFichaAtiva;

  /// Indica se a ficha está ativa.
  final bool fichaAtiva;

  /// Conteúdo a mostrar.
  final Widget filho;

  /// Indica se o conteúdo é mostrado.
  bool get visivel => papel == exigido && (!soFichaAtiva || fichaAtiva);

  @override
  Widget build(BuildContext context) =>
      visivel ? filho : const SizedBox.shrink();
}
