import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/calendario_repositorio.dart';
import 'package:dosecerta/data/tomas_repositorio.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/toma_calendario_dto.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Tomas de exemplo da documentação da API (OP-32).
const String tomasJson = '''
[{"id":"t-402","tratamentoId":"tr-201","medicamento":"Metformina","dose":"500 mg",
"grauImportancia":"Moderado","previstaEm":"2026-10-06T20:00:00+01:00","estado":"ConfirmadaComAtraso",
"confirmadaPor":{"id":"u-11","nome":"Ana Fictícia"},"confirmadaEm":"2026-10-06T21:30:00+01:00"},
{"id":"t-401","tratamentoId":"tr-201","medicamento":"Metformina","dose":"500 mg",
"grauImportancia":"Moderado","previstaEm":"2026-10-07T08:00:00+01:00","estado":"Pendente",
"confirmadaPor":null,"confirmadaEm":null}]''';

void main() {
  late AdaptadorQueGuarda adaptador;
  late ApiClient api;

  setUp(() {
    adaptador = AdaptadorQueGuarda();
    api = ApiClient(
      configuracao: ConfiguracaoApi.validar(
        'https://api.exemplo.pt',
        modoDebug: false,
      ),
      authInterceptor: AuthInterceptor(
        obterToken: () => 't',
        aoRecusarSessao: () {},
      ),
      dio: Dio()..httpClientAdapter = adaptador,
    );
  });

  group('EstadoToma', () {
    test('lê os quatro estados da API e recusa um desconhecido', () {
      expect(
        [
          'Pendente',
          'Confirmada',
          'ConfirmadaComAtraso',
          'NaoConfirmada',
        ].map(EstadoToma.fromJson),
        EstadoToma.values,
      );
      expect(() => EstadoToma.fromJson('Prevista'), throwsFormatException);
    });
  });

  group('TomasRepositorio', () {
    test(
      'OP-32 envia de e ate e lê as tomas com quem confirmou (RF-063, RF-065)',
      () async {
        adaptador.responderJson(tomasJson);

        final tomas = await TomasRepositorio(api)
            .listar('f-1', '2026-10-05', '2026-10-11');

        expect(adaptador.pedido!.method, 'GET');
        expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/tomas');
        expect(adaptador.pedido!.uri.queryParameters, {
          'de': '2026-10-05',
          'ate': '2026-10-11',
        });
        expect(tomas.first.estado, EstadoToma.confirmadaComAtraso);
        expect(tomas.first.grauImportancia, GrauImportancia.moderado);
        expect(tomas.first.confirmadaPor!.nome, 'Ana Fictícia');
        expect(tomas.first.confirmadaEm, '2026-10-06T21:30:00+01:00');
        expect(tomas.last.estado, EstadoToma.pendente);
        expect(tomas.last.confirmadaPor, isNull);
        expect(tomas.last.confirmadaEm, isNull);
      },
    );

    test('OP-33 usa POST sem corpo e lê a toma confirmada', () async {
      adaptador.responderJson(
        '{"id":"t-401","tratamentoId":"tr-201","medicamento":"Metformina",'
        '"dose":"500 mg","grauImportancia":"Moderado",'
        '"previstaEm":"2026-10-07T08:00:00+01:00","estado":"Confirmada",'
        '"confirmadaPor":{"id":"u-11","nome":"Ana Fictícia"},'
        '"confirmadaEm":"2026-10-07T08:05:00+01:00"}',
      );

      final toma = await TomasRepositorio(api).confirmar('t-401');

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/tomas/t-401/confirmacao');
      expect(adaptador.corpo, isEmpty);
      expect(toma.estado, EstadoToma.confirmada);
      expect(toma.confirmadaPor!.id, 'u-11');
    });
  });

  group('CalendarioRepositorio', () {
    test('OP-34 lê as tomas a sincronizar, com a ficha', () async {
      adaptador.responderJson(
        '[{"tomaId":"t-403","fichaId":"f-101","nomeFicha":"Maria Fictícia Santos",'
        '"medicamento":"Metformina","dose":"500 mg",'
        '"previstaEm":"2026-10-07T20:00:00+01:00"}]',
      );

      final tomas = await CalendarioRepositorio(api).sincronizar();

      expect(adaptador.pedido!.method, 'GET');
      expect(adaptador.pedido!.uri.path, '/api/v1/calendario/tomas');
      expect(tomas.single.nomeFicha, 'Maria Fictícia Santos');
      expect(tomas.single.previstaEm, '2026-10-07T20:00:00+01:00');
    });

    test('a toma sincronizada guarda-se e lê-se de novo sem perder campos', () {
      const toma = TomaCalendarioDto(
        tomaId: 't-1',
        fichaId: 'f-1',
        nomeFicha: 'João Fictício Lopes',
        medicamento: 'Insulina',
        dose: '10 UI',
        previstaEm: '2026-11-18T12:00:00+00:00',
      );

      final lida = TomaCalendarioDto.fromJson(toma.toJson());

      expect(lida.toJson(), toma.toJson());
    });
  });
}
