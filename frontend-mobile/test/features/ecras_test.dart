import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/features/autenticacao/inicio_sessao_page.dart';
import 'package:dosecerta/features/autenticacao/registo_page.dart';
import 'package:dosecerta/features/conta/conta_page.dart';
import 'package:dosecerta/models/conta_criada_dto.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/falsos.dart';

void main() {
  late AuthRepositorioFalso auth;
  late ContaRepositorioFalso conta;

  setUp(() {
    auth = AuthRepositorioFalso();
    conta = ContaRepositorioFalso();
  });

  Widget app(Widget pagina, {double escalaLetra = 1}) {
    return ProviderScope(
      overrides: [
        authRepositorioProvider.overrideWithValue(auth),
        contaRepositorioProvider.overrideWithValue(conta),
        sessaoStoreProvider.overrideWithValue(SessaoStoreEmMemoria()),
        relogioProvider.overrideWithValue(
          RelogioFixo(DateTime.utc(2026, 11, 9)),
        ),
        sessaoInicialProvider.overrideWithValue(
          SessaoGuardada(token: 't', expiraEm: DateTime.utc(2026, 11, 10)),
        ),
      ],
      child: MaterialApp(
        theme: TemaDoseCerta.claro(),
        builder: (context, filho) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(escalaLetra)),
          child: filho!,
        ),
        home: pagina,
      ),
    );
  }

  Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

  group('E01 Registo', () {
    testWidgets(
      'validação prévia indica o campo e a regra e mantém os dados (RNF-003, RNF-028)',
      (tester) async {
        await tester.pumpWidget(app(const RegistoPage()));
        await tester.enterText(campo('Nome'), 'Ana Fictícia');
        await tester.enterText(campo('Palavra-passe'), 'abc');
        await tester.tap(find.text('Criar conta').last);
        await tester.pumpAndSettle();

        expect(find.text('E-mail: é obrigatório.'), findsOneWidget);
        expect(
          find.text(
            'Palavra-passe: tem de ter pelo menos 8 caracteres, com uma letra e um algarismo.',
          ),
          findsOneWidget,
        );
        expect(find.text('Ana Fictícia'), findsOneWidget);
        expect(auth.ultimoRegisto, isNull);
      },
    );

    testWidgets(
      'erro de campo devolvido pelo servidor aparece por baixo do campo',
      (tester) async {
        auth.erroRegisto = const ProblemaApi(
          codigo: 'EMAIL_JA_REGISTADO',
          mensagem: '',
          estadoHttp: 409,
        );
        await tester.pumpWidget(app(const RegistoPage()));
        await tester.enterText(campo('Nome'), 'Ana Fictícia');
        await tester.enterText(campo('E-mail'), 'ana@exemplo.pt');
        await tester.enterText(campo('Palavra-passe'), 'Segura123');
        await tester.tap(find.text('Criar conta').last);
        await tester.pumpAndSettle();

        expect(
          find.text('Já existe uma conta com este e-mail.'),
          findsOneWidget,
        );
        expect(find.text('ana@exemplo.pt'), findsOneWidget);
      },
    );
  });

  testWidgets('E02 mostra os convites pendentes depois do registo (RF-044)', (
    tester,
  ) async {
    const criada = ContaCriadaDto(
      id: 'i',
      nome: 'Ana',
      email: 'ana@exemplo.pt',
      convitesPendentes: 2,
    );
    await tester.pumpWidget(app(const InicioSessaoPage(contaCriada: criada)));
    expect(
      find.text(
        'Conta criada. Inicie sessão. Tem 2 convites à sua espera na área de convites.',
      ),
      findsOneWidget,
    );
  });

  testWidgets(
    'E03 mostra «Este e-mail pertence a outra conta.» e mantém os dados',
    (tester) async {
      conta.erroAtualizar = const ProblemaApi(
        codigo: 'EMAIL_JA_REGISTADO',
        mensagem: '',
        estadoHttp: 409,
      );
      await tester.pumpWidget(app(const ContaPage()));
      await tester.pumpAndSettle();
      expect(find.text('Ana Fictícia'), findsOneWidget);

      await tester.enterText(campo('E-mail'), 'outra@exemplo.pt');
      await tester.tap(find.text('Guardar dados'));
      await tester.pumpAndSettle();

      expect(find.text('Este e-mail pertence a outra conta.'), findsOneWidget);
      expect(find.text('outra@exemplo.pt'), findsOneWidget);
    },
  );

  group('Acessibilidade (RNF-025, RNF-026)', () {
    final ecras = <String, Widget>{
      'E01': const RegistoPage(),
      'E02': const InicioSessaoPage(),
      'E03': const ContaPage(),
    };

    for (final entrada in ecras.entries) {
      testWidgets('${entrada.key}: alvos de toque de pelo menos 48 x 48 dp', (
        tester,
      ) async {
        final semantica = tester.ensureSemantics();
        await tester.pumpWidget(app(entrada.value));
        await tester.pumpAndSettle();
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();
      });

      testWidgets(
        '${entrada.key}: letra a 150% sem texto cortado nem sobreposições',
        (tester) async {
          await tester.pumpWidget(app(entrada.value, escalaLetra: 1.5));
          await tester.pumpAndSettle();
          expect(tester.takeException(), isNull);
        },
      );
    }
  });
}
