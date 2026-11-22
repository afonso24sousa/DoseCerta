import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/tratamentos_repositorio.dart';
import 'package:dosecerta/models/dados_tratamento_dto.dart';
import 'package:dosecerta/models/estado_tratamento.dart';
import 'package:dosecerta/models/ficheiro_escolhido.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Tratamento de exemplo da documentação da API (OP-27 a OP-31).
const String tratamentoJson = '''
{"id":"t-1","fichaId":"f-1","medicamento":"Metformina","dose":"500 mg","grauImportancia":"Moderado",
"dataInicio":"2026-10-07","dataFim":"2026-11-05","horarios":["08:00","20:00"],"estado":"EmCurso",
"receitaDocumentoId":"d-1"}''';

const DadosTratamentoDto dadosRegisto = DadosTratamentoDto(
  medicamento: 'Metformina',
  dose: '500 mg',
  grauImportancia: GrauImportancia.moderado,
  dataInicio: '2026-10-07',
  horarios: ['08:00', '20:00'],
);

final FicheiroEscolhido receita = FicheiroEscolhido(
  nome: 'receita.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 4,
  bytes: utf8.encode('%PDF'),
);

void main() {
  late AdaptadorQueGuarda adaptador;
  late TratamentosRepositorio repositorio;

  setUp(() {
    adaptador = AdaptadorQueGuarda();
    repositorio = TratamentosRepositorio(
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

  group('DadosTratamentoDto', () {
    test('não envia os campos vazios (OP-30 mantém o valor deles)', () {
      expect(
        const DadosTratamentoDto(
          dose: '850 mg',
          grauImportancia: GrauImportancia.grave,
        ).toJson(),
        {'dose': '850 mg', 'grauImportancia': 'Grave'},
      );
      expect(const DadosTratamentoDto().vazio, isTrue);
    });
  });

  group('TratamentosRepositorio', () {
    test('OP-27 sem receita envia JSON e lê o tratamento', () async {
      adaptador.responderJson(tratamentoJson, 201);

      final tratamento = await repositorio.registar('f-1', dadosRegisto);

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/tratamentos');
      expect(
        adaptador.pedido!.headers[Headers.contentTypeHeader],
        startsWith('application/json'),
      );
      expect(jsonDecode(adaptador.corpoTexto), {
        'medicamento': 'Metformina',
        'dose': '500 mg',
        'grauImportancia': 'Moderado',
        'dataInicio': '2026-10-07',
        'horarios': ['08:00', '20:00'],
      });
      expect(tratamento.grauImportancia, GrauImportancia.moderado);
      expect(tratamento.estado, EstadoTratamento.emCurso);
      expect(tratamento.horarios, ['08:00', '20:00']);
      expect(tratamento.receitaDocumentoId, 'd-1');
    });

    test('OP-27 com receita envia multipart com as partes dados e receita (API 3.6)', () async {
      adaptador.responderJson(tratamentoJson, 201);

      await repositorio.registar('f-1', dadosRegisto, receita: receita);

      final texto = adaptador.corpoTexto;
      expect(
        adaptador.pedido!.headers[Headers.contentTypeHeader],
        startsWith('multipart/form-data'),
      );
      expect(texto, contains('name="dados"'));
      expect(texto, contains('"medicamento":"Metformina"'));
      expect(texto, contains('name="receita"; filename="receita.pdf"'));
      expect(texto, isNot(contains('name="categoria"')));
    });

    test('OP-28 envia o estado só quando é indicado', () async {
      adaptador.responderJson('[$tratamentoJson]');
      final todos = await repositorio.listar('f-1');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/tratamentos');
      expect(adaptador.pedido!.uri.query, isEmpty);
      expect(todos.single.medicamento, 'Metformina');

      adaptador.responderJson('[]');
      await repositorio.listar('f-1', estado: EstadoTratamento.terminado);
      expect(adaptador.pedido!.uri.queryParameters, {'estado': 'Terminado'});
    });

    test('OP-29 consulta o tratamento sem data de fim', () async {
      adaptador.responderJson(
        tratamentoJson.replaceFirst('"2026-11-05"', 'null'),
      );
      final tratamento = await repositorio.consultar('t-1');
      expect(adaptador.pedido!.method, 'GET');
      expect(adaptador.pedido!.uri.path, '/api/v1/tratamentos/t-1');
      expect(tratamento.dataFim, isNull);
    });

    test('OP-30 usa PUT, em JSON ou em multipart com receita', () async {
      adaptador.responderJson(tratamentoJson);
      await repositorio.atualizar(
        't-1',
        const DadosTratamentoDto(dose: '850 mg'),
      );
      expect(adaptador.pedido!.method, 'PUT');
      expect(adaptador.pedido!.uri.path, '/api/v1/tratamentos/t-1');
      expect(jsonDecode(adaptador.corpoTexto), {'dose': '850 mg'});

      adaptador.responderJson(tratamentoJson);
      await repositorio.atualizar(
        't-1',
        const DadosTratamentoDto(dose: '850 mg'),
        receita: receita,
      );
      expect(adaptador.pedido!.method, 'PUT');
      expect(adaptador.corpoTexto, contains('name="receita"'));
    });

    test('OP-31 termina o tratamento', () async {
      adaptador.responderJson(
        tratamentoJson.replaceFirst('EmCurso', 'Terminado'),
      );
      final tratamento = await repositorio.terminar('t-1');
      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/tratamentos/t-1/termino');
      expect(tratamento.estado, EstadoTratamento.terminado);
    });

    test('os erros da API chegam como ProblemaApi com os campos', () async {
      adaptador.responderJson(
        '{"codigo":"VALIDACAO_INVALIDA","mensagem":"Dados inválidos.",'
        '"campos":[{"campo":"dataFim","regra":"Não pode ser anterior à data de início."}]}',
        400,
      );
      await expectLater(
        repositorio.registar('f-1', dadosRegisto),
        throwsA(
          isA<ProblemaApi>()
              .having((p) => p.codigo, 'codigo', 'VALIDACAO_INVALIDA')
              .having((p) => p.campos.single.campo, 'campo', 'dataFim'),
        ),
      );
    });
  });
}
