import 'package:dosecerta/avisos_locais/aviso_local.dart';
import 'package:dosecerta/avisos_locais/avisos_locais_controller.dart';
import 'package:dosecerta/avisos_locais/avisos_locais_service.dart';
import 'package:dosecerta/avisos_locais/sincronizador_calendario.dart';
import 'package:dosecerta/avisos_locais/tomas_sincronizadas_store.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../suporte/falsos.dart';
import '../suporte/falsos_tomas.dart';

void main() {
  // 09/11/2026 às 12:00 em Lisboa (inverno: UTC+0).
  final agora = DateTime.utc(2026, 11, 9, 12);

  group('AvisoLocal (RF-062)', () {
    test('mostra a ficha, o medicamento, a dose e a hora de Lisboa tal como vem da API', () {
      final aviso = AvisoLocal.daToma(
        tomaSincronizadaExemplo(previstaEm: '2026-07-01T20:00:00+01:00'),
      );

      expect(aviso.titulo, 'Toma de Maria Fictícia Santos');
      expect(aviso.corpo, 'Metformina 500 mg às 20:00');
      expect(aviso.previstaEm, DateTime.utc(2026, 7, 1, 19));
    });

    test('a mesma toma tem sempre o mesmo identificador de aviso', () {
      final id = AvisoLocal.idDaToma('3f2b6c1e-0000-4000-8000-000000000001');

      expect(AvisoLocal.idDaToma('3f2b6c1e-0000-4000-8000-000000000001'), id);
      expect(
        AvisoLocal.idDaToma('3f2b6c1e-0000-4000-8000-000000000002'),
        isNot(id),
      );
      expect(id, inInclusiveRange(0, 0x7FFFFFFF));
    });
  });

  group('AvisosLocaisService', () {
    late AgendadorAvisosFalso agendador;
    late AvisosLocaisService servico;

    setUp(() {
      agendador = AgendadorAvisosFalso();
      servico = AvisosLocaisService(agendador, RelogioFixo(agora));
    });

    test('cancela todos e agenda só as tomas ainda por vir (acordado com a I068 e a I070)', () async {
      await agendador.agendar(
        AvisoLocal.daToma(tomaSincronizadaExemplo(tomaId: 'antiga')),
      );

      final agendados = await servico.reconciliar([
        tomaSincronizadaExemplo(
          tomaId: 'passou',
          previstaEm: '2026-11-09T11:59:00+00:00',
        ),
        tomaSincronizadaExemplo(
          tomaId: 'hoje',
          previstaEm: '2026-11-09T20:00:00+00:00',
        ),
        tomaSincronizadaExemplo(
          tomaId: 'amanha',
          previstaEm: '2026-11-10T08:00:00+00:00',
        ),
      ]);

      expect(agendador.cancelamentosTotais, 1);
      expect(agendados.map((a) => a.tomaId), ['hoje', 'amanha']);
      expect(agendador.agendados.keys, {
        AvisoLocal.idDaToma('hoje'),
        AvisoLocal.idDaToma('amanha'),
      });
    });

    test('cancelarAviso cancela só o aviso da toma confirmada', () async {
      await servico.reconciliar([
        tomaSincronizadaExemplo(tomaId: 'a'),
        tomaSincronizadaExemplo(tomaId: 'b'),
      ]);

      await servico.cancelarAviso('a');

      expect(agendador.agendados.keys, {AvisoLocal.idDaToma('b')});
    });
  });

  group('SincronizadorCalendario (RF-061)', () {
    late CalendarioRepositorioFalso repositorio;
    late TomasSincronizadasStoreEmMemoria store;
    late AgendadorAvisosFalso agendador;
    late SincronizadorCalendario sincronizador;

    setUp(() {
      repositorio = CalendarioRepositorioFalso();
      store = TomasSincronizadasStoreEmMemoria();
      agendador = AgendadorAvisosFalso();
      sincronizador = SincronizadorCalendario(
        repositorio: repositorio,
        store: store,
        avisos: AvisosLocaisService(agendador, RelogioFixo(agora)),
        relogio: RelogioFixo(agora),
      );
    });

    test('com ligação, guarda as tomas e agenda os avisos', () async {
      repositorio.tomas = [tomaSincronizadaExemplo()];

      final resultado = await sincronizador.sincronizar('u-eu');

      expect(resultado, ResultadoSincronizacao.sucesso);
      expect(store.guardadas!.utilizadorId, 'u-eu');
      expect(store.guardadas!.tomas.single.tomaId, 't-1');
      expect(store.guardadas!.ultimaSincronizacao, agora);
      expect(agendador.agendados, hasLength(1));
    });

    test('uma ficha arquivada deixa de ter avisos na sincronização seguinte (RF-069)', () async {
      repositorio.tomas = [tomaSincronizadaExemplo()];
      await sincronizador.sincronizar('u-eu');

      // Depois de arquivar, a OP-34 já não devolve as tomas dessa ficha.
      repositorio.tomas = [];
      await sincronizador.sincronizar('u-eu');

      expect(agendador.agendados, isEmpty);
      expect(store.guardadas!.tomas, isEmpty);
    });

    test(
      'sem ligação, mantém as tomas e os avisos da última sincronização',
      () async {
        repositorio.tomas = [tomaSincronizadaExemplo()];
        await sincronizador.sincronizar('u-eu');
        repositorio.erro = const SemLigacaoErro();

        final resultado = await sincronizador.sincronizar('u-eu');

        expect(resultado, ResultadoSincronizacao.semLigacao);
        expect(agendador.cancelamentosTotais, 1);
        expect(agendador.agendados, hasLength(1));
        expect(store.guardadas!.tomas, hasLength(1));
      },
    );

    test('uma recusa do servidor também mantém os avisos', () async {
      repositorio.tomas = [tomaSincronizadaExemplo()];
      await sincronizador.sincronizar('u-eu');
      repositorio.erro = const ProblemaApi(
        codigo: 'NAO_AUTENTICADO',
        mensagem: '',
        estadoHttp: 401,
      );

      expect(
        await sincronizador.sincronizar('u-eu'),
        ResultadoSincronizacao.falhou,
      );
      expect(agendador.agendados, hasLength(1));
    });

    test('outro Utilizador no mesmo telemóvel: os avisos do anterior são cancelados, mesmo sem ligação', () async {
      repositorio.tomas = [tomaSincronizadaExemplo()];
      await sincronizador.sincronizar('u-eu');
      repositorio.erro = const SemLigacaoErro();

      await sincronizador.sincronizar('u-outro');

      expect(agendador.agendados, isEmpty);
      expect(store.guardadas, isNull);
    });
  });

  group('TomasSincronizadasStore (shared_preferences)', () {
    setUp(() => SharedPreferences.setMockInitialValues({}));

    test('guarda, lê e apaga as tomas sincronizadas', () async {
      const store = TomasSincronizadasStore();

      await store.guardar('u-eu', [tomaSincronizadaExemplo()], agora);
      final lidas = await store.ler();

      expect(lidas!.utilizadorId, 'u-eu');
      expect(lidas.tomas.single.toJson(), tomaSincronizadaExemplo().toJson());
      expect(lidas.ultimaSincronizacao, agora);

      await store.limpar();
      expect(await store.ler(), isNull);
    });

    test('um registo ilegível conta como não haver sincronização', () async {
      SharedPreferences.setMockInitialValues({
        TomasSincronizadasStore.chave: '{',
      });

      expect(await const TomasSincronizadasStore().ler(), isNull);
    });
  });

  group('AvisosLocaisController', () {
    late CalendarioRepositorioFalso repositorio;
    late TomasSincronizadasStoreEmMemoria store;
    late AgendadorAvisosFalso agendador;
    late PermissaoNotificacoesFalsa permissao;
    late AuthRepositorioFalso auth;

    ProviderContainer contentor({SessaoGuardada? sessao}) {
      final c = ProviderContainer(
        overrides: [
          relogioProvider.overrideWithValue(RelogioFixo(agora)),
          sessaoInicialProvider.overrideWithValue(sessao),
          sessaoStoreProvider.overrideWithValue(SessaoStoreEmMemoria()),
          authRepositorioProvider.overrideWithValue(auth),
          calendarioRepositorioProvider.overrideWithValue(repositorio),
          tomasSincronizadasStoreProvider.overrideWithValue(store),
          agendadorAvisosProvider.overrideWithValue(agendador),
          permissaoNotificacoesProvider.overrideWithValue(permissao),
        ],
      );
      addTearDown(c.dispose);
      return c;
    }

    final sessao = SessaoGuardada(
      token: 't',
      expiraEm: agora.add(const Duration(hours: 8)),
      utilizadorId: 'u-eu',
    );

    setUp(() {
      repositorio = CalendarioRepositorioFalso()
        ..tomas = [tomaSincronizadaExemplo()];
      store = TomasSincronizadasStoreEmMemoria();
      agendador = AgendadorAvisosFalso();
      permissao = PermissaoNotificacoesFalsa();
      auth = AuthRepositorioFalso();
    });

    test(
      'ao abrir a aplicação com sessão, lê as autorizações e sincroniza',
      () async {
        permissao.notificacoes = false;
        final c = contentor(sessao: sessao);

        c.read(avisosLocaisControllerProvider);
        await pumpEventQueue();

        final estado = c.read(avisosLocaisControllerProvider);
        expect(estado.notificacoesAutorizadas, isFalse);
        expect(estado.mostrarNotificacoesDesativadas, isTrue);
        expect(estado.ultimoResultado, ResultadoSincronizacao.sucesso);
        expect(agendador.agendados, hasLength(1));
        // Nada é pedido sem um toque do Utilizador (janela do sistema e QG-07).
        expect(permissao.pedidos, 0);
      },
    );

    test('sem sessão não sincroniza', () async {
      final c = contentor();

      c.read(avisosLocaisControllerProvider);
      await pumpEventQueue();

      expect(repositorio.pedidos, 0);
    });

    test('ao iniciar sessão, sincroniza', () async {
      final c = contentor();
      c.read(avisosLocaisControllerProvider);
      await pumpEventQueue();

      await c
          .read(sessaoControllerProvider.notifier)
          .iniciarSessao('ana@exemplo.pt', 'x');
      await pumpEventQueue();

      expect(repositorio.pedidos, 1);
      expect(store.guardadas!.utilizadorId, 'utilizador-1');
    });

    test(
      'terminar a sessão cancela todos os avisos e apaga as tomas',
      () async {
        final c = contentor(sessao: sessao);
        c.read(avisosLocaisControllerProvider);
        await pumpEventQueue();
        expect(agendador.agendados, hasLength(1));

        await c.read(sessaoControllerProvider.notifier).terminarSessao();
        await pumpEventQueue();

        expect(agendador.agendados, isEmpty);
        expect(store.guardadas, isNull);
      },
    );

    test(
      'a sessão expirada mantém os avisos já agendados (regra das 48 horas)',
      () async {
        final c = contentor(sessao: sessao);
        c.read(avisosLocaisControllerProvider);
        await pumpEventQueue();

        c.read(sessaoControllerProvider.notifier).sessaoExpirada();
        await pumpEventQueue();

        expect(agendador.agendados, hasLength(1));
      },
    );

    test('«Ativar avisos» pede as notificações e os alarmes exatos e volta a sincronizar', () async {
      permissao
        ..notificacoes = false
        ..alarmesExatos = false;
      final c = contentor(sessao: sessao);
      c.read(avisosLocaisControllerProvider);
      await pumpEventQueue();

      await c.read(avisosLocaisControllerProvider.notifier).pedirPermissao();

      final estado = c.read(avisosLocaisControllerProvider);
      expect(permissao.pedidos, 1);
      expect(permissao.pedidosAlarmes, 1);
      expect(estado.notificacoesAutorizadas, isTrue);
      expect(estado.alarmesExatosAutorizados, isTrue);
      expect(repositorio.pedidos, 2);
    });

    test('se o Utilizador recusar as notificações, o aviso continua e os alarmes não são pedidos', () async {
      permissao
        ..notificacoes = false
        ..respostaAoPedido = false;
      final c = contentor(sessao: sessao);
      c.read(avisosLocaisControllerProvider);
      await pumpEventQueue();

      await c.read(avisosLocaisControllerProvider.notifier).pedirPermissao();

      expect(
        c.read(avisosLocaisControllerProvider).mostrarNotificacoesDesativadas,
        isTrue,
      );
      expect(permissao.pedidosAlarmes, 0);
    });
  });
}
