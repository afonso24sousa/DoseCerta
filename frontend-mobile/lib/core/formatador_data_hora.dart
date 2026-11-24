import 'package:timezone/data/latest_10y.dart' as fusos;
import 'package:timezone/timezone.dart' as tz;

/// Apresenta as datas como `dd/mm/aaaa` e as horas como `hh:mm` e converte o que o Utilizador escreve para
/// `AAAA-MM-DD`, o formato da API (RNF-029). Os instantes da API já vêm em hora de Lisboa, com o desvio, por isso a
/// data e a hora apresentadas são as que vêm no texto, sem conversão para o fuso do telemóvel (F006).
class FormatadorDataHora {
  const FormatadorDataHora._();

  static tz.Location? _lisboa;

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

  /// Apresenta a hora de um instante da API (`AAAA-MM-DDThh:mm:ss+hh:mm`) como `hh:mm`, tal como vem.
  static String apresentarHora(String iso) {
    if (iso.length < 16) {
      return iso;
    }
    return iso.substring(11, 16);
  }

  /// Devolve o dia de Lisboa (sem hora) no [instante], que é o calendário do servidor. O telemóvel e a pipeline
  /// podem estar noutro fuso (por exemplo UTC), o que muda o dia perto da meia-noite.
  static DateTime diaDeLisboa(DateTime instante) {
    final local = tz.TZDateTime.from(instante, _fusoLisboa);
    return DateTime(local.year, local.month, local.day);
  }

  /// Devolve a data e a hora de Lisboa no [instante], ao minuto, por exemplo para propor o momento atual num
  /// formulário (F009, RF-090).
  static DateTime emLisboa(DateTime instante) {
    final local = tz.TZDateTime.from(instante, _fusoLisboa);
    return DateTime(
      local.year,
      local.month,
      local.day,
      local.hour,
      local.minute,
    );
  }

  /// Converte a data e a hora de Lisboa [dataHora] (os campos do [DateTime], sem fuso) num instante ISO 8601 com o
  /// desvio de Lisboa, o formato dos instantes enviados à API (por exemplo `2026-10-07T15:05:00+01:00`). O desvio
  /// é o da hora de verão ou de inverno nesse dia.
  static String instanteDeLisboa(DateTime dataHora) {
    final local = tz.TZDateTime(
      _fusoLisboa,
      dataHora.year,
      dataHora.month,
      dataHora.day,
      dataHora.hour,
      dataHora.minute,
    );
    final desvio = local.timeZoneOffset;
    final sinal = desvio.isNegative ? '-' : '+';
    final horasDesvio = desvio.inHours.abs().toString().padLeft(2, '0');
    final minutosDesvio = (desvio.inMinutes.abs() % 60).toString().padLeft(
      2,
      '0',
    );
    final hora = local.hour.toString().padLeft(2, '0');
    final minuto = local.minute.toString().padLeft(2, '0');
    return '${paraIso(local)}T$hora:$minuto:00$sinal$horasDesvio:$minutosDesvio';
  }

  static tz.Location get _fusoLisboa => _lisboa ??= () {
    fusos.initializeTimeZones();
    return tz.getLocation('Europe/Lisbon');
  }();

  /// Apresenta um [DateTime] como `AAAA-MM-DD`, o formato das datas da API.
  static String paraIso(DateTime data) {
    final mes = data.month.toString().padLeft(2, '0');
    final dia = data.day.toString().padLeft(2, '0');
    return '${data.year.toString().padLeft(4, '0')}-$mes-$dia';
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
