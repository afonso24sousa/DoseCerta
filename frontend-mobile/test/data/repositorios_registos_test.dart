import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/registos_repositorio.dart';
import 'package:dosecerta/models/nova_nota_dto.dart';
import 'package:dosecerta/models/nova_ocorrencia_dto.dart';
import 'package:dosecerta/models/tipo_registo.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Ocorrência de exemplo da documentação da API (OP-46 e OP-48).
const String ocorrenciaJson = '''
{"id":"r-701","fichaId":"f-1","tipo":"Ocorrencia","descricao":"Queda ligeira sem lesões.",
"autor":{"id":"u-11","nome":"Ana Fictícia"},"ocorridaEm":"2026-10-07T15:05:00+01:00",
"criadaEm":"2026-10-07T15:20:00+01:00"}''';

/// Nota de exemplo da documentação da API (OP-47 e OP-49).
const String notaJson = '''
{"id":"r-702","fichaId":"f-1","tipo":"Nota","descricao":"Reforçar a hidratação durante a tarde.",
"autor":{"id":"u-11","nome":"Ana Fictícia"},"ocorridaEm":null,"criadaEm":"2026-10-07T15:20:00+01:00"}''';

void main() {
  late AdaptadorQueGuarda adaptador;
  late RegistosRepositorio repositorio;

  setUp(() {
    adaptador = AdaptadorQueGuarda();
    repositorio = RegistosRepositorio(
      ApiClient(
        configuracao: ConfiguracaoApi.validar(
          'https://api.exemplo.pt',
          modoDebug: false,
        ),
        authInterceptor: AuthInterceptor(
          obterToken: () => 't',
          aoRecusarSessao: () {},
        ),
        dio: Dio()..httpClientAdapter = adaptador,
      ),
    );
  });

  group('TipoRegisto', () {
    test('lê os dois tipos da API e recusa um desconhecido', () {
      expect(
        ['Ocorrencia', 'Nota'].map(TipoRegisto.fromJson),
        TipoRegisto.values,
      );
      expect(() => TipoRegisto.fromJson('Comentario'), throwsFormatException);
    });
  });

  group('RegistosRepositorio', () {
    test('OP-46 envia a descrição e ocorridaEm e lê a ocorrência com o autor (RF-090, RF-092)', () async {
      adaptador.responderJson(ocorrenciaJson, 201);

      final registo = await repositorio.registarOcorrencia(
        'f-1',
        const NovaOcorrenciaDto(
          descricao: 'Queda ligeira sem lesões.',
          ocorridaEm: '2026-10-07T15:05:00+01:00',
        ),
      );

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/ocorrencias');
      expect(jsonDecode(adaptador.corpoTexto), {
        'descricao': 'Queda ligeira sem lesões.',
        'ocorridaEm': '2026-10-07T15:05:00+01:00',
      });
      expect(registo.tipo, TipoRegisto.ocorrencia);
      expect(registo.autor.nome, 'Ana Fictícia');
      expect(registo.ocorridaEm, '2026-10-07T15:05:00+01:00');
      expect(registo.criadaEm, '2026-10-07T15:20:00+01:00');
    });

    test(
      'OP-47 envia só a descrição e lê a nota sem ocorridaEm (RF-091)',
      () async {
        adaptador.responderJson(notaJson, 201);

        final registo = await repositorio.registarNota(
          'f-1',
          const NovaNotaDto(
            descricao: 'Reforçar a hidratação durante a tarde.',
          ),
        );

        expect(adaptador.pedido!.method, 'POST');
        expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/notas');
        expect(jsonDecode(adaptador.corpoTexto), {
          'descricao': 'Reforçar a hidratação durante a tarde.',
        });
        expect(registo.tipo, TipoRegisto.nota);
        expect(registo.ocorridaEm, isNull);
      },
    );

    test(
      'OP-48 pede tipo=Ocorrencia, sem datas, e lê a lista (RF-094)',
      () async {
        adaptador.responderJson('[$ocorrenciaJson]');

        final registos = await repositorio.listarOcorrencias('f-1');

        expect(adaptador.pedido!.method, 'GET');
        expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/registos');
        expect(adaptador.pedido!.uri.queryParameters, {'tipo': 'Ocorrencia'});
        expect(registos.single.descricao, 'Queda ligeira sem lesões.');
      },
    );

    test('OP-49 pede tipo=Nota e envia de e ate quando indicados', () async {
      adaptador.responderJson('[$notaJson]');

      final registos = await repositorio.listarNotas(
        'f-1',
        de: '2026-10-01',
        ate: '2026-10-07',
      );

      expect(adaptador.pedido!.uri.queryParameters, {
        'tipo': 'Nota',
        'de': '2026-10-01',
        'ate': '2026-10-07',
      });
      expect(registos.single.tipo, TipoRegisto.nota);
    });
  });
}
