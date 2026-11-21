import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/convites_repositorio.dart';
import 'package:dosecerta/data/pessoas_repositorio.dart';
import 'package:dosecerta/models/estado_convite.dart';
import 'package:dosecerta/models/novo_convite_dto.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Pessoa de exemplo da documentação da API (OP-18, OP-21, OP-23, OP-24 e OP-26).
const String pessoaJson = '''
{"utilizadorId":"u-12","nome":"Rui Fictício","email":"rui@exemplo.pt",
"papel":"Familiar","principal":false,"provisorio":false}''';

/// Convite de exemplo da documentação da API (OP-19 e OP-20).
const String conviteJson = '''
{"id":"c-801","fichaId":"f-101","nomeFicha":"Maria Fictícia Santos",
"papelOferecido":"Cuidador","paraPrincipal":true,"convidadoPor":"Ana Fictícia",
"estado":"Pendente","criadoEm":"2026-10-07T09:00:00+01:00"}''';

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

  group('DTO da F004', () {
    test('NovoConviteDto envia email, papel e paraPrincipal (OP-19)', () {
      const dados = NovoConviteDto(
        email: 'rui@exemplo.pt',
        papel: Papel.familiar,
      );
      expect(dados.toJson(), {
        'email': 'rui@exemplo.pt',
        'papel': 'Familiar',
        'paraPrincipal': false,
      });
    });

    test('EstadoConvite recusa um valor desconhecido', () {
      expect(EstadoConvite.fromJson('Aceite'), EstadoConvite.aceite);
      expect(() => EstadoConvite.fromJson('Outro'), throwsFormatException);
    });
  });

  group('PessoasRepositorio', () {
    test('OP-18 lê as pessoas da ficha com o papel e o principal', () async {
      adaptador.responderJson(
        '[$pessoaJson,{"utilizadorId":"u-1","nome":"Ana Fictícia",'
        '"email":"ana@exemplo.pt","papel":"Cuidador","principal":true,'
        '"provisorio":true}]',
      );

      final pessoas = await PessoasRepositorio(api).listar('f-1');

      expect(adaptador.pedido!.method, 'GET');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/pessoas');
      expect(pessoas.first.papel, Papel.familiar);
      expect(pessoas.first.principal, isFalse);
      expect(pessoas.last.principal, isTrue);
      expect(pessoas.last.provisorio, isTrue);
    });

    test('OP-23 promover envia PUT com o papel Cuidador', () async {
      adaptador.responderJson(pessoaJson.replaceFirst('Familiar', 'Cuidador'));

      final pessoa = await PessoasRepositorio(api).promover('f-1', 'u-12');

      expect(adaptador.pedido!.method, 'PUT');
      expect(
        adaptador.pedido!.uri.path,
        '/api/v1/fichas/f-1/pessoas/u-12/papel',
      );
      expect(jsonDecode(adaptador.corpoTexto), {'papel': 'Cuidador'});
      expect(pessoa.papel, Papel.cuidador);
    });

    test('OP-24 despromover envia PUT com o papel Familiar', () async {
      adaptador.responderJson(pessoaJson);

      await PessoasRepositorio(api).despromover('f-1', 'u-12');

      expect(adaptador.pedido!.method, 'PUT');
      expect(
        adaptador.pedido!.uri.path,
        '/api/v1/fichas/f-1/pessoas/u-12/papel',
      );
      expect(jsonDecode(adaptador.corpoTexto), {'papel': 'Familiar'});
    });

    test('OP-25 remover usa DELETE no caminho da pessoa', () async {
      adaptador.responderJson('', 204);

      await PessoasRepositorio(api).remover('f-1', 'u-12');

      expect(adaptador.pedido!.method, 'DELETE');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/pessoas/u-12');
    });

    test('OP-26 transferir envia o utilizadorId do destinatário', () async {
      adaptador.responderJson(
        pessoaJson
            .replaceFirst('Familiar', 'Cuidador')
            .replaceFirst('"principal":false', '"principal":true'),
      );

      final pessoa = await PessoasRepositorio(api)
          .transferirPrincipal('f-1', 'u-12');

      expect(adaptador.pedido!.method, 'POST');
      expect(
        adaptador.pedido!.uri.path,
        '/api/v1/fichas/f-1/cuidador-principal',
      );
      expect(jsonDecode(adaptador.corpoTexto), {'utilizadorId': 'u-12'});
      expect(pessoa.principal, isTrue);
    });
  });

  group('ConvitesRepositorio', () {
    test('OP-19 envia o convite e lê o convite criado', () async {
      adaptador.responderJson(conviteJson, 201);

      final convite = await ConvitesRepositorio(api).convidar(
        'f-101',
        const NovoConviteDto(
          email: 'rui@exemplo.pt',
          papel: Papel.cuidador,
          paraPrincipal: true,
        ),
      );

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-101/convites');
      expect(jsonDecode(adaptador.corpoTexto), {
        'email': 'rui@exemplo.pt',
        'papel': 'Cuidador',
        'paraPrincipal': true,
      });
      expect(convite.estado, EstadoConvite.pendente);
      expect(convite.paraPrincipal, isTrue);
    });

    test(
      'OP-20 lê os convites recebidos com a ficha, o papel e quem convidou',
      () async {
        adaptador.responderJson('[$conviteJson]');

        final convites = await ConvitesRepositorio(api).listarRecebidos();

        expect(adaptador.pedido!.method, 'GET');
        expect(adaptador.pedido!.uri.path, '/api/v1/convites');
        expect(convites.single.nomeFicha, 'Maria Fictícia Santos');
        expect(convites.single.papelOferecido, Papel.cuidador);
        expect(convites.single.convidadoPor, 'Ana Fictícia');
      },
    );

    test('OP-21 aceitar lê a nova associação', () async {
      adaptador.responderJson(pessoaJson);

      final pessoa = await ConvitesRepositorio(api).aceitar('c-801');

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/convites/c-801/aceitacao');
      expect(pessoa.utilizadorId, 'u-12');
    });

    test('OP-22 recusar usa POST na recusa', () async {
      adaptador.responderJson('', 204);

      await ConvitesRepositorio(api).recusar('c-801');

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/convites/c-801/recusa');
    });
  });
}
