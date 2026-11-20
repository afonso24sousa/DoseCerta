import '../../core/formatador_data_hora.dart';
import '../../models/tratamento_dto.dart';

/// Textos do tratamento comuns a E15 e E17, iguais aos do frontend web (I066).
class ApresentacaoTratamento {
  const ApresentacaoTratamento._();

  /// Mensagem quando o tratamento já não pode ser editado (`TRATAMENTO_TERMINADO`, RF-055).
  static const String naoEditavel =
      'O tratamento já terminou e não pode ser editado; continua disponível para consulta.';

  /// Mensagens dos erros da F005, passadas em `especificas` a `MensagensErro.traduzir` (contrato do Sprint 08:
  /// ninguém mexe no catálogo partilhado).
  static const Map<String, String> mensagensErro = {
    'TRATAMENTO_TERMINADO': naoEditavel,
    'TRATAMENTO_JA_TERMINADO': 'O tratamento já está terminado.',
    'RECURSO_NAO_ENCONTRADO': 'O tratamento não existe ou pertence a uma ficha a que não está associado.',
  };

  /// Medicamento e dose, por exemplo «Metformina 500 mg».
  static String titulo(TratamentoDto tratamento) =>
      '${tratamento.medicamento} ${tratamento.dose}';

  /// Horários, por exemplo «08:00, 14:00 e 20:00».
  static String horarios(List<String> horarios) {
    if (horarios.length < 2) {
      return horarios.isEmpty ? '' : horarios.first;
    }
    final ultimos = horarios.last;
    return '${horarios.sublist(0, horarios.length - 1).join(', ')} e $ultimos';
  }

  /// Período, por exemplo «07/10/2026 a 05/11/2026» ou «desde 07/10/2026, contínuo».
  static String periodo(TratamentoDto tratamento) {
    final inicio = FormatadorDataHora.apresentarData(tratamento.dataInicio);
    final fim = tratamento.dataFim;
    return fim == null
        ? 'desde $inicio, contínuo'
        : '$inicio a ${FormatadorDataHora.apresentarData(fim)}';
  }
}
