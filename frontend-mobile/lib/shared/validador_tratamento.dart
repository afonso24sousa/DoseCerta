import '../core/formatador_data_hora.dart';
import '../core/mensagens_erro.dart';
import '../models/grau_importancia.dart';

/// Validação prévia do tratamento em E16 (RF-050, RF-051, RF-055, RNF-028), com as regras do servidor
/// (`TratamentoService`). Os erros usam os nomes dos campos da API, para se juntarem aos que o servidor devolve. A
/// decisão final é sempre do servidor.
class ValidadorTratamento {
  const ValidadorTratamento._();

  /// Comprimento máximo do medicamento (modelo de dados).
  static const int comprimentoMaximoMedicamento = 120;

  /// Comprimento máximo da dose (modelo de dados).
  static const int comprimentoMaximoDose = 60;

  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'medicamento': 'Medicamento',
    'dose': 'Dose',
    'grauImportancia': 'Grau de importância',
    'dataInicio': 'Data de início',
    'dataFim': 'Data de fim',
    'horarios': 'Horários',
    'receita.ficheiro': 'Receita médica',
  };

  /// Regra da data de fim num tratamento que já a tem: o servidor mantém a data quando não é enviada.
  static const String regraDataFimNaoRetirada =
      'Num tratamento com data de fim, a data pode ser alterada mas não retirada.';

  static final RegExp _hora = RegExp(r'^(\d{1,2}):(\d{2})$');

  /// Lê uma hora `h:mm` ou `hh:mm` e devolve-a como `HH:mm`; `null` se não for uma hora válida.
  static String? normalizarHora(String texto) {
    final partes = _hora.firstMatch(texto.trim());
    if (partes == null) {
      return null;
    }
    final horas = int.parse(partes.group(1)!);
    final minutos = int.parse(partes.group(2)!);
    if (horas > 23 || minutos > 59) {
      return null;
    }
    return '${horas.toString().padLeft(2, '0')}:${partes.group(2)}';
  }

  /// Valida a hora a acrescentar aos [existentes]; devolve a mensagem de erro, ou `null` se puder ser acrescentada.
  static String? validarHora(String texto, List<String> existentes) {
    final hora = normalizarHora(texto);
    if (hora == null) {
      return 'Hora: indique uma hora no formato hh:mm, por exemplo 08:00.';
    }
    if (existentes.contains(hora)) {
      return 'Hora: a hora $hora já está na lista.';
    }
    return null;
  }

  /// Valida o formulário; devolve os erros por campo da API (vazio se tudo estiver certo). Na [edicao], a data de
  /// início não é validada (não muda) e, se o tratamento já tinha data de fim ([comDataFim]), não pode ficar vazia.
  static Map<String, String> validar({
    required String medicamento,
    required String dose,
    required GrauImportancia? grau,
    required String dataInicio,
    required String dataFim,
    required List<String> horarios,
    bool edicao = false,
    bool comDataFim = false,
  }) {
    final erros = <String, String>{};
    void erro(String campo, String regra) =>
        erros[campo] = MensagensErro.comRotulo(rotulos[campo]!, regra);

    _obrigatorio(
      erro,
      'medicamento',
      medicamento,
      comprimentoMaximoMedicamento,
    );
    _obrigatorio(erro, 'dose', dose, comprimentoMaximoDose);
    if (grau == null) {
      erro('grauImportancia', 'Escolha Leve, Moderado ou Grave.');
    }
    final inicio = FormatadorDataHora.lerData(dataInicio);
    if (inicio == null) {
      erro('dataInicio', 'Indique uma data no formato dd/mm/aaaa.');
    }
    if (dataFim.trim().isEmpty) {
      if (edicao && comDataFim) {
        erro('dataFim', regraDataFimNaoRetirada);
      }
    } else {
      final fim = FormatadorDataHora.lerData(dataFim);
      if (fim == null) {
        erro(
          'dataFim',
          'Indique uma data no formato dd/mm/aaaa ou deixe vazia.',
        );
      } else if (inicio != null && fim.isBefore(inicio)) {
        erro('dataFim', 'Não pode ser anterior à data de início.');
      }
    }
    if (horarios.isEmpty) {
      erro('horarios', 'Indique pelo menos uma hora de toma.');
    }
    return erros;
  }

  static void _obrigatorio(
    void Function(String campo, String regra) erro,
    String campo,
    String valor,
    int maximo,
  ) {
    final texto = valor.trim();
    if (texto.isEmpty) {
      erro(campo, 'É obrigatório.');
    } else if (texto.length > maximo) {
      erro(campo, 'Tem no máximo $maximo caracteres.');
    }
  }
}
