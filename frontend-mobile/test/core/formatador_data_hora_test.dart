import 'package:dosecerta/core/formatador_data_hora.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('FormatadorDataHora: horas e dia de Lisboa (I069, RNF-029)', () {
    test(
      'a hora é a que vem da API, sem conversão para o fuso do telemóvel',
      () {
        expect(
          FormatadorDataHora.apresentarHora('2026-07-01T20:00:00+01:00'),
          '20:00',
        );
        expect(
          FormatadorDataHora.apresentarData('2026-07-01T20:00:00+01:00'),
          '01/07/2026',
        );
      },
    );

    test('no verão, às 23:30 UTC já é o dia seguinte em Lisboa', () {
      expect(
        FormatadorDataHora.diaDeLisboa(DateTime.utc(2026, 7, 1, 23, 30)),
        DateTime(2026, 7, 2),
      );
    });

    test('no inverno, Lisboa está em UTC', () {
      expect(
        FormatadorDataHora.diaDeLisboa(DateTime.utc(2026, 11, 18, 23, 30)),
        DateTime(2026, 11, 18),
      );
    });

    test('a data da API é AAAA-MM-DD', () {
      expect(FormatadorDataHora.paraIso(DateTime(2026, 1, 5)), '2026-01-05');
    });
  });
}
