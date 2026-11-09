import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/core/parte_ficheiro.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

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
        obterToken: () => 'token-1',
        aoRecusarSessao: () {},
      ),
      dio: Dio()..httpClientAdapter = adaptador,
    );
  });

  group('ApiClient.enviarMultipart (API, secção 3.6)', () {
    test(
      'envia multipart/form-data com o token, pelo método indicado',
      () async {
        adaptador.resposta = ResponseBody.fromString(
          '{"id":"f-1"}',
          201,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );

        final corpo = await api.enviarMultipart(
          'POST',
          '/fichas',
          campos: const [MapEntry('dados', '{"propria":true}')],
        );

        expect(corpo, {'id': 'f-1'});
        final pedido = adaptador.pedido!;
        expect(pedido.method, 'POST');
        expect(pedido.uri.toString(), 'https://api.exemplo.pt/api/v1/fichas');
        expect(pedido.headers['Authorization'], 'Bearer token-1');
        expect(
          pedido.headers[Headers.contentTypeHeader],
          startsWith('multipart/form-data; boundary='),
        );
        expect(pedido.sendTimeout, ApiClient.tempoLimiteFicheiros);
      },
    );

    test('mantém a ordem das partes documento e categoria, com o nome e o tipo de cada ficheiro', () async {
      final temporaria = await Directory.systemTemp.createTemp('i060');
      addTearDown(() => temporaria.delete(recursive: true));
      final pdf = File('${temporaria.path}/receita.pdf')
        ..writeAsBytesSync(utf8.encode('%PDF-1.4 receita'));

      await api.enviarMultipart(
        'PUT',
        '/fichas/f-1',
        campos: const [
          MapEntry('dados', '{"nomeCompleto":"Maria"}'),
          MapEntry('categoria', 'ReceitaMedica'),
          MapEntry('categoria', 'DocumentoIdentificacao'),
        ],
        ficheiros: [
          ParteFicheiro(
            nomeParte: 'documento',
            nomeFicheiro: 'receita.pdf',
            tipoConteudo: 'application/pdf',
            caminho: pdf.path,
          ),
          const ParteFicheiro(
            nomeParte: 'documento',
            nomeFicheiro: 'cartão.png',
            tipoConteudo: 'image/png',
            bytes: [0x89, 0x50, 0x4E, 0x47],
          ),
        ],
      );

      expect(adaptador.pedido!.method, 'PUT');
      final texto = latin1.decode(adaptador.corpo);
      final receita = texto.indexOf('ReceitaMedica');
      final identificacao = texto.indexOf('DocumentoIdentificacao');
      final ficheiroReceita = texto.indexOf('filename="receita.pdf"');
      final ficheiroCartao = texto.indexOf('filename="cart');
      expect(texto, contains('name="dados"'));
      expect(texto, contains('%PDF-1.4 receita'));
      expect(receita, lessThan(identificacao));
      expect(ficheiroReceita, greaterThan(-1));
      expect(ficheiroReceita, lessThan(ficheiroCartao));
      expect('name="documento"'.allMatches(texto), hasLength(2));
      expect(texto, contains('content-type: application/pdf'));
      expect(texto, contains('content-type: image/png'));
    });

    test('um erro da API chega como ProblemaApi com os campos', () async {
      adaptador.resposta = ResponseBody.fromString(
        '{"codigo":"VALIDACAO_INVALIDA","mensagem":"x","campos":'
        '[{"campo":"documentos[0].categoria","regra":"É obrigatória."}]}',
        400,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );

      await expectLater(
        api.enviarMultipart('POST', '/fichas'),
        throwsA(
          isA<ProblemaApi>()
              .having((p) => p.estadoHttp, 'estadoHttp', 400)
              .having(
                (p) => p.campos.single.campo,
                'campo',
                'documentos[0].categoria',
              ),
        ),
      );
    });
  });

  group('ApiClient.obterBytes (OP-17)', () {
    test('devolve o conteúdo do ficheiro em bytes', () async {
      adaptador.resposta = ResponseBody.fromBytes(
        [0x25, 0x50, 0x44, 0x46],
        200,
        headers: {
          Headers.contentTypeHeader: ['application/pdf'],
        },
      );

      final bytes = await api.obterBytes('/documentos/d-1');

      expect(bytes, Uint8List.fromList([0x25, 0x50, 0x44, 0x46]));
      expect(adaptador.pedido!.headers['Authorization'], 'Bearer token-1');
    });

    test('um erro em bytes mantém o código da API (DOCUMENTO_INDISPONIVEL, RF-124)', () async {
      adaptador.resposta = ResponseBody.fromBytes(
        utf8.encode(
          '{"codigo":"DOCUMENTO_INDISPONIVEL","mensagem":"Não foi possível obter o documento."}',
        ),
        503,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );

      await expectLater(
        api.obterBytes('/documentos/d-1'),
        throwsA(
          isA<ProblemaApi>()
              .having((p) => p.codigo, 'codigo', 'DOCUMENTO_INDISPONIVEL')
              .having((p) => p.estadoHttp, 'estadoHttp', 503),
        ),
      );
    });

    test('um erro que não é JSON dá ERRO_INTERNO', () async {
      adaptador.resposta = ResponseBody.fromBytes(
        utf8.encode('<html>Bad Gateway</html>'),
        502,
      );

      await expectLater(
        api.obterBytes('/documentos/d-1'),
        throwsA(
          isA<ProblemaApi>().having(
            (p) => p.codigo,
            'codigo',
            ProblemaApi.codigoErroInterno,
          ),
        ),
      );
    });
  });
}
