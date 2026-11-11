import 'package:flutter/material.dart';

import '../core/formatador_data_hora.dart';

/// Campo de data que mostra `dd/mm/aaaa` e entrega `AAAA-MM-DD` à API (RNF-029). A data pode ser escrita ou
/// escolhida no calendário; o erro do campo aparece por baixo, com o formato esperado (RNF-028). O texto vive no
/// [controlador] da página, por isso um erro nunca apaga o que foi preenchido.
class CampoData extends StatelessWidget {
  /// Cria o campo.
  const CampoData({
    super.key,
    required this.rotulo,
    required this.controlador,
    required this.ultimaData,
    this.primeiraData,
    this.erro,
    this.ativo = true,
    this.ajuda = 'Formato dd/mm/aaaa',
  });

  /// Rótulo do campo (por exemplo «Data de nascimento»). Curto, para não ser cortado com a letra aumentada
  /// (RNF-026); o formato vai em [ajuda].
  final String rotulo;

  /// Texto por baixo do campo (por omissão, o formato), que pode ocupar várias linhas.
  final String ajuda;

  /// Controlador do texto, no formato `dd/mm/aaaa`.
  final TextEditingController controlador;

  /// Última data que o calendário permite escolher.
  final DateTime ultimaData;

  /// Primeira data que o calendário permite escolher (por omissão, 01/01/1900).
  final DateTime? primeiraData;

  /// Mensagem de erro do campo, ou `null`.
  final String? erro;

  /// Indica se o campo aceita edição.
  final bool ativo;

  /// Valor do campo no formato da API, ou `null` se não for uma data válida.
  String? get valorIso => FormatadorDataHora.paraIsoData(controlador.text);

  /// Abre o calendário e escreve a data escolhida no campo.
  Future<void> escolher(BuildContext context) async {
    final primeira = primeiraData ?? DateTime(1900);
    final atual = FormatadorDataHora.lerData(controlador.text);
    final inicial =
        atual == null || atual.isAfter(ultimaData) || atual.isBefore(primeira)
        ? ultimaData
        : atual;
    final escolhida = await showDatePicker(
      context: context,
      firstDate: primeira,
      lastDate: ultimaData,
      initialDate: inicial,
      initialEntryMode: DatePickerEntryMode.calendarOnly,
    );
    if (escolhida != null) {
      controlador.text = FormatadorDataHora.deData(escolhida);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextField(
        controller: controlador,
        enabled: ativo,
        keyboardType: TextInputType.datetime,
        textInputAction: TextInputAction.next,
        decoration: InputDecoration(
          labelText: rotulo,
          hintText: 'dd/mm/aaaa',
          helperText: ajuda,
          errorText: erro,
          suffixIcon: IconButton(
            tooltip: 'Escolher no calendário',
            icon: const Icon(Icons.calendar_month_outlined),
            onPressed: ativo ? () => escolher(context) : null,
          ),
        ),
      ),
    );
  }
}
