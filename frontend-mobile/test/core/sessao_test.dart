import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/auth_interceptor.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/core/sessao_controller.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/falsos.dart';

void main() {
  final agora = DateTime.utc(2026, 11, 9, 12);
  late RelogioFixo relogio;
  late SessaoStoreEmMemoria store;
  late AuthRepositorioFalso auth;

  ProviderContainer criar({SessaoGuardada? inicial}) {
    final container = ProviderContainer(
      overrides: [
        relogioProvider.overrideWithValue(relogio),
        sessaoStoreProvider.overrideWithValue(store),
        authRepositorioProvider.overrideWithValue(auth),
        sessaoInicialProvider.overrideWithValue(inicial),
      ],
    );
    addTearDown(container.dispose);
    return container;
  }

  setUp(() {
    relogio = RelogioFixo(agora);
    store = SessaoStoreEmMemoria();
    auth = AuthRepositorioFalso();
  });

  group('SessaoController', () {
    test('restaura a sessão válida sem pedidos de rede', () {
      final c = criar(
        inicial: SessaoGuardada(
          token: 't',
          expiraEm: agora.add(const Duration(hours: 1)),
        ),
      );
      expect(c.read(sessaoControllerProvider).autenticada, isTrue);
    });

    test(
      'sessão já expirada leva ao início de sessão e é apagada (RNF-009)',
      () async {
        final c = criar(
          inicial: SessaoGuardada(
            token: 't',
            expiraEm: agora.subtract(const Duration(seconds: 1)),
          ),
        );
        final estado = c.read(sessaoControllerProvider);
        expect(estado.autenticada, isFalse);
        expect(estado.motivoFim, MotivoFimSessao.expirada);
        await Future<void>.delayed(Duration.zero);
        expect(store.limpezas, 1);
      },
    );

    test('iniciar sessão guarda o token, a expiração e o sub', () async {
      final c = criar();
      await c
          .read(sessaoControllerProvider.notifier)
          .iniciarSessao('ana@exemplo.pt', 'x');
      final estado = c.read(sessaoControllerProvider);
      expect(estado.autenticada, isTrue);
      expect(estado.utilizadorId, 'utilizador-1');
      expect(store.guardada?.expiraEm, DateTime.parse(auth.sessao.expiraEm));
    });

    test('terminar sessão chama OP-03 e apaga a sessão (RF-016)', () async {
      final c = criar(
        inicial: SessaoGuardada(
          token: 't',
          expiraEm: agora.add(const Duration(hours: 1)),
        ),
      );
      await c.read(sessaoControllerProvider.notifier).terminarSessao();
      expect(auth.logouts, 1);
      expect(
        c.read(sessaoControllerProvider).motivoFim,
        MotivoFimSessao.terminada,
      );
      expect(store.limpezas, 1);
    });

    test(
      'sem ligação, terminar sessão falha e a sessão mantém-se (RNF-033)',
      () async {
        auth.erroLogout = const SemLigacaoErro();
        final c = criar(
          inicial: SessaoGuardada(
            token: 't',
            expiraEm: agora.add(const Duration(hours: 1)),
          ),
        );
        await expectLater(
          c.read(sessaoControllerProvider.notifier).terminarSessao(),
          throwsA(isA<SemLigacaoErro>()),
        );
        expect(c.read(sessaoControllerProvider).autenticada, isTrue);
      },
    );

    test('token já recusado (401) em OP-03 termina a sessão local', () async {
      auth.erroLogout = const ProblemaApi(
        codigo: 'NAO_AUTENTICADO',
        mensagem: '',
        estadoHttp: 401,
      );
      final c = criar(
        inicial: SessaoGuardada(
          token: 't',
          expiraEm: agora.add(const Duration(hours: 1)),
        ),
      );
      await c.read(sessaoControllerProvider.notifier).terminarSessao();
      expect(c.read(sessaoControllerProvider).autenticada, isFalse);
    });

    test('sessaoExpirada termina a sessão com o motivo expirada', () {
      final c = criar(
        inicial: SessaoGuardada(
          token: 't',
          expiraEm: agora.add(const Duration(hours: 1)),
        ),
      );
      c.read(sessaoControllerProvider.notifier).sessaoExpirada();
      expect(
        c.read(sessaoControllerProvider).motivoFim,
        MotivoFimSessao.expirada,
      );
    });
  });

  group('AuthInterceptor (RNF-054, RF-017)', () {
    late int recusas;
    late AuthInterceptor interceptor;

    setUp(() {
      recusas = 0;
      interceptor = AuthInterceptor(
        obterToken: () => 'tok',
        aoRecusarSessao: () => recusas++,
      );
    });

    RequestOptions pedido(String caminho) => RequestOptions(path: caminho);

    test('junta o Bearer aos pedidos autenticados e não aos públicos', () {
      final conta = pedido('/conta');
      interceptor.onRequest(conta, RequestInterceptorHandler());
      expect(conta.headers['Authorization'], 'Bearer tok');

      final login = pedido('/auth/login');
      interceptor.onRequest(login, RequestInterceptorHandler());
      expect(login.headers.containsKey('Authorization'), isFalse);
    });

    // Pedido completo pelo ApiClient, com um adaptador HTTP que responde sempre 401 no formato da API.
    Future<void> tratar(String caminho) async {
      final api = ApiClient(
        configuracao: ConfiguracaoApi.validar(
          'https://api.exemplo.pt',
          modoDebug: false,
        ),
        authInterceptor: interceptor,
        dio: Dio()..httpClientAdapter = _Adaptador401(),
      );
      await expectLater(
        api.post(caminho),
        throwsA(
          isA<ProblemaApi>()
              .having((p) => p.estadoHttp, 'estadoHttp', 401)
              .having((p) => p.codigo, 'codigo', 'NAO_AUTENTICADO'),
        ),
      );
    }

    test('401 num pedido autenticado termina a sessão', () async {
      await tratar('/conta');
      expect(recusas, 1);
    });

    test(
      '401 do início de sessão (credenciais inválidas) não termina a sessão',
      () async {
        await tratar('/auth/login');
        expect(recusas, 0);
      },
    );
  });
}

/// Adaptador HTTP que responde sempre `401` com o corpo de erro da API.
class _Adaptador401 implements HttpClientAdapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    return ResponseBody.fromString(
      '{"codigo":"NAO_AUTENTICADO","mensagem":"Sessão inválida."}',
      401,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
