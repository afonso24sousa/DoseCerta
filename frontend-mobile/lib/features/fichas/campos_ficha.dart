import 'package:flutter/material.dart';

import '../../models/dominios_necessidades_dto.dart';
import '../../models/item_dominio_dto.dart';
import '../../shared/campo_data.dart';
import '../../shared/campo_formulario.dart';
import 'formulario_ficha.dart';

/// Campos da ficha (E06 e E09): dados de identificação e necessidades específicas, escolhidas das listas fechadas
/// de OP-08 (RF-020, RF-021). Cada erro aparece por baixo do seu campo (RNF-028).
class CamposFicha extends StatelessWidget {
  /// Cria os campos.
  const CamposFicha({
    super.key,
    required this.formulario,
    required this.dominios,
    required this.erros,
    required this.hoje,
  });

  /// Valores do formulário.
  final FormularioFicha formulario;

  /// Listas fechadas (OP-08).
  final DominiosNecessidadesDto dominios;

  /// Erros por campo da API.
  final Map<String, String> erros;

  /// Dia atual, último dia que o calendário deixa escolher.
  final DateTime hoje;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: formulario,
      builder: (context, _) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            CampoFormulario(
              rotulo: 'Nome completo',
              controlador: formulario.nome,
              erro: erros['nomeCompleto'],
              sugestoesPreenchimento: const [AutofillHints.name],
            ),
            CampoData(
              rotulo: 'Data de nascimento (dd/mm/aaaa)',
              controlador: formulario.dataNascimento,
              ultimaData: hoje,
              erro: erros['dataNascimento'],
            ),
            CampoFormulario(
              rotulo: 'Contacto de emergência',
              controlador: formulario.contacto,
              erro: erros['contactoEmergencia'],
              tipoTeclado: TextInputType.phone,
              ajuda: 'Por exemplo +351 910 000 001',
            ),
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: DropdownButtonFormField<int?>(
                initialValue: formulario.grauAutonomiaId,
                isExpanded: true,
                decoration: InputDecoration(
                  labelText: 'Grau de autonomia/mobilidade (lista fechada)',
                  errorText: erros['grauAutonomiaId'],
                ),
                items: [
                  const DropdownMenuItem<int?>(child: Text('Sem indicação')),
                  for (final grau in dominios.grausAutonomia)
                    DropdownMenuItem<int?>(
                      value: grau.id,
                      child: Text(grau.designacao),
                    ),
                ],
                onChanged: formulario.mudarGrau,
              ),
            ),
            _Escolhas(
              titulo: 'Condições de saúde relevantes (lista fechada)',
              itens: dominios.condicoesSaude,
              escolhidos: formulario.condicoes,
              alternar: formulario.alternarCondicao,
              erro: erros['condicoesSaudeIds'],
            ),
            _Escolhas(
              titulo: 'Alergias (lista fechada)',
              itens: dominios.alergias,
              escolhidos: formulario.alergias,
              alternar: formulario.alternarAlergia,
              erro: erros['alergiasIds'],
            ),
          ],
        );
      },
    );
  }
}

/// Lista fechada com escolha múltipla.
class _Escolhas extends StatelessWidget {
  const _Escolhas({
    required this.titulo,
    required this.itens,
    required this.escolhidos,
    required this.alternar,
    this.erro,
  });

  final String titulo;
  final List<ItemDominioDto> itens;
  final Set<int> escolhidos;
  final ValueChanged<int> alternar;
  final String? erro;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(titulo, style: tema.textTheme.titleSmall),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              for (final item in itens)
                FilterChip(
                  label: Text(item.designacao),
                  selected: escolhidos.contains(item.id),
                  onSelected: (_) => alternar(item.id),
                ),
            ],
          ),
          if (erro != null)
            Text(erro!, style: TextStyle(color: tema.colorScheme.error)),
        ],
      ),
    );
  }
}
