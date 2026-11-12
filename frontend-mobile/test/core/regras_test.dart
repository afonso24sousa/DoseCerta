import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/configuracao_api.dart';
import 'package:dosecerta/core/jwt.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/core/sessao_controller.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/validador_palavra_passe.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ValidadorPalavraPasse (RNF-003)', () {
    test('aceita 8 caracteres com uma letra e um algarismo', () {
      expect(ValidadorPalavraPasse.validar('Segura12'), isNull);
      expect(ValidadorPalavraPasse.validar('ação1234'), isNull);
    });

    test('recusa palavras-passe curtas, sem letra ou sem algarismo', () {
      expect(
        ValidadorPalavraPasse.validar('Abc1234'),
        ValidadorPalavraPasse.regra,
      );
      expect(
        ValidadorPalavraPasse.validar('12345678'),
        ValidadorPalavraPasse.regra,
      );
      expect(
        ValidadorPalavraPasse.validar('abcdefgh'),
        ValidadorPalavraPasse.regra,
      );
      expect(
        ValidadorPalavraPasse.validar(''),
        ValidadorPalavraPasse.regraObrigatoria,
      );
    });
  });

  group('ConfiguracaoApi (RNF-002)', () {
    test('aceita HTTPS e acrescenta /api/v1', () {
      final c = ConfiguracaoApi.validar(
        'https://api.exemplo.pt/',
        modoDebug: false,
      );
      expect(c.baseUrl, 'https://api.exemplo.pt/api/v1');
    });

    test('recusa HTTP noutros endereços, mesmo em debug', () {
      expect(
        () => ConfiguracaoApi.validar('http://api.exemplo.pt', modoDebug: true),
        throwsArgumentError,
      );
    });

    test('aceita http://10.0.2.2 só em debug', () {
      expect(
        ConfiguracaoApi.validar(
          'http://10.0.2.2:5000',
          modoDebug: true,
        ).baseUrl,
        'http://10.0.2.2:5000/api/v1',
      );
      expect(
        () => ConfiguracaoApi.validar('http://10.0.2.2:5000', modoDebug: false),
        throwsArgumentError,
      );
    });

    test('o endereço por omissão é o público, em HTTPS', () {
      expect(ConfiguracaoApi.urlPorOmissao, startsWith('https://'));
    });
  });

  group('ProblemaApi e MensagensErro (RNF-028)', () {
    test('lê o corpo de erro da API com os campos', () {
      final p = ProblemaApi.daResposta({
        'codigo': 'VALIDACAO_INVALIDA',
        'mensagem': 'Os dados submetidos são inválidos.',
        'campos': [
          {
            'campo': 'palavraPasse',
            'regra': 'Tem de ter pelo menos 8 caracteres.',
          },
        ],
      }, estadoHttp: 400);
      expect(p.codigo, 'VALIDACAO_INVALIDA');
      expect(p.estadoHttp, 400);
      expect(
        MensagensErro.mensagensDeCampo(p, {'palavraPasse': 'Palavra-passe'}),
        {'palavraPasse': 'Palavra-passe: tem de ter pelo menos 8 caracteres.'},
      );
    });

    test('corpo sem o formato da API dá ERRO_INTERNO', () {
      final p = ProblemaApi.daResposta('<html>', estadoHttp: 502);
      expect(p.codigo, ProblemaApi.codigoErroInterno);
      expect(MensagensErro.traduzir(p), MensagensErro.erroInesperado);
    });

    test('credenciais inválidas não indicam o dado errado (RF-012)', () {
      const p = ProblemaApi(codigo: 'CREDENCIAIS_INVALIDAS', mensagem: 'x');
      expect(MensagensErro.traduzir(p), 'E-mail ou palavra-passe incorretos.');
    });

    test('mensagem do ecrã substitui a do catálogo', () {
      const p = ProblemaApi(codigo: 'EMAIL_JA_REGISTADO', mensagem: 'x');
      expect(
        MensagensErro.traduzir(
          p,
          especificas: {'EMAIL_JA_REGISTADO': 'Outra.'},
        ),
        'Outra.',
      );
    });

    test('sem ligação mostra a mensagem do RNF-033', () {
      expect(
        MensagensErro.traduzir(const SemLigacaoErro()),
        SemLigacaoErro.mensagemPadrao,
      );
    });

    test('ErroCampo tolera valores em falta', () {
      expect(ErroCampo.fromJson(const {}).campo, '');
    });
  });

  test('lerSujeitoDoToken devolve o sub do JWT', () {
    expect(
      lerSujeitoDoToken('a.eyJzdWIiOiJ1dGlsaXphZG9yLTEifQ.c'),
      'utilizador-1',
    );
    expect(lerSujeitoDoToken('invalido'), isNull);
  });

  group('AppRouter.redirecionar (RF-017)', () {
    final comSessao = SessaoEstado.autenticada(
      SessaoGuardada(token: 't', expiraEm: DateTime.utc(2030)),
    );
    const semSessao = SessaoEstado.semSessao();

    test('sem sessão, só E01 e E02 abrem', () {
      expect(
        AppRouter.redirecionar(semSessao, AppRouter.conta),
        AppRouter.entrar,
      );
      expect(
        AppRouter.redirecionar(semSessao, AppRouter.hoje),
        AppRouter.entrar,
      );
      expect(AppRouter.redirecionar(semSessao, AppRouter.registo), isNull);
      expect(AppRouter.redirecionar(semSessao, AppRouter.entrar), isNull);
    });

    test('com sessão, E01 e E02 levam a E04', () {
      expect(
        AppRouter.redirecionar(comSessao, AppRouter.entrar),
        AppRouter.hoje,
      );
      expect(
        AppRouter.redirecionar(comSessao, AppRouter.registo),
        AppRouter.hoje,
      );
      expect(AppRouter.redirecionar(comSessao, AppRouter.conta), isNull);
    });
  });
}
