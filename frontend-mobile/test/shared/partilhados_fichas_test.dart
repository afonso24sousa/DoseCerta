import 'dart:io';

import 'package:dosecerta/core/formatador_data_hora.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/shared/campo_data.dart';
import 'package:dosecerta/shared/dialogo_confirmacao.dart';
import 'package:dosecerta/shared/ficheiro_recusado_erro.dart';
import 'package:dosecerta/shared/folha_origem_ficheiro.dart';
import 'package:dosecerta/shared/origem_ficheiro.dart';
import 'package:dosecerta/shared/por_papel.dart';
import 'package:dosecerta/shared/selecionador_ficheiros.dart';
import 'package:dosecerta/shared/tema_dosecerta.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';

const List<int> pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
const List<int> jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
const List<int> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

/// Fonte de ficheiros falsa: devolve o ficheiro ou lança o erro indicado pelo teste.
class FonteFalsa implements FonteFicheiros {
  XFile? ficheiro;
  Object? erro;
  OrigemFicheiro? pedida;

  @override
  Future<FicheiroDaOrigem?> escolher(OrigemFicheiro origem) async {
    pedida = origem;
    if (erro != null) {
      throw erro!;
    }
    final escolhido = ficheiro;
    return escolhido == null
        ? null
        : (ficheiro: escolhido, nome: escolhido.name);
  }
}

Widget app(Widget filho) {
  return MaterialApp(
    theme: TemaDoseCerta.claro(),
    locale: const Locale('pt', 'PT'),
    supportedLocales: const [Locale('pt', 'PT')],
    localizationsDelegates: GlobalMaterialLocalizations.delegates,
    home: Scaffold(body: filho),
  );
}

void main() {
  group('FormatadorDataHora (RNF-029)', () {
    test('apresenta AAAA-MM-DD e instantes como dd/mm/aaaa', () {
      expect(FormatadorDataHora.apresentarData('1941-05-17'), '17/05/1941');
      expect(
        FormatadorDataHora.apresentarData('2026-10-07T09:10:00+01:00'),
        '07/10/2026',
      );
      expect(FormatadorDataHora.deData(DateTime(2026, 1, 5)), '05/01/2026');
    });

    test('converte dd/mm/aaaa para AAAA-MM-DD e recusa datas inválidas', () {
      expect(FormatadorDataHora.paraIsoData('17/05/1941'), '1941-05-17');
      expect(FormatadorDataHora.paraIsoData(' 1/2/2020 '), '2020-02-01');
      expect(FormatadorDataHora.paraIsoData('31/02/2020'), isNull);
      expect(FormatadorDataHora.paraIsoData('1941-05-17'), isNull);
      expect(FormatadorDataHora.paraIsoData(''), isNull);
    });
  });

  group('SelecionadorFicheiros (RNF-008)', () {
    late Directory temporaria;

    setUp(() async {
      temporaria = await Directory.systemTemp.createTemp('i060');
    });

    tearDown(() => temporaria.delete(recursive: true));

    XFile criar(String nome, List<int> conteudo) {
      final ficheiro = File('${temporaria.path}/$nome')
        ..writeAsBytesSync(conteudo);
      return XFile(ficheiro.path, name: nome);
    }

    test(
      'um ficheiro com menos de 8 bytes é lido sem erro e recusado',
      () async {
        await expectLater(
          SelecionadorFicheiros.validar(
            XFile.fromData(Uint8List.fromList([0x25, 0x50])),
          ),
          throwsA(isA<FicheiroRecusadoErro>()),
        );
      },
    );

    test('reconhece PDF, JPG e PNG pelo conteúdo', () {
      expect(SelecionadorFicheiros.tipoPeloConteudo(pdf), 'application/pdf');
      expect(SelecionadorFicheiros.tipoPeloConteudo(jpg), 'image/jpeg');
      expect(SelecionadorFicheiros.tipoPeloConteudo(png), 'image/png');
      expect(SelecionadorFicheiros.tipoPeloConteudo([0x50, 0x4B]), isNull);
      expect(SelecionadorFicheiros.tipoPeloConteudo(const []), isNull);
    });

    test(
      'aceita um PDF válido e guarda o caminho, o nome e o tamanho',
      () async {
        final ficheiro = await SelecionadorFicheiros.validar(
          criar('receita.pdf', [...pdf, 1, 2, 3]),
        );
        expect(ficheiro.tipoConteudo, 'application/pdf');
        expect(ficheiro.nome, 'receita.pdf');
        expect(ficheiro.tamanhoBytes, 11);
        expect(ficheiro.caminho, isNotNull);
        expect(ficheiro.bytes, isNull);
      },
    );

    test('recusa um ficheiro com extensão .pdf que não é PDF', () async {
      await expectLater(
        SelecionadorFicheiros.validar(
          criar('falso.pdf', 'isto nao e um pdf'.codeUnits),
        ),
        throwsA(
          isA<FicheiroRecusadoErro>().having(
            (e) => e.mensagem,
            'mensagem',
            FicheiroRecusadoErro.formatoInvalido,
          ),
        ),
      );
    });

    test('aceita 10 MB e recusa 10 MB mais 1 byte', () async {
      final limite = SelecionadorFicheiros.tamanhoMaximoBytes;
      final dezMb = Uint8List(limite)..setAll(0, png);
      final aceite = await SelecionadorFicheiros.validar(
        criar('dez.png', dezMb),
      );
      expect(aceite.tamanhoBytes, limite);

      final maior = Uint8List(limite + 1)..setAll(0, png);
      await expectLater(
        SelecionadorFicheiros.validar(criar('grande.png', maior)),
        throwsA(
          isA<FicheiroRecusadoErro>().having(
            (e) => e.mensagem,
            'mensagem',
            FicheiroRecusadoErro.demasiadoGrande,
          ),
        ),
      );
    });

    test('ficheiro sem caminho (content://) segue em bytes', () async {
      final ficheiro = await SelecionadorFicheiros.validar(
        XFile.fromData(Uint8List.fromList([...jpg, 9])),
        nome: 'foto.jpg',
      );
      expect(ficheiro.nome, 'foto.jpg');
      expect(ficheiro.caminho, isNull);
      expect(ficheiro.bytes, hasLength(9));
      expect(ficheiro.tipoConteudo, 'image/jpeg');
    });

    test('cancelar a escolha devolve null; falha da origem dá erro', () async {
      final fonte = FonteFalsa();
      final selecionador = SelecionadorFicheiros(fonte);

      expect(await selecionador.daCamara(), isNull);
      expect(fonte.pedida, OrigemFicheiro.camera);

      fonte.erro = PlatformException(code: 'no_available_camera');
      await expectLater(
        selecionador.daGaleria(),
        throwsA(isA<FicheiroRecusadoErro>()),
      );

      fonte
        ..erro = null
        ..ficheiro = criar('id.png', png);
      final escolhido = await selecionador.doFicheiro();
      expect(fonte.pedida, OrigemFicheiro.ficheiro);
      expect(escolhido?.tipoConteudo, 'image/png');
    });
  });

  group('PorPapel (matriz de permissões)', () {
    test('mostra ao Cuidador e esconde do Familiar e da ficha arquivada', () {
      expect(
        const PorPapel(papel: Papel.cuidador, filho: SizedBox()).visivel,
        isTrue,
      );
      expect(
        const PorPapel(papel: Papel.familiar, filho: SizedBox()).visivel,
        isFalse,
      );
      expect(
        const PorPapel(
          papel: Papel.cuidador,
          soFichaAtiva: true,
          fichaAtiva: false,
          filho: SizedBox(),
        ).visivel,
        isFalse,
      );
    });
  });

  group('Widgets partilhados', () {
    testWidgets('a folha de origem mostra Câmara, Galeria e Ficheiro (D2)', (
      tester,
    ) async {
      final handle = tester.ensureSemantics();
      OrigemFicheiro? escolhida;
      await tester.pumpWidget(
        app(
          Builder(
            builder: (context) => TextButton(
              onPressed: () async =>
                  escolhida = await FolhaOrigemFicheiro.mostrar(
                    context,
                    titulo: 'Carregar: Receita médica',
                  ),
              child: const Text('Abrir'),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();

      expect(find.text('Carregar: Receita médica'), findsOneWidget);
      expect(find.text(SelecionadorFicheiros.ficheirosAceites), findsOneWidget);
      expect(find.text('Câmara'), findsOneWidget);
      expect(find.text('Ficheiro'), findsOneWidget);
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));

      await tester.tap(find.text('Galeria'));
      await tester.pumpAndSettle();
      expect(escolhida, OrigemFicheiro.galeria);
      handle.dispose();
    });

    testWidgets('a confirmação devolve verdadeiro só ao confirmar', (
      tester,
    ) async {
      bool? resposta;
      await tester.pumpWidget(
        app(
          Builder(
            builder: (context) => TextButton(
              onPressed: () async =>
                  resposta = await DialogoConfirmacao.confirmar(
                    context,
                    titulo: 'Arquivar ficha',
                    mensagem: 'O histórico é preservado.',
                    acao: 'Arquivar',
                  ),
              child: const Text('Abrir'),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      expect(resposta, isFalse);

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Arquivar'));
      await tester.pumpAndSettle();
      expect(resposta, isTrue);
    });

    testWidgets(
      'o campo de data mostra o erro por baixo e o calendário não aceita datas futuras',
      (tester) async {
        final controlador = TextEditingController(text: '01/01/2099');
        addTearDown(controlador.dispose);
        await tester.pumpWidget(
          app(
            CampoData(
              rotulo: 'Data de nascimento',
              controlador: controlador,
              ultimaData: DateTime(2026, 11, 9),
              erro: 'Data de nascimento: não pode ser uma data futura.',
            ),
          ),
        );
        expect(
          find.text('Data de nascimento: não pode ser uma data futura.'),
          findsOneWidget,
        );

        await tester.tap(find.byTooltip('Escolher no calendário'));
        await tester.pumpAndSettle();
        // O dia 15 é depois da última data permitida: tocar não o escolhe.
        await tester.tap(find.text('15'));
        await tester.tap(find.text('5'));
        await tester.tap(find.text('OK'));
        await tester.pumpAndSettle();
        expect(controlador.text, '05/11/2026');
      },
    );
  });
}
