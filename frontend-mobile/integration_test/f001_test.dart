// Testes de sistema da F001 na aplicação mobile (I063, QG-07), contra a API real indicada em
// `--dart-define=URL_API`. Especificação: m4-especificacao-testes-sistema-frontend-mobile-v01.md.
import 'dart:convert';
import 'dart:io';

import 'package:dosecerta/main.dart' as app;
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

const urlApi = String.fromEnvironment('URL_API');

/// Conta fictícia com e-mail único por execução.
class ContaFicticia {
  ContaFicticia(String prefixo)
    : email = '$prefixo.${DateTime.now().microsecondsSinceEpoch}@exemplo.pt';

  final String nome = 'Ana Fictícia';
  final String email;
  final String palavraPasse = 'Teste2026a1';
}

/// Cria a conta diretamente na API (precondição dos cenários).
Future<void> criarContaNaApi(ContaFicticia conta) async {
  final cliente = HttpClient();
  try {
    final pedido = await cliente.postUrl(
      Uri.parse('$urlApi/api/v1/auth/registo'),
    );
    pedido.headers.contentType = ContentType.json;
    pedido.write(
      jsonEncode({
        'nome': conta.nome,
        'email': conta.email,
        'palavraPasse': conta.palavraPasse,
      }),
    );
    final resposta = await pedido.close();
    expect(resposta.statusCode, 201);
  } finally {
    cliente.close();
  }
}

/// Espera, sem tempo fixo, até o elemento aparecer (máximo de 30 segundos).
Future<void> esperarPor(WidgetTester tester, Finder alvo) async {
  for (var i = 0; i < 150; i++) {
    await tester.pump(const Duration(milliseconds: 200));
    if (alvo.evaluate().isNotEmpty) {
      return;
    }
  }
  expect(alvo, findsWidgets);
}

/// Fecha o aviso temporário (SnackBar) que estiver aberto e espera que saia do ecrã, para não ficar por cima de
/// um botão no fundo do ecrã e receber o toque.
Future<void> fecharAviso(WidgetTester tester) async {
  final aviso = find.byType(SnackBar);
  if (aviso.evaluate().isEmpty) {
    return;
  }
  ScaffoldMessenger.of(tester.element(aviso.first)).removeCurrentSnackBar();
  for (var i = 0; i < 25 && aviso.evaluate().isNotEmpty; i++) {
    await tester.pump(const Duration(milliseconds: 200));
  }
  expect(aviso, findsNothing);
}

/// Arranca a aplicação sem sessão guardada, para cada cenário ser independente.
Future<void> arrancarSemSessao(WidgetTester tester) async {
  await const FlutterSecureStorage().deleteAll();
  await app.main();
  await esperarPor(tester, find.text('Início de sessão'));
}

Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

String textoDe(WidgetTester tester, String rotulo) =>
    tester.widget<TextField>(campo(rotulo)).controller!.text;

Future<void> tocar(WidgetTester tester, Finder alvo) async {
  await tester.ensureVisible(alvo);
  await tester.tap(alvo);
  await tester.pump();
}

Future<void> iniciarSessao(
  WidgetTester tester,
  String email,
  String palavraPasse,
) async {
  await tester.enterText(campo('E-mail'), email);
  await tester.enterText(campo('Palavra-passe'), palavraPasse);
  await tocar(tester, find.widgetWithText(FilledButton, 'Entrar'));
  await esperarPor(tester, find.byType(NavigationBar));
}

Future<void> abrirConta(WidgetTester tester) async {
  await tocar(tester, find.byTooltip('Conta'));
  await esperarPor(tester, find.widgetWithText(FilledButton, 'Guardar dados'));
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('TS-M-F001-01 cria a conta com dados válidos (RF-010)', (
    tester,
  ) async {
    final conta = ContaFicticia('registo');
    await arrancarSemSessao(tester);
    await tocar(tester, find.widgetWithText(TextButton, 'Criar conta'));
    await esperarPor(tester, find.text('Registo'));

    await tester.enterText(campo('Nome'), conta.nome);
    await tester.enterText(campo('E-mail'), conta.email);
    await tester.enterText(campo('Palavra-passe'), conta.palavraPasse);
    await tocar(tester, find.widgetWithText(FilledButton, 'Criar conta'));

    await esperarPor(tester, find.textContaining('Conta criada.'));
    expect(find.text('Início de sessão'), findsOneWidget);
  });

  testWidgets('TS-M-F001-02 recusa um e-mail já registado (RF-011)', (
    tester,
  ) async {
    final conta = ContaFicticia('repetido');
    await criarContaNaApi(conta);
    await arrancarSemSessao(tester);
    await tocar(tester, find.widgetWithText(TextButton, 'Criar conta'));
    await esperarPor(tester, find.text('Registo'));

    await tester.enterText(campo('Nome'), 'Bruno Fictício');
    await tester.enterText(campo('E-mail'), conta.email);
    await tester.enterText(campo('Palavra-passe'), conta.palavraPasse);
    await tocar(tester, find.widgetWithText(FilledButton, 'Criar conta'));

    await esperarPor(tester, find.text('Já existe uma conta com este e-mail.'));
    expect(find.text('Registo'), findsOneWidget);
  });

  testWidgets('TS-M-F001-03 inicia sessão com credenciais válidas (RF-012)', (
    tester,
  ) async {
    final conta = ContaFicticia('login');
    await criarContaNaApi(conta);
    await arrancarSemSessao(tester);

    await iniciarSessao(tester, conta.email, conta.palavraPasse);
    expect(find.text('Início de sessão'), findsNothing);
  });

  testWidgets('TS-M-F001-04 recusa credenciais inválidas (RF-012)', (
    tester,
  ) async {
    final conta = ContaFicticia('invalido');
    await criarContaNaApi(conta);
    await arrancarSemSessao(tester);

    await tester.enterText(campo('E-mail'), conta.email);
    await tester.enterText(campo('Palavra-passe'), 'Errada123');
    await tocar(tester, find.widgetWithText(FilledButton, 'Entrar'));

    await esperarPor(tester, find.text('E-mail ou palavra-passe incorretos.'));
    expect(find.byType(NavigationBar), findsNothing);
  });

  testWidgets(
    'TS-M-F001-05 consulta e atualiza o nome e o e-mail (RF-013, RF-014)',
    (tester) async {
      final conta = ContaFicticia('conta');
      final novoEmail = ContaFicticia('conta-nova').email;
      await criarContaNaApi(conta);
      await arrancarSemSessao(tester);
      await iniciarSessao(tester, conta.email, conta.palavraPasse);
      await abrirConta(tester);
      await esperarPor(tester, find.text(conta.email));
      expect(textoDe(tester, 'Nome'), conta.nome);

      await tester.enterText(campo('Nome'), 'Carla Fictícia');
      await tester.enterText(campo('E-mail'), novoEmail);
      await tocar(tester, find.widgetWithText(FilledButton, 'Guardar dados'));

      await esperarPor(tester, find.text('Dados guardados.'));
      expect(textoDe(tester, 'Nome'), 'Carla Fictícia');
      expect(textoDe(tester, 'E-mail'), novoEmail);
    },
  );

  testWidgets(
    'TS-M-F001-06 altera a palavra-passe e entra com a nova (RF-015)',
    (tester) async {
      final conta = ContaFicticia('senha');
      const nova = 'Nova2026b2';
      await criarContaNaApi(conta);
      await arrancarSemSessao(tester);
      await iniciarSessao(tester, conta.email, conta.palavraPasse);
      await abrirConta(tester);

      await tester.enterText(campo('Palavra-passe atual'), conta.palavraPasse);
      await tester.enterText(campo('Nova palavra-passe'), nova);
      await tocar(
        tester,
        find.widgetWithText(OutlinedButton, 'Alterar palavra-passe'),
      );
      await esperarPor(tester, find.text('Palavra-passe alterada.'));
      // O aviso «Palavra-passe alterada.» fica por cima de «Terminar sessão», no fundo do ecrã.
      await fecharAviso(tester);

      await tocar(
        tester,
        find.widgetWithText(OutlinedButton, 'Terminar sessão'),
      );
      await esperarPor(tester, find.text('Início de sessão'));
      await iniciarSessao(tester, conta.email, nova);
    },
  );

  testWidgets(
    'TS-M-F001-07 termina a sessão e exige sessão ao reabrir (RF-016, RF-017)',
    (tester) async {
      final conta = ContaFicticia('logout');
      await criarContaNaApi(conta);
      await arrancarSemSessao(tester);
      await iniciarSessao(tester, conta.email, conta.palavraPasse);
      await abrirConta(tester);

      await tocar(
        tester,
        find.widgetWithText(OutlinedButton, 'Terminar sessão'),
      );
      await esperarPor(tester, find.text('Início de sessão'));

      await app.main();
      await esperarPor(tester, find.text('Início de sessão'));
      expect(find.byType(NavigationBar), findsNothing);
    },
  );
}
