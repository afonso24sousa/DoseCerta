import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/agenda_repositorio.dart';
import 'package:dosecerta/data/compromissos_repositorio.dart';
import 'package:dosecerta/models/dados_compromisso_dto.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/tipo_item_agenda.dart';
import 'package:dosecerta/models/vista_agenda.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

const String compromissoJson =
    '{"id":"c-601","fichaId":"f-101","dataHora":"2026-10-12T10:30:00+01:00",'
    '"local":"Centro de Saúde Fictício","motivo":"Consulta de rotina","antecedenciaAvisoMinutos":60}';

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

  const dados = DadosCompromissoDto(
    dataHora: '2026-10-12T10:30:00+01:00',
    local: 'Centro de Saúde Fictício',
    motivo: 'Consulta de rotina',
    antecedenciaAvisoMinutos: 60,
  );

  test('OP-41 envia todos os campos e lê o compromisso', () async {
    adaptador.responderJson(compromissoJson, 201);
    final c = await CompromissosRepositorio(api).registar('f-101', dados);
    expect(adaptador.pedido!.method, 'POST');
    expect(adaptador.pedido!.uri.path, endsWith('/fichas/f-101/compromissos'));
    expect(jsonDecode(adaptador.corpoTexto), dados.toJson());
    expect(c.id, 'c-601');
    expect(c.antecedenciaAvisoMinutos, 60);
  });

  test('OP-42 só envia de e ate quando indicados', () async {
    adaptador.responderJson('[$compromissoJson]');
    final repo = CompromissosRepositorio(api);
    final lista = await repo.listar('f-101');
    expect(adaptador.pedido!.uri.queryParameters, isEmpty);
    expect(lista.single.local, 'Centro de Saúde Fictício');
    adaptador.responderJson('[]');
    await repo.listar('f-101', de: '2026-10-01', ate: '2026-10-31');
    expect(adaptador.pedido!.uri.queryParameters, {
      'de': '2026-10-01',
      'ate': '2026-10-31',
    });
  });

  test('OP-43 e OP-44 usam o caminho do compromisso', () async {
    adaptador.responderJson(compromissoJson);
    final repo = CompromissosRepositorio(api);
    await repo.editar('c-601', dados);
    expect(adaptador.pedido!.method, 'PUT');
    expect(adaptador.pedido!.uri.path, endsWith('/compromissos/c-601'));
    adaptador.responderJson('', 204);
    await repo.remover('c-601');
    expect(adaptador.pedido!.method, 'DELETE');
    expect(adaptador.pedido!.uri.path, endsWith('/compromissos/c-601'));
  });

  test('OP-45 envia a vista e a data e lê tomas e compromissos', () async {
    adaptador.responderJson('''
[{"id":"t-401","tipo":"Toma","fichaId":"f-101","nomeFicha":"Maria Fictícia Santos","titulo":"Metformina 500 mg",
"inicio":"2026-10-12T08:00:00+01:00","estado":"Pendente","confirmavel":true},
{"id":"c-601","tipo":"Compromisso","fichaId":"f-101","nomeFicha":"Maria Fictícia Santos","titulo":"Consulta de rotina",
"inicio":"2026-10-12T10:30:00+01:00","confirmavel":false}]''');
    final itens = await AgendaRepositorio(api)
        .consultar(VistaAgenda.semana, '2026-10-12');
    expect(adaptador.pedido!.uri.path, endsWith('/agenda'));
    expect(adaptador.pedido!.uri.queryParameters, {
      'vista': 'semana',
      'data': '2026-10-12',
    });
    expect(itens.first.tipo, TipoItemAgenda.toma);
    expect(itens.first.estado, EstadoToma.pendente);
    expect(itens.last.tipo, TipoItemAgenda.compromisso);
    expect(itens.last.estado, isNull);
    expect(itens.last.confirmavel, isFalse);
  });
}
