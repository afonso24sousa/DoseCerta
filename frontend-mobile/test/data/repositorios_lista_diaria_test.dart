import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/lista_diaria_repositorio.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/tipo_item_agenda.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Lista diária de exemplo da documentação da API (OP-50).
const String listaDiariaJson = '''
[{"tipo":"Toma","id":"t-409","fichaId":"f-1","nomeFicha":"Maria Fictícia Santos","titulo":"Paracetamol 1 g",
"previstaEm":"2026-10-06T23:30:00+01:00","estado":"Pendente","confirmavel":true},
{"tipo":"Toma","id":"t-401","fichaId":"f-1","nomeFicha":"Maria Fictícia Santos","titulo":"Metformina 500 mg",
"previstaEm":"2026-10-07T08:00:00+01:00","estado":"Confirmada","confirmadaPor":"Ana Fictícia",
"confirmadaEm":"2026-10-07T08:05:00+01:00","confirmavel":false},
{"tipo":"Compromisso","id":"c-601","fichaId":"f-1","nomeFicha":"Maria Fictícia Santos",
"titulo":"Consulta de rotina","previstaEm":"2026-10-07T10:30:00+01:00","confirmavel":false}]''';

void main() {
  late AdaptadorQueGuarda adaptador;
  late ListaDiariaRepositorio repositorio;

  setUp(() {
    adaptador = AdaptadorQueGuarda();
    repositorio = ListaDiariaRepositorio(
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

  test('TipoItemAgenda lê Toma e Compromisso e recusa um desconhecido', () {
    expect(
      ['Toma', 'Compromisso'].map(TipoItemAgenda.fromJson),
      TipoItemAgenda.values,
    );
    expect(() => TipoItemAgenda.fromJson('Tarefa'), throwsFormatException);
  });

  test('OP-50 sem data deixa o servidor escolher o dia de hoje e lê tomas e compromissos (RF-100, RF-101)', () async {
    adaptador.responderJson(listaDiariaJson);

    final itens = await repositorio.consultar();

    expect(adaptador.pedido!.method, 'GET');
    expect(adaptador.pedido!.uri.path, '/api/v1/lista-diaria');
    expect(adaptador.pedido!.uri.queryParameters, isEmpty);
    expect(itens, hasLength(3));
    expect(itens[0].estado, EstadoToma.pendente);
    expect(itens[0].confirmavel, isTrue);
    expect(itens[0].nomeFicha, 'Maria Fictícia Santos');
    expect(itens[1].confirmadaPor, 'Ana Fictícia');
    expect(itens[1].confirmadaEm, '2026-10-07T08:05:00+01:00');
    expect(itens[2].tipo, TipoItemAgenda.compromisso);
    expect(itens[2].eToma, isFalse);
    expect(itens[2].estado, isNull);
    expect(itens[2].confirmavel, isFalse);
  });

  test('OP-50 envia a data pedida', () async {
    adaptador.responderJson('[]');

    final itens = await repositorio.consultar(data: '2026-10-08');

    expect(adaptador.pedido!.uri.queryParameters, {'data': '2026-10-08'});
    expect(itens, isEmpty);
  });
}
