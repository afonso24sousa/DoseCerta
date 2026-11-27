// Apoio aos testes de sistema de F002 em diante (I070): preparação dos dados pela API real indicada em
// `--dart-define=URL_API`, arranque da aplicação com a fonte de ficheiros falsa e ações comuns. Só dados fictícios.
import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:dosecerta/avisos_locais/agendador_avisos.dart';
import 'package:dosecerta/avisos_locais/aviso_local.dart';
import 'package:dosecerta/avisos_locais/permissao_notificacoes.dart';
import 'package:dosecerta/core/inicializador_app.dart';
import 'package:dosecerta/core/provedores.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/main.dart';
import 'package:dosecerta/shared/origem_ficheiro.dart';
import 'package:dosecerta/shared/selecionador_ficheiros.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';

/// Raiz da API (a mesma que a aplicação usa).
const urlApi = String.fromEnvironment('URL_API');

/// Palavra-passe fictícia das contas de teste (RNF-003), válida só na base de dados efémera da pipeline.
const palavraPasseFicticia = 'Teste2026a1';

/// Conta fictícia criada pela API, com o token de uma sessão aberta pela API.
class ContaApi {
  ContaApi(this.nome, this.email, this.token);

  final String nome;
  final String email;
  final String token;
}

/// Resposta de um pedido à API.
class RespostaApi {
  RespostaApi(this.estado, this.corpo);

  final int estado;
  final Object? corpo;

  Map<String, Object?> get objeto => corpo! as Map<String, Object?>;
  List<Object?> get lista => corpo! as List<Object?>;
}

/// Pedido JSON à API, com o token se for indicado. Cada tentativa tem um limite de tempo e são feitas até 3: no
/// arranque a frio do emulador, um pedido sem limite pode ficar pendurado e encravar o QG-07 inteiro.
Future<RespostaApi> pedido(
  String metodo,
  String caminho, {
  String? token,
  Object? corpo,
}) async {
  for (var tentativa = 1; ; tentativa++) {
    try {
      return await _pedidoUnico(
        metodo,
        caminho,
        token: token,
        corpo: corpo,
      ).timeout(const Duration(seconds: 30));
    } on Exception catch (erro) {
      if (tentativa >= 3) {
        throw StateError('$metodo $caminho falhou 3 vezes: $erro');
      }
      await Future<void>.delayed(const Duration(seconds: 2));
    }
  }
}

Future<RespostaApi> _pedidoUnico(
  String metodo,
  String caminho, {
  String? token,
  Object? corpo,
}) async {
  final cliente = HttpClient()..connectionTimeout = const Duration(seconds: 10);
  try {
    final req = await cliente.openUrl(
      metodo,
      Uri.parse('$urlApi/api/v1$caminho'),
    );
    if (token != null) {
      req.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token');
    }
    if (corpo != null) {
      req.headers.contentType = ContentType.json;
      req.write(jsonEncode(corpo));
    }
    final resposta = await req.close();
    final texto = await resposta.transform(utf8.decoder).join();
    return RespostaApi(
      resposta.statusCode,
      texto.isEmpty ? null : jsonDecode(texto),
    );
  } finally {
    cliente.close();
  }
}

/// Cria uma conta fictícia (OP-01) com e-mail único e abre uma sessão pela API (OP-02).
Future<ContaApi> contaNaApi(
  String prefixo, {
  String nome = 'Ana Fictícia',
}) async {
  final email = '$prefixo.${DateTime.now().microsecondsSinceEpoch}@exemplo.pt';
  final registo = await pedido(
    'POST',
    '/auth/registo',
    corpo: {'nome': nome, 'email': email, 'palavraPasse': palavraPasseFicticia},
  );
  expect(registo.estado, 201);
  final sessao = await pedido(
    'POST',
    '/auth/login',
    corpo: {'email': email, 'palavraPasse': palavraPasseFicticia},
  );
  return ContaApi(nome, email, sessao.objeto['token']! as String);
}

/// Cria uma ficha pela API (OP-09): de terceiro com o criador como principal, ou própria.
Future<String> fichaNaApi(
  ContaApi conta,
  String nome, {
  bool propria = false,
}) async {
  final resposta = await pedido(
    'POST',
    '/fichas',
    token: conta.token,
    corpo: {
      'nomeCompleto': nome,
      'dataNascimento': '1941-05-17',
      'contactoEmergencia': '+351 910 000 009',
      'grauAutonomiaId': null,
      'condicoesSaudeIds': <int>[],
      'alergiasIds': <int>[],
      'propria': propria,
      if (!propria) 'cuidadorPrincipal': {'eu': true},
    },
  );
  expect(resposta.estado, 201);
  return resposta.objeto['id']! as String;
}

/// Associa [convidado] à ficha com o [papel]: convite (OP-19) e aceitação (OP-21).
Future<void> associarNaApi(
  ContaApi cuidador,
  String fichaId,
  ContaApi convidado,
  String papel,
) async {
  final convite = await pedido(
    'POST',
    '/fichas/$fichaId/convites',
    token: cuidador.token,
    corpo: {'email': convidado.email, 'papel': papel, 'paraPrincipal': false},
  );
  expect(convite.estado, 201);
  final id = convite.objeto['id']! as String;
  expect(
    (await pedido(
      'POST',
      '/convites/$id/aceitacao',
      token: convidado.token,
    )).estado,
    200,
  );
}

/// Último domingo do [mes], à 01:00 UTC: início e fim da hora de verão na União Europeia.
DateTime _ultimoDomingo(int ano, int mes) {
  var dia = DateTime.utc(ano, mes + 1, 1).subtract(const Duration(days: 1));
  while (dia.weekday != DateTime.sunday) {
    dia = dia.subtract(const Duration(days: 1));
  }
  return DateTime.utc(ano, mes, dia.day, 1);
}

/// Instante atual na hora de Lisboa, que é a do servidor. O emulador e a pipeline podem estar noutro fuso (UTC).
DateTime agoraLisboa() {
  final utc = DateTime.now().toUtc();
  final verao =
      !utc.isBefore(_ultimoDomingo(utc.year, 3)) &&
      utc.isBefore(_ultimoDomingo(utc.year, 10));
  return utc.add(Duration(hours: verao ? 1 : 0));
}

/// Data `AAAA-MM-DD` de hoje mais [dias], no calendário de Lisboa.
String dataLisboa([int dias = 0]) {
  final hoje = agoraLisboa();
  final dia = DateTime.utc(hoje.year, hoje.month, hoje.day + dias);
  return '${dia.year}-${dia.month.toString().padLeft(2, '0')}-${dia.day.toString().padLeft(2, '0')}';
}

/// Regista um tratamento contínuo pela API (OP-27), para a ficha passar a ter histórico.
Future<void> tratamentoNaApi(ContaApi conta, String fichaId) async {
  final data = dataLisboa();
  final resposta = await pedido(
    'POST',
    '/fichas/$fichaId/tratamentos',
    token: conta.token,
    corpo: {
      'medicamento': 'Paracetamol',
      'dose': '1 g',
      'grauImportancia': 'Leve',
      'dataInicio': data,
      'dataFim': null,
      'horarios': ['09:00'],
    },
  );
  expect(resposta.estado, 201);
}

/// PDF fictício mínimo, que começa por `%PDF` como o servidor confirma pelo conteúdo.
Uint8List pdfFicticio() => Uint8List.fromList(
  utf8.encode(
    '%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n'
    '3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n',
  ),
);

/// Câmara, galeria e seletor de ficheiros falsos (o `integration_test` não opera os seletores nativos): devolvem o
/// próximo ficheiro de [ficheiros], por ordem, e registam as origens pedidas.
class FonteFicticia implements FonteFicheiros {
  final List<({Uint8List bytes, String nome})> ficheiros = [];
  final List<OrigemFicheiro> pedidas = [];

  @override
  Future<FicheiroDaOrigem?> escolher(OrigemFicheiro origem) async {
    pedidas.add(origem);
    if (ficheiros.isEmpty) {
      return null;
    }
    final proximo = ficheiros.removeAt(0);
    return (
      ficheiro: XFile.fromData(proximo.bytes, name: proximo.nome),
      nome: proximo.nome,
    );
  }
}

/// Armazenamento da sessão em memória, no lugar do `FlutterSecureStorage` do Android. No emulador da pipeline, as
/// chamadas ao armazenamento seguro (Keystore) bloqueiam de vez em quando no arranque de um cenário e encravam o QG-07
/// (execução 23 do PR #112 e PR #121). A aplicação usa-o pelo `SessaoStore`, que recebe o armazenamento no
/// `InicializadorApp`: o código de produção (`lib/`) não muda. Os valores mantêm-se entre arranques do mesmo cenário,
/// para a reabertura da aplicação ler o que ficou guardado (TS-M-F001-07).
class ArmazenamentoMemoria implements FlutterSecureStorage {
  /// Valores guardados, partilhados por todos os arranques.
  static final Map<String, String> valores = {};

  @override
  dynamic noSuchMethod(Invocation invocacao) {
    final chave = invocacao.namedArguments[#key] as String?;
    switch (invocacao.memberName) {
      case #read:
        return Future<String?>.value(valores[chave]);
      case #write:
        final valor = invocacao.namedArguments[#value] as String?;
        if (valor == null) {
          valores.remove(chave);
        } else {
          valores[chave!] = valor;
        }
        return Future<void>.value();
      case #delete:
        valores.remove(chave);
        return Future<void>.value();
      case #deleteAll:
        valores.clear();
        return Future<void>.value();
      case #containsKey:
        return Future<bool>.value(valores.containsKey(chave));
      case #readAll:
        return Future<Map<String, String>>.value(Map.of(valores));
    }
    return super.noSuchMethod(invocacao);
  }
}

/// Autorizações de notificações e de alarmes exatos sempre concedidas, sem janelas do sistema.
class PermissaoNotificacoesAutorizada implements PermissaoNotificacoes {
  /// Cria as autorizações concedidas.
  const PermissaoNotificacoesAutorizada();

  @override
  Future<bool?> estado() async => true;

  @override
  Future<bool?> pedir() async => true;

  @override
  Future<bool?> estadoAlarmesExatos() async => true;

  @override
  Future<bool?> pedirAlarmesExatos() async => true;
}

/// Agendador que não agenda nada no sistema operativo.
class AgendadorSemAvisos implements AgendadorAvisos {
  /// Cria o agendador.
  const AgendadorSemAvisos();

  @override
  Future<void> agendar(AvisoLocal aviso) async {}

  @override
  Future<void> cancelar(int idAviso) async {}

  @override
  Future<void> cancelarTodos() async {}
}

/// Abre a aplicação com a sessão que estiver no [ArmazenamentoMemoria] e, se for indicada, a [fonte] de ficheiros
/// falsa no lugar dos seletores nativos. Cada abertura cria um `ProviderScope` novo, como um arranque a frio.
///
/// As [outras] substituições (por exemplo um repositório com respostas fixas, nos cenários «[duplo]» da F011) são
/// aplicadas no fim.
Future<void> abrirAplicacao(
  WidgetTester tester, {
  FonteFicticia? fonte,
  List<Override> outras = const [],
}) async {
  final substituicoes = await InicializadorApp(
    armazenamento: SessaoStore(ArmazenamentoMemoria()),
  ).iniciar();
  await tester.pumpWidget(
    ProviderScope(
      key: UniqueKey(),
      overrides: [
        ...substituicoes,
        // No emulador da pipeline as notificações não estão autorizadas: sem isto, o aviso das autorizações da I069
        // ocupa o topo de todos os ecrãs e os botões do fundo das listas deixam de ser construídos. Os avisos locais
        // são verificados nos testes da I069 (DD06 e especificação mobile v02, secção 6.3).
        permissaoNotificacoesProvider.overrideWithValue(
          const PermissaoNotificacoesAutorizada(),
        ),
        agendadorAvisosProvider.overrideWithValue(const AgendadorSemAvisos()),
        if (fonte != null)
          selecionadorFicheirosProvider.overrideWithValue(
            SelecionadorFicheiros(fonte),
          ),
        ...outras,
      ],
      child: const DoseCertaApp(),
    ),
  );
}

/// Arranca a aplicação sem sessão guardada, com a [fonte] de ficheiros falsa no lugar dos seletores nativos e as
/// [outras] substituições.
Future<void> arrancar(
  WidgetTester tester,
  FonteFicticia fonte, {
  List<Override> outras = const [],
}) async {
  ArmazenamentoMemoria.valores.clear();
  await abrirAplicacao(tester, fonte: fonte, outras: outras);
  await esperarPor(tester, find.text('Início de sessão'));
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

/// Campo de texto pelo rótulo.
Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

/// Toca no alvo depois de o mostrar. Fecha antes a `SnackBar` que estiver visível, que pode tapar o alvo no fundo do
/// ecrã (por exemplo «Ficha arquivada.» sobre «Reativar ou eliminar»); as mensagens já foram verificadas antes do toque.
Future<void> tocar(WidgetTester tester, Finder alvo) async {
  final campoComFoco = tester
      .widgetList<EditableText>(find.byType(EditableText))
      .any((campo) => campo.focusNode.hasFocus);
  FocusManager.instance.primaryFocus?.unfocus();
  if (campoComFoco) {
    await _esperarTecladoFechado(tester);
  }
  final mensageiro = find.byType(ScaffoldMessenger);
  if (mensageiro.evaluate().isNotEmpty) {
    tester
        .state<ScaffoldMessengerState>(mensageiro.first)
        .removeCurrentSnackBar();
  }
  await tester.pump();
  await tester.ensureVisible(alvo.first);
  await tester.pump();
  await tester.tap(alvo.first);
  await tester.pump();
}

/// Espera que o teclado fique fechado em 5 verificações seguidas (máximo de 5 segundos). Num emulador lento, o
/// teclado pedido pelo último `enterText` pode só abrir depois de o foco sair do campo: esperar apenas enquanto está
/// aberto deixava o toque partir antes de ele abrir, e o ecrã encolhia por baixo do alvo (QG-07 do PR #138).
Future<void> _esperarTecladoFechado(WidgetTester tester) async {
  var fechadoSeguido = 0;
  for (var i = 0; i < 50 && fechadoSeguido < 5; i++) {
    await tester.pump(const Duration(milliseconds: 100));
    fechadoSeguido = tester.view.viewInsets.bottom > 0 ? 0 : fechadoSeguido + 1;
  }
}

/// Inicia sessão em E02 e espera pela barra inferior.
Future<void> iniciarSessao(WidgetTester tester, ContaApi conta) async {
  await tester.enterText(campo('E-mail'), conta.email);
  await tester.enterText(campo('Palavra-passe'), palavraPasseFicticia);
  await tocar(tester, find.widgetWithText(FilledButton, 'Entrar'));
  await esperarPor(tester, find.byType(NavigationBar));
}

/// Abre a ficha [nome] a partir de E05 (separador Fichas da barra inferior).
Future<void> abrirFicha(WidgetTester tester, String nome) async {
  await tocar(tester, find.byTooltip('Fichas'));
  await esperarPor(tester, find.text(nome));
  await tocar(tester, find.text(nome));
  await esperarPor(tester, find.text('Ficha'));
}

// ---------------------------------------------------------------------------------------------------------------
// F008 a F011 (I079)
// ---------------------------------------------------------------------------------------------------------------

String _dois(int n) => n.toString().padLeft(2, '0');

/// Data `AAAA-MM-DD` e hora `hh:mm` de Lisboa daqui a [minutos] (negativo: há [minutos]).
({String data, String hora}) momentoLisboa(int minutos) {
  final t = agoraLisboa().add(Duration(minutes: minutos));
  return (
    data: '${t.year}-${_dois(t.month)}-${_dois(t.day)}',
    hora: '${_dois(t.hour)}:${_dois(t.minute)}',
  );
}

/// Instante ISO 8601 do dia [data] (`AAAA-MM-DD`) à [hora] (`hh:mm`) de Lisboa, com o desvio de Lisboa desse dia
/// (`+00:00` no inverno, `+01:00` no verão), como a aplicação o envia à API.
String instanteLisboa(String data, String hora) {
  final d = DateTime.parse(data);
  final h = int.parse(hora.substring(0, 2));
  final m = int.parse(hora.substring(3, 5));
  // Na hora de verão, o instante UTC é uma hora antes da hora de Lisboa.
  final utcSeVerao = DateTime.utc(d.year, d.month, d.day, h - 1, m);
  final verao =
      !utcSeVerao.isBefore(_ultimoDomingo(d.year, 3)) &&
      utcSeVerao.isBefore(_ultimoDomingo(d.year, 10));
  return '${data}T$hora:00+0${verao ? 1 : 0}:00';
}

/// Data `AAAA-MM-DD` apresentada como no ecrã, `dd/mm/aaaa` (RNF-029).
String dataNoEcra(String data) =>
    '${data.substring(8, 10)}/${data.substring(5, 7)}/${data.substring(0, 4)}';

/// Regista pela API (OP-27) um tratamento com tomas só no dia [data] (`AAAA-MM-DD`), à [hora]. Devolve o id.
Future<String> tratamentoDoDiaNaApi(
  ContaApi conta,
  String fichaId, {
  required String medicamento,
  required String data,
  required String hora,
  String grau = 'Leve',
}) async {
  final resposta = await pedido(
    'POST',
    '/fichas/$fichaId/tratamentos',
    token: conta.token,
    corpo: {
      'medicamento': medicamento,
      'dose': '500 mg',
      'grauImportancia': grau,
      'dataInicio': data,
      'dataFim': data,
      'horarios': [hora],
    },
  );
  expect(resposta.estado, 201);
  return resposta.objeto['id']! as String;
}

/// Regista um compromisso pela API (OP-41) e devolve-o.
Future<Map<String, Object?>> compromissoNaApi(
  ContaApi conta,
  String fichaId, {
  required String dataHora,
  required String motivo,
  String local = 'Centro de Saúde Fictício',
  int antecedenciaAvisoMinutos = 1440,
}) async {
  final resposta = await pedido(
    'POST',
    '/fichas/$fichaId/compromissos',
    token: conta.token,
    corpo: {
      'dataHora': dataHora,
      'local': local,
      'motivo': motivo,
      'antecedenciaAvisoMinutos': antecedenciaAvisoMinutos,
    },
  );
  expect(resposta.estado, 201);
  return resposta.objeto;
}

/// Compromissos da ficha (OP-42).
Future<List<Map<String, Object?>>> compromissosNaApi(
  ContaApi conta,
  String fichaId,
) async {
  final resposta = await pedido(
    'GET',
    '/fichas/$fichaId/compromissos',
    token: conta.token,
  );
  expect(resposta.estado, 200);
  return resposta.lista.cast<Map<String, Object?>>();
}

/// Regista uma ocorrência pela API (OP-46), que notifica os outros Cuidadores e os Familiares (RF-115).
Future<void> ocorrenciaNaApi(
  ContaApi conta,
  String fichaId,
  String descricao,
  String ocorridaEm,
) async {
  final resposta = await pedido(
    'POST',
    '/fichas/$fichaId/ocorrencias',
    token: conta.token,
    corpo: {'descricao': descricao, 'ocorridaEm': ocorridaEm},
  );
  expect(resposta.estado, 201);
}

/// Regista uma nota para os Cuidadores pela API.
Future<void> notaNaApi(ContaApi conta, String fichaId, String descricao) async {
  final resposta = await pedido(
    'POST',
    '/fichas/$fichaId/notas',
    token: conta.token,
    corpo: {'descricao': descricao},
  );
  expect(resposta.estado, 201);
}

/// Convida o [email] para a ficha com o [papel] (OP-19), sem aceitar.
Future<void> convidarNaApi(
  ContaApi cuidador,
  String fichaId,
  String email,
  String papel,
) async {
  final convite = await pedido(
    'POST',
    '/fichas/$fichaId/convites',
    token: cuidador.token,
    corpo: {'email': email, 'papel': papel, 'paraPrincipal': false},
  );
  expect(convite.estado, 201);
}

/// Confirma pela API (OP-33) a toma da ficha do dia [data] do [medicamento] (OP-32).
Future<void> confirmarTomaNaApi(
  ContaApi conta,
  String fichaId,
  String data,
  String medicamento,
) async {
  final tomas = await pedido(
    'GET',
    '/fichas/$fichaId/tomas?de=$data&ate=$data',
    token: conta.token,
  );
  final toma = tomas.lista.cast<Map<String, Object?>>().singleWhere(
    (t) => t['medicamento'] == medicamento,
  );
  final resposta = await pedido(
    'POST',
    '/tomas/${toma['id']}/confirmacao',
    token: conta.token,
  );
  expect(resposta.estado, 200);
}

/// Espera, sem tempo fixo, que a [conta] tenha pelo menos [n] notificações (OP-51). O aviso de um compromisso é
/// criado pelo processo periódico do servidor, até 60 segundos depois (RNF-040): até 30 consultas, de 3 em 3 s.
Future<void> esperarNotificacoes(ContaApi conta, int n) async {
  var recebidas = 0;
  for (var tentativa = 0; tentativa < 30; tentativa++) {
    final resposta = await pedido('GET', '/notificacoes', token: conta.token);
    recebidas = resposta.lista.length;
    if (recebidas >= n) {
      return;
    }
    await Future<void>.delayed(const Duration(seconds: 3));
  }
  expect(recebidas, greaterThanOrEqualTo(n), reason: 'notificações recebidas');
}

/// Puxa a lista para baixo, para a atualizar (`RefreshIndicator`), e espera que a atualização termine.
Future<void> puxarParaAtualizar(WidgetTester tester) async {
  await tester.fling(find.byType(ListView).first, const Offset(0, 400), 1000);
  for (var i = 0; i < 25; i++) {
    await tester.pump(const Duration(milliseconds: 200));
    if (find.byType(RefreshProgressIndicator).evaluate().isEmpty) {
      return;
    }
  }
}
