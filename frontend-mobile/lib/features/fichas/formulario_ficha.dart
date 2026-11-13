import 'package:flutter/widgets.dart';

import '../../core/formatador_data_hora.dart';
import '../../models/dados_ficha_dto.dart';

/// Valores do formulário de ficha (E06 e E09): os textos vivem em controladores e as necessidades em conjuntos,
/// por isso um erro nunca apaga o que foi preenchido (RNF-028).
class FormularioFicha extends ChangeNotifier {
  /// Nome completo.
  final TextEditingController nome = TextEditingController();

  /// Data de nascimento, como o Utilizador a escreve (`dd/mm/aaaa`).
  final TextEditingController dataNascimento = TextEditingController();

  /// Contacto de emergência.
  final TextEditingController contacto = TextEditingController();

  /// Grau de autonomia/mobilidade escolhido.
  int? grauAutonomiaId;

  /// Condições de saúde escolhidas.
  final Set<int> condicoes = {};

  /// Alergias escolhidas.
  final Set<int> alergias = {};

  /// Preenche com os dados de uma ficha (E09).
  void preencher(DadosFichaDto dados) {
    nome.text = dados.nomeCompleto;
    dataNascimento.text = FormatadorDataHora.apresentarData(
      dados.dataNascimento,
    );
    contacto.text = dados.contactoEmergencia;
    grauAutonomiaId = dados.grauAutonomiaId;
    condicoes
      ..clear()
      ..addAll(dados.condicoesSaudeIds);
    alergias
      ..clear()
      ..addAll(dados.alergiasIds);
    notifyListeners();
  }

  /// Muda o grau de autonomia/mobilidade.
  void mudarGrau(int? id) {
    grauAutonomiaId = id;
    notifyListeners();
  }

  /// Escolhe ou retira uma condição de saúde.
  void alternarCondicao(int id) {
    condicoes.contains(id) ? condicoes.remove(id) : condicoes.add(id);
    notifyListeners();
  }

  /// Escolhe ou retira uma alergia.
  void alternarAlergia(int id) {
    alergias.contains(id) ? alergias.remove(id) : alergias.add(id);
    notifyListeners();
  }

  /// Dados para a API. A data segue em `AAAA-MM-DD` (já validada antes).
  DadosFichaDto dados({required bool propria}) {
    return DadosFichaDto(
      propria: propria,
      nomeCompleto: nome.text.trim(),
      dataNascimento:
          FormatadorDataHora.paraIsoData(dataNascimento.text) ??
          dataNascimento.text.trim(),
      contactoEmergencia: contacto.text.trim(),
      grauAutonomiaId: grauAutonomiaId,
      condicoesSaudeIds: condicoes.toList()..sort(),
      alergiasIds: alergias.toList()..sort(),
    );
  }

  @override
  void dispose() {
    nome.dispose();
    dataNascimento.dispose();
    contacto.dispose();
    super.dispose();
  }
}
