import 'package:flutter/material.dart';

/// Tema da aplicação: áreas de toque mínimas de 48 x 48 dp em todos os elementos interativos (RNF-025) e
/// tipografia do Material que acompanha o tamanho de letra do sistema (RNF-026). Os layouts não usam alturas
/// fixas e os ecrãs deslocam-se, para o conteúdo continuar acessível com a letra a 150%.
class TemaDoseCerta {
  const TemaDoseCerta._();

  /// Dimensão mínima de um alvo de toque (RNF-025).
  static const Size alvoMinimo = Size(48, 48);

  /// Cor principal (a do protótipo de baixa fidelidade).
  static const Color corPrincipal = Color(0xFF3D5A7A);

  /// Tema claro.
  static ThemeData claro() {
    final esquema = ColorScheme.fromSeed(seedColor: corPrincipal);
    final botao = ButtonStyle(
      minimumSize: const WidgetStatePropertyAll(alvoMinimo),
      tapTargetSize: MaterialTapTargetSize.padded,
    );
    return ThemeData(
      colorScheme: esquema,
      materialTapTargetSize: MaterialTapTargetSize.padded,
      visualDensity: VisualDensity.standard,
      filledButtonTheme: FilledButtonThemeData(style: botao),
      outlinedButtonTheme: OutlinedButtonThemeData(style: botao),
      textButtonTheme: TextButtonThemeData(style: botao),
      iconButtonTheme: IconButtonThemeData(style: botao),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
        errorMaxLines: 4,
        helperMaxLines: 3,
      ),
    );
  }
}
