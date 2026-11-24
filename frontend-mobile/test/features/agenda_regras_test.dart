import 'package:dosecerta/core/formatador_data_hora.dart';
import 'package:dosecerta/features/agenda/agenda_controller.dart';
import 'package:dosecerta/features/agenda/form_compromisso_controller.dart';
import 'package:dosecerta/models/vista_agenda.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('instanteDeLisboa usa o desvio de inverno e de verão', () {
    expect(
      FormatadorDataHora.instanteDeLisboa(DateTime(2026, 11, 24), 9, 5),
      '2026-11-24T09:05:00+00:00',
    );
    expect(
      FormatadorDataHora.instanteDeLisboa(DateTime(2026, 7, 1), 10, 30),
      '2026-07-01T10:30:00+01:00',
    );
  });

  test('a semana vai de segunda a domingo e o período desloca-se', () {
    // 24/11/2026 é uma terça-feira.
    final terca = DateTime(2026, 11, 24);
    expect(AgendaController.inicioSemana(terca), DateTime(2026, 11, 23));
    expect(
      AgendaController.rotulo((vista: VistaAgenda.semana, data: terca)),
      '23/11/2026 a 29/11/2026',
    );
    expect(
      AgendaController.rotulo((vista: VistaAgenda.mes, data: terca)),
      'novembro de 2026',
    );
    expect(
      AgendaController.deslocar((vista: VistaAgenda.dia, data: terca), 1).data,
      DateTime(2026, 11, 25),
    );
    expect(
      AgendaController.deslocar((
        vista: VistaAgenda.semana,
        data: terca,
      ), -1).data,
      DateTime(2026, 11, 17),
    );
    expect(
      AgendaController.deslocar((
        vista: VistaAgenda.mes,
        data: DateTime(2026, 12, 31),
      ), 1).data,
      DateTime(2027),
    );
  });

  test('a validação exige data, hora, local e motivo dentro dos limites', () {
    final erros = FormCompromissoController.validar((
      data: '31/02/2026',
      hora: '25:00',
      local: ' ',
      motivo: 'x' * 251,
      antecedencia: 1440,
    ));
    expect(erros.keys, containsAll(['data', 'hora', 'local', 'motivo']));
    final validos = (
      data: '24/11/2026',
      hora: '9:05',
      local: 'Centro de Saúde Fictício',
      motivo: 'Consulta de rotina',
      antecedencia: 30,
    );
    expect(FormCompromissoController.validar(validos), isEmpty);
    final dados = FormCompromissoController.dados(validos);
    expect(dados.dataHora, '2026-11-24T09:05:00+00:00');
    expect(dados.antecedenciaAvisoMinutos, 30);
  });
}
