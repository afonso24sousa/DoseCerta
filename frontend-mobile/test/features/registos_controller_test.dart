import 'dart:async';

import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/features/registos/registos_controller.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/tipo_registo.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/falsos_fichas.dart';
import '../suporte/falsos_registos.dart';

void main() {
  late FichasRepositorioFalso fichas;
  late RegistosRepositorioFalso registos;
  late ProviderContainer contentor;

  setUp(() {
    fichas = FichasRepositorioFalso();
    // Familiar: cada consulta pede só as ocorrências, o que torna a ordem das esperas previsível.
    fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
    registos = RegistosRepositorioFalso()..ocorrencias.add(registoExemplo());
    contentor = ProviderContainer(
      overrides: [
        fichasRepositorioProvider.overrideWithValue(fichas),
        registosRepositorioProvider.overrideWithValue(registos),
      ],
    );
    contentor.listen(registosControllerProvider('f-1'), (_, _) {});
  });

  tearDown(() => contentor.dispose());

  RegistosController controlador() =>
      contentor.read(registosControllerProvider('f-1').notifier);

  RegistosEstado estado() => contentor.read(registosControllerProvider('f-1'));

  /// Deixa correr os pedidos pendentes até à próxima espera.
  Future<void> avancar() async {
    for (var i = 0; i < 5; i++) {
      await Future<void>.delayed(Duration.zero);
    }
  }

  test('mudar de separador durante a consulta mantém o «a carregar»', () async {
    final espera = Completer<void>();
    registos.esperas.add(espera);
    final consulta = controlador().carregar();
    await avancar();

    controlador().mudarSeparador(TipoRegisto.ocorrencia);
    expect(estado().aCarregar, isTrue);

    espera.complete();
    await consulta;
    expect(estado().aCarregar, isFalse);
  });

  test('com consultas sobrepostas, a resposta antiga não substitui a mais recente (RF-093)', () async {
    final espera = Completer<void>();
    registos.esperas.add(espera);
    final antiga = controlador().carregar();
    await avancar();

    registos.ocorrencias.insert(
      0,
      registoExemplo(id: 'r-novo', descricao: 'Recusou o jantar.'),
    );
    await controlador().carregar();
    expect(estado().ocorrencias, hasLength(2));

    espera.complete();
    await antiga;
    expect(estado().ocorrencias.map((r) => r.id), ['r-novo', 'r-1']);
    expect(estado().aCarregar, isFalse);
  });
}
