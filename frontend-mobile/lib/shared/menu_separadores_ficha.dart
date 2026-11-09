import 'package:flutter/material.dart';

/// Separadores da ficha (protótipo, E08: «Os separadores passam a um menu deslizante com o mesmo conteúdo»).
enum SeparadorFicha {
  /// Dados da ficha (E08).
  dados('Dados'),

  /// Medicação (E15).
  medicacao('Medicação'),

  /// Agenda (E21).
  agenda('Agenda'),

  /// Pessoas (E12).
  pessoas('Pessoas'),

  /// Ocorrências e notas (E23).
  registos('Registos'),

  /// Documentos (E10).
  documentos('Documentos');

  const SeparadorFicha(this.rotulo);

  /// Texto do separador.
  final String rotulo;
}

/// Menu deslizante da ficha com Dados, Medicação, Agenda, Pessoas, Registos e Documentos. Todos os separadores
/// existem para Cuidadores e Familiares (o Familiar vê em Registos só as ocorrências, RF-095), por isso nenhum é
/// omitido nesta versão.
class MenuSeparadoresFicha extends StatelessWidget {
  /// Cria o menu com o separador [ativo].
  const MenuSeparadoresFicha({
    super.key,
    required this.ativo,
    required this.mudar,
  });

  /// Separador atual.
  final SeparadorFicha ativo;

  /// Chamado quando o Utilizador escolhe outro separador.
  final ValueChanged<SeparadorFicha> mudar;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (final separador in SeparadorFicha.values)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ChoiceChip(
                label: Text(separador.rotulo),
                selected: separador == ativo,
                onSelected: separador == ativo ? null : (_) => mudar(separador),
              ),
            ),
        ],
      ),
    );
  }
}
