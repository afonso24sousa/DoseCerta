import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/documentos_repositorio.dart';
import 'package:dosecerta/data/dominios_repositorio.dart';
import 'package:dosecerta/data/fichas_repositorio.dart';
import 'package:dosecerta/models/categoria_documento.dart';
import 'package:dosecerta/models/dados_ficha_dto.dart';
import 'package:dosecerta/models/definicao_principal_dto.dart';
import 'package:dosecerta/models/documento_para_envio.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/ficheiro_escolhido.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Ficha de exemplo da documentação da API (OP-09 a OP-11).
const String fichaJson = '''
{"id":"f-1","nomeCompleto":"Maria Fictícia Santos","dataNascimento":"1941-05-17",
"contactoEmergencia":"+351 910 000 001","eProprio":false,"estado":"Ativa",
"grauAutonomia":{"id":2,"designacao":"Autonomia parcial"},
"condicoesSaude":[{"id":1,"designacao":"Diabetes"}],"alergias":[],
"meuPapel":"Cuidador","souPrincipal":true}''';

const DadosFichaDto dadosTerceiro = DadosFichaDto(
  propria: false,
  nomeCompleto: 'Maria Fictícia Santos',
  dataNascimento: '1941-05-17',
  contactoEmergencia: '+351 910 000 001',
  grauAutonomiaId: 2,
  condicoesSaudeIds: [1],
  alergiasIds: [1],
  cuidadorPrincipal: DefinicaoPrincipalDto.convidar('rui@exemplo.pt'),
);

DocumentoParaEnvio documento(CategoriaDocumento categoria, String nome) {
  return DocumentoParaEnvio(
    categoria: categoria,
    ficheiro: FicheiroEscolhido(
      nome: nome,
      tipoConteudo: nome.endsWith('.pdf') ? 'application/pdf' : 'image/png',
      tamanhoBytes: 4,
      bytes: utf8.encode('C-$nome'),
    ),
  );
}

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

  group('DadosFichaDto (D1)', () {
    test('criar ficha de terceiro envia propria e cuidadorPrincipal', () {
      expect(dadosTerceiro.toJsonCriar(), {
        'propria': false,
        'nomeCompleto': 'Maria Fictícia Santos',
        'dataNascimento': '1941-05-17',
        'contactoEmergencia': '+351 910 000 001',
        'grauAutonomiaId': 2,
        'condicoesSaudeIds': [1],
        'alergiasIds': [1],
        'cuidadorPrincipal': {'eu': false, 'email': 'rui@exemplo.pt'},
      });
    });

    test('ficha própria não envia o Cuidador principal (RF-030)', () {
      const propria = DadosFichaDto(
        propria: true,
        nomeCompleto: 'Ana',
        dataNascimento: '1990-01-02',
        contactoEmergencia: '910000000',
        cuidadorPrincipal: DefinicaoPrincipalDto.eu(),
      );
      final json = propria.toJsonCriar();
      expect(json['propria'], isTrue);
      expect(json.containsKey('cuidadorPrincipal'), isFalse);
      expect(json['grauAutonomiaId'], isNull);
    });

    test('editar não envia propria nem cuidadorPrincipal (OP-11)', () {
      final json = dadosTerceiro.toJsonEditar();
      expect(json.containsKey('propria'), isFalse);
      expect(json.containsKey('cuidadorPrincipal'), isFalse);
      expect(json['nomeCompleto'], 'Maria Fictícia Santos');
    });

    test('eu=true envia só eu (RF-024)', () {
      expect(const DefinicaoPrincipalDto.eu().toJson(), {'eu': true});
    });
  });

  group('FichasRepositorio', () {
    test('OP-07 lê as fichas e filtra pelo estado', () async {
      adaptador.responderJson(
        '[{"id":"f-1","nomeCompleto":"A minha ficha","eProprio":true,'
        '"estado":"Arquivada","meuPapel":"Cuidador","souPrincipal":true},'
        '{"id":"f-2","nomeCompleto":"João","eProprio":true,'
        '"estado":"Ativa","meuPapel":"Familiar","souPrincipal":false}]',
      );

      final fichas = await FichasRepositorio(api)
          .listar(estado: EstadoFicha.arquivada);

      expect(adaptador.pedido!.uri.query, 'estado=Arquivada');
      expect(fichas.first.estado, EstadoFicha.arquivada);
      expect(fichas.first.eMinhaFichaPropria, isTrue);
      expect(fichas.last.meuPapel, Papel.familiar);
      expect(fichas.last.eMinhaFichaPropria, isFalse);
    });

    test('OP-07 sem estado não envia o parâmetro', () async {
      adaptador.responderJson('[]');
      await FichasRepositorio(api).listar();
      expect(adaptador.pedido!.uri.query, isEmpty);
    });

    test('OP-09 sem documentos envia JSON e lê a ficha', () async {
      adaptador.responderJson(fichaJson, 201);

      final ficha = await FichasRepositorio(api).criar(dadosTerceiro);

      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas');
      expect(
        adaptador.pedido!.headers[Headers.contentTypeHeader],
        startsWith('application/json'),
      );
      expect(jsonDecode(adaptador.corpoTexto), dadosTerceiro.toJsonCriar());
      expect(ficha.grauAutonomia?.designacao, 'Autonomia parcial');
      expect(ficha.souCuidador, isTrue);
      expect(ficha.ativa, isTrue);
    });

    test('OP-09 com documentos envia dados e os pares categoria e documento pela ordem (API 3.6)', () async {
      adaptador.responderJson(fichaJson, 201);

      await FichasRepositorio(api).criar(
        dadosTerceiro,
        documentos: [
          documento(CategoriaDocumento.receitaMedica, 'receita.pdf'),
          documento(CategoriaDocumento.documentoIdentificacao, 'id.png'),
        ],
      );

      final texto = adaptador.corpoTexto;
      expect(
        adaptador.pedido!.headers[Headers.contentTypeHeader],
        startsWith('multipart/form-data'),
      );
      expect(texto, contains('name="dados"'));
      expect(texto, contains('"cuidadorPrincipal":{"eu":false'));
      expect(
        texto.indexOf('ReceitaMedica'),
        lessThan(texto.indexOf('DocumentoIdentificacao')),
      );
      expect(
        texto.indexOf('filename="receita.pdf"'),
        lessThan(texto.indexOf('filename="id.png"')),
      );
      expect('name="documento"'.allMatches(texto), hasLength(2));
      expect('name="categoria"'.allMatches(texto), hasLength(2));
    });

    test('OP-11 com documento usa PUT multipart sem propria', () async {
      adaptador.responderJson(fichaJson);

      await FichasRepositorio(api).editar(
        'f-1',
        dadosTerceiro,
        documentos: [
          documento(CategoriaDocumento.relatorioClinico, 'relatorio.pdf'),
        ],
      );

      expect(adaptador.pedido!.method, 'PUT');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1');
      expect(adaptador.corpoTexto, isNot(contains('propria')));
      expect(adaptador.corpoTexto, contains('RelatorioClinico'));
    });

    test('OP-12, OP-13 e OP-14 usam o método e o caminho da API', () async {
      final repositorio = FichasRepositorio(api);
      adaptador.resposta = ResponseBody.fromString('', 204);
      await repositorio.arquivar('f-1');
      expect(adaptador.pedido!.method, 'POST');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/arquivo');

      adaptador.resposta = ResponseBody.fromString('', 204);
      await repositorio.reativar('f-1');
      expect(adaptador.pedido!.method, 'DELETE');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/arquivo');

      adaptador.resposta = ResponseBody.fromString('', 204);
      await repositorio.eliminar('f-1');
      expect(adaptador.pedido!.method, 'DELETE');
      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1');
    });
  });

  group('DocumentosRepositorio', () {
    test('OP-15 envia as partes categoria e ficheiro', () async {
      adaptador.responderJson(
        '{"id":"d-1","categoria":"RelatorioClinico","nomeOriginal":"relatorio.pdf",'
        '"formato":"PDF","tamanhoBytes":482311,"carregadoEm":"2026-10-07T09:10:00+01:00"}',
        201,
      );

      final dto = await DocumentosRepositorio(api).carregar(
        'f-1',
        documento(CategoriaDocumento.relatorioClinico, 'relatorio.pdf'),
      );

      expect(adaptador.pedido!.uri.path, '/api/v1/fichas/f-1/documentos');
      expect(adaptador.corpoTexto, contains('name="ficheiro"'));
      expect(adaptador.corpoTexto, isNot(contains('name="documento"')));
      expect(adaptador.corpoTexto, contains('RelatorioClinico'));
      expect(dto.categoria, CategoriaDocumento.relatorioClinico);
      expect(dto.tamanhoBytes, 482311);
    });

    test('OP-16 lê a lista e OP-17 descarrega o ficheiro', () async {
      adaptador.responderJson(
        '[{"id":"d-1","categoria":"ReceitaMedica","nomeOriginal":"receita.pdf",'
        '"formato":"PDF","tamanhoBytes":125,"carregadoEm":"2026-11-09T20:38:11+00:00"}]',
      );
      final repositorio = DocumentosRepositorio(api);

      final lista = await repositorio.listar('f-1');
      expect(lista.single.categoria.rotulo, 'Receita médica');

      adaptador.resposta = ResponseBody.fromBytes([1, 2, 3], 200);
      final bytes = await repositorio.descarregar('d-1');
      expect(adaptador.pedido!.uri.path, '/api/v1/documentos/d-1');
      expect(bytes, [1, 2, 3]);
    });
  });

  test('DominiosRepositorio lê as três listas de OP-08', () async {
    adaptador.responderJson(
      '{"condicoesSaude":[{"id":1,"designacao":"Diabetes"}],'
      '"alergias":[{"id":1,"designacao":"Medicamentos"}],'
      '"grausAutonomia":[{"id":2,"designacao":"Autonomia parcial"}]}',
    );

    final dominios = await DominiosRepositorio(api).obterNecessidades();

    expect(adaptador.pedido!.uri.path, '/api/v1/dominios/necessidades');
    expect(dominios.condicoesSaude.single.designacao, 'Diabetes');
    expect(dominios.alergias.single.id, 1);
    expect(dominios.grausAutonomia.single.designacao, 'Autonomia parcial');
  });
}
