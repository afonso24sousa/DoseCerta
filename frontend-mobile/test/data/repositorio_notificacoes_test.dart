import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/data/notificacoes_repositorio.dart';
import 'package:dosecerta/models/estado_envio_email.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/models/tipo_notificacao.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/adaptador_http.dart';

/// Notificações de exemplo da documentação da API (OP-51).
const String notificacoesJson = '''
[{"id":"n-2","tipo":"TomaGraveFamiliares","mensagem":"A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.",
"fichaId":"f-1","nomeFicha":"Maria Fictícia Santos","medicamento":"Insulina","previstaEm":"2026-10-07T10:00:00+01:00",
"criadaEm":"2026-10-07T11:00:00+01:00","email":"NaoEntregue"},
{"id":"n-1","tipo":"ConviteRecebido","mensagem":"Convite para a ficha de João Fictício Lopes.",
"fichaId":"f-2","nomeFicha":"João Fictício Lopes","medicamento":null,"previstaEm":null,
"criadaEm":"2026-10-06T09:00:00+01:00","email":"Pendente"}]''';

void main() {
  late AdaptadorQueGuarda adaptador;
  late NotificacoesRepositorio repositorio;

  setUp(() {
    adaptador = AdaptadorQueGuarda();
    repositorio = NotificacoesRepositorio(
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

  group('NotificacoesRepositorio (OP-51)', () {
    test('sem filtro pede GET /notificacoes sem query e lê os DTO', () async {
      adaptador.responderJson(notificacoesJson);

      final lista = await repositorio.listar();

      expect(adaptador.pedido!.method, 'GET');
      expect(adaptador.pedido!.uri.path, '/api/v1/notificacoes');
      expect(adaptador.pedido!.uri.query, isEmpty);
      expect(lista, hasLength(2));
      expect(lista[0].tipo, TipoNotificacao.tomaGraveFamiliares);
      expect(lista[0].medicamento, 'Insulina');
      expect(lista[0].previstaEm, '2026-10-07T10:00:00+01:00');
      expect(lista[0].email, EstadoEnvioEmail.naoEntregue);
      expect(lista[1].tipo, TipoNotificacao.conviteRecebido);
      expect(lista[1].medicamento, isNull);
      expect(lista[1].previstaEm, isNull);
      expect(lista[1].email, EstadoEnvioEmail.pendente);
    });

    test('envia de e ate só quando são indicados', () async {
      adaptador.responderJson('[]');
      await repositorio.listar(de: '2026-10-01', ate: '2026-10-31');
      expect(adaptador.pedido!.uri.queryParameters, {
        'de': '2026-10-01',
        'ate': '2026-10-31',
      });

      adaptador.responderJson('[]');
      await repositorio.listar(ate: '2026-10-31');
      expect(adaptador.pedido!.uri.queryParameters, {'ate': '2026-10-31'});
    });

    test('400 VALIDACAO_INVALIDA chega como ProblemaApi com o campo', () async {
      adaptador.responderJson(
        '{"codigo":"VALIDACAO_INVALIDA","mensagem":"Pedido inválido.",'
        '"campos":[{"campo":"ate","regra":"Tem de ser igual ou posterior a de."}]}',
        400,
      );

      await expectLater(
        repositorio.listar(de: '2026-10-31', ate: '2026-10-01'),
        throwsA(
          isA<ProblemaApi>()
              .having((p) => p.codigo, 'codigo', 'VALIDACAO_INVALIDA')
              .having((p) => p.campos.single.campo, 'campo', 'ate'),
        ),
      );
    });

    test('401 NAO_AUTENTICADO chega como ProblemaApi', () async {
      adaptador.responderJson(
        '{"codigo":"NAO_AUTENTICADO","mensagem":"Sessão inválida."}',
        401,
      );

      await expectLater(
        repositorio.listar(),
        throwsA(
          isA<ProblemaApi>().having(
            (p) => p.codigo,
            'codigo',
            'NAO_AUTENTICADO',
          ),
        ),
      );
    });

    test('um tipo desconhecido é recusado ao ler', () async {
      adaptador.responderJson(
        notificacoesJson.replaceFirst('TomaGraveFamiliares', 'Outro'),
      );
      await expectLater(repositorio.listar(), throwsFormatException);
    });
  });
}
