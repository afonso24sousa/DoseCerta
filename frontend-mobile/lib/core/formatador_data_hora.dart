/// Apresenta as datas como `dd/mm/aaaa` e converte o que o Utilizador escreve para `AAAA-MM-DD`, o formato da API
/// (RNF-029). Nesta versão só trata datas simples (F002); as horas e os instantes chegam com a F006 e a F008.
class FormatadorDataHora {
  const FormatadorDataHora._();

  static final RegExp _dataApresentada = RegExp(
    r'^(\d{1,2})/(\d{1,2})/(\d{4})$',
  );

  /// Apresenta uma data da API (`AAAA-MM-DD`, ou o início de um instante ISO 8601) como `dd/mm/aaaa`. Os
  /// instantes chegam com o desvio de Lisboa, por isso os 10 primeiros caracteres já são o dia de Lisboa.
  static String apresentarData(String iso) {
    if (iso.length < 10) {
      return iso;
    }
    final ano = iso.substring(0, 4);
    final mes = iso.substring(5, 7);
    final dia = iso.substring(8, 10);
    return '$dia/$mes/$ano';
  }

  /// Apresenta um [DateTime] como `dd/mm/aaaa`.
  static String deData(DateTime data) {
    final dia = data.day.toString().padLeft(2, '0');
    final mes = data.month.toString().padLeft(2, '0');
    return '$dia/$mes/${data.year}';
  }

  /// Converte `dd/mm/aaaa` (também aceita dia e mês com um algarismo) em `AAAA-MM-DD`; devolve `null` se o texto
  /// não for uma data válida (por exemplo 31/02/2020).
  static String? paraIsoData(String texto) {
    final data = lerData(texto);
    if (data == null) {
      return null;
    }
    final mes = data.month.toString().padLeft(2, '0');
    final dia = data.day.toString().padLeft(2, '0');
    return '${data.year.toString().padLeft(4, '0')}-$mes-$dia';
  }

  /// Lê `dd/mm/aaaa` como data (sem hora); devolve `null` se não for uma data válida.
  static DateTime? lerData(String texto) {
    final partes = _dataApresentada.firstMatch(texto.trim());
    if (partes == null) {
      return null;
    }
    final dia = int.parse(partes.group(1)!);
    final mes = int.parse(partes.group(2)!);
    final ano = int.parse(partes.group(3)!);
    final data = DateTime(ano, mes, dia);
    if (data.year != ano || data.month != mes || data.day != dia) {
      return null;
    }
    return data;
  }
}
