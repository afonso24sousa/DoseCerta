import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../models/toma_calendario_dto.dart';

/// Tomas da última sincronização bem-sucedida (RF-061), do Utilizador a que pertencem.
class TomasSincronizadas {
  /// Cria o registo da sincronização.
  const TomasSincronizadas({
    required this.utilizadorId,
    required this.tomas,
    required this.ultimaSincronizacao,
  });

  /// Utilizador da sessão que sincronizou.
  final String utilizadorId;

  /// Tomas `Pendente` das 48 horas seguintes (OP-34).
  final List<TomaCalendarioDto> tomas;

  /// Instante da última sincronização bem-sucedida.
  final DateTime ultimaSincronizacao;
}

/// Guarda no dispositivo (`shared_preferences`) as tomas sincronizadas (modelo de classes, secção 7). Não guarda
/// dados de saúde além dos que o aviso mostra (ficha, medicamento, dose e hora).
class TomasSincronizadasStore {
  /// Cria o armazenamento.
  const TomasSincronizadasStore();

  /// Chave no `shared_preferences`.
  static const String chave = 'dosecerta.tomas_sincronizadas';

  /// Lê a última sincronização; `null` se não houver nenhuma ou se o registo estiver ilegível.
  Future<TomasSincronizadas?> ler() async {
    final preferencias = await SharedPreferences.getInstance();
    final texto = preferencias.getString(chave);
    if (texto == null) {
      return null;
    }
    try {
      final json = jsonDecode(texto) as Map<String, dynamic>;
      return TomasSincronizadas(
        utilizadorId: json['utilizadorId'] as String,
        tomas: [
          for (final toma in json['tomas'] as List<dynamic>)
            TomaCalendarioDto.fromJson(toma as Map<String, dynamic>),
        ],
        ultimaSincronizacao: DateTime.parse(
          json['ultimaSincronizacao'] as String,
        ),
      );
    } on Object {
      return null;
    }
  }

  /// Guarda as [tomas] sincronizadas por [utilizadorId] no [instante], em vez das anteriores.
  Future<void> guardar(
    String utilizadorId,
    List<TomaCalendarioDto> tomas,
    DateTime instante,
  ) async {
    final preferencias = await SharedPreferences.getInstance();
    await preferencias.setString(
      chave,
      jsonEncode({
        'utilizadorId': utilizadorId,
        'tomas': [for (final toma in tomas) toma.toJson()],
        'ultimaSincronizacao': instante.toUtc().toIso8601String(),
      }),
    );
  }

  /// Retira a toma [tomaId] das tomas guardadas, depois de confirmada, sem mudar o instante da sincronização: a
  /// toma deixa de contar no E26 sem esperar pela sincronização seguinte.
  Future<void> retirar(String tomaId) async {
    final atuais = await ler();
    if (atuais == null || atuais.tomas.every((toma) => toma.tomaId != tomaId)) {
      return;
    }
    await guardar(atuais.utilizadorId, [
      for (final toma in atuais.tomas)
        if (toma.tomaId != tomaId) toma,
    ], atuais.ultimaSincronizacao);
  }

  /// Apaga as tomas sincronizadas (ao terminar a sessão).
  Future<void> limpar() async {
    final preferencias = await SharedPreferences.getInstance();
    await preferencias.remove(chave);
  }
}
