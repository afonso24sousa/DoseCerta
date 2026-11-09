import 'dart:typed_data';

import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/sem_ligacao_erro.dart';
import 'package:dosecerta/features/fichas/nova_ficha_documentos_controller.dart';
import 'package:dosecerta/models/categoria_documento.dart';
import 'package:dosecerta/models/erro_campo.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/ficheiro_recusado_erro.dart';
import 'package:dosecerta/shared/validador_ficha.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

void main() {
  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.novaFicha);
  });

  Finder campo(String rotulo) => find.widgetWithText(TextField, rotulo);

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  Future<void> preencherDados(WidgetTester tester) async {
    await tester.enterText(campo('Nome completo'), 'Maria Fictícia Santos');
    await tester.enterText(
      campo('Data de nascimento (dd/mm/aaaa)'),
      '17/05/1941',
    );
    await tester.enterText(campo('Contacto de emergência'), '+351 910 000 001');
    await tocar(tester, find.text('Diabetes'));
  }

  Future<void> irParaDocumentos(WidgetTester tester) async {
    await abrir(tester);
    await preencherDados(tester);
    await tocar(tester, find.text('Seguinte'));
    expect(app.local, AppRouter.novaFichaDocumentos);
  }

  Future<void> carregar(
    WidgetTester tester,
    CategoriaDocumento categoria,
    String origem,
  ) async {
    await tocar(tester, find.text('Carregar: ${categoria.rotulo}'));
    await tester.tap(find.text(origem));
    await tester.pumpAndSettle();
  }

  group('E06 Nova ficha: dados e necessidades', () {
    testWidgets(
      'validação prévia indica o campo e a regra e mantém os dados (RF-020, RNF-028)',
      (tester) async {
        await abrir(tester);
        await tester.enterText(campo('Nome completo'), 'Maria');
        await tester.enterText(
          campo('Data de nascimento (dd/mm/aaaa)'),
          '01/01/2099',
        );
        await tester.enterText(campo('Contacto de emergência'), 'abc');
        await tocar(tester, find.text('Seguinte'));

        expect(app.local, AppRouter.novaFicha);
        expect(find.text(ValidadorFicha.erroData), findsOneWidget);
        expect(
          find.text(
            'Contacto de emergência: tem de ser um número de telefone com algarismos, espaços e o prefixo + '
            'opcional, até 30 caracteres, por exemplo +351 910 000 001.',
          ),
          findsOneWidget,
        );
        expect(find.text('Maria'), findsOneWidget);
        expect(find.text('01/01/2099'), findsOneWidget);
      },
    );

    testWidgets('campos obrigatórios em falta', (tester) async {
      await abrir(tester);
      await tocar(tester, find.text('Seguinte'));

      expect(find.text('Nome completo: é obrigatório.'), findsOneWidget);
      expect(
        find.text('Contacto de emergência: é obrigatório.'),
        findsOneWidget,
      );
      expect(find.text(ValidadorFicha.erroData), findsOneWidget);
    });

    testWidgets(
      'quem já tem ficha própria vê a opção indisponível (RF-031, D5)',
      (tester) async {
        app.fichas.juntar(
          fichaExemplo(id: 'f-p', nome: 'A minha ficha', propria: true),
        );
        await abrir(tester);

        expect(
          find.text('Já tem uma ficha própria. Só pode ter uma.'),
          findsOneWidget,
        );
        final opcao = tester.widget<RadioListTile<bool>>(
          find.widgetWithText(
            RadioListTile<bool>,
            'Gerir a minha própria saúde',
          ),
        );
        expect(opcao.enabled, isFalse);
      },
    );

    testWidgets('uma ficha própria de outra pessoa não conta (D5)', (
      tester,
    ) async {
      app.fichas.juntar(
        fichaExemplo(id: 'f-x', propria: true, principal: false),
      );
      await abrir(tester);
      expect(
        find.text('Já tem uma ficha própria. Só pode ter uma.'),
        findsNothing,
      );
    });
  });

  group('E07 Nova ficha: documentos e Cuidador principal', () {
    testWidgets(
      'ficha de terceiro com o criador como Cuidador principal e uma receita da galeria (RF-020, RF-022, RF-024)',
      (tester) async {
        await irParaDocumentos(tester);
        await carregar(tester, CategoriaDocumento.receitaMedica, 'Galeria');

        expect(find.text('receita.pdf'), findsOneWidget);
        expect(app.fonte.pedidas.single.name, 'galeria');

        await tocar(tester, find.text('Criar ficha'));

        final dados = app.fichas.ultimaCriacao!;
        expect(dados.propria, isFalse);
        expect(dados.dataNascimento, '1941-05-17');
        expect(dados.condicoesSaudeIds, [1]);
        expect(dados.cuidadorPrincipal!.eu, isTrue);
        final documento = app.fichas.ultimosDocumentos.single;
        expect(documento.categoria, CategoriaDocumento.receitaMedica);
        expect(documento.ficheiro.tipoConteudo, 'application/pdf');
        expect(app.local, AppRouter.ficha('f-nova'));
      },
    );

    testWidgets('ficha própria: sem o passo do Cuidador principal (RF-030)', (
      tester,
    ) async {
      await abrir(tester);
      await tocar(tester, find.text('Gerir a minha própria saúde'));
      await preencherDados(tester);
      await tocar(tester, find.text('Seguinte'));

      expect(find.text('Eu sou o Cuidador principal'), findsNothing);
      await tocar(tester, find.text('Criar ficha'));

      expect(app.fichas.ultimaCriacao!.propria, isTrue);
      expect(
        app.fichas.ultimaCriacao!.toJsonCriar().containsKey(
          'cuidadorPrincipal',
        ),
        isFalse,
      );
    });

    testWidgets(
      'convidar outra pessoa exige um e-mail válido (RF-025, RNF-028)',
      (tester) async {
        await irParaDocumentos(tester);
        await tocar(tester, find.textContaining('Convidar outra pessoa'));
        await tocar(tester, find.text('Criar ficha'));
        expect(
          find.text('E-mail da pessoa a convidar: é obrigatório.'),
          findsOneWidget,
        );
        expect(app.fichas.ultimaCriacao, isNull);

        await tester.enterText(
          campo('E-mail da pessoa a convidar'),
          'rui@exemplo.pt',
        );
        await tocar(tester, find.text('Criar ficha'));
        final principal = app.fichas.ultimaCriacao!.cuidadorPrincipal!;
        expect(principal.eu, isFalse);
        expect(principal.email, 'rui@exemplo.pt');
      },
    );

    testWidgets(
      'um ficheiro recusado no dispositivo não é junto e nada é enviado (RNF-008)',
      (tester) async {
        await irParaDocumentos(tester);
        app.fonte
          ..ficheiro = XFile.fromData(Uint8List.fromList('texto'.codeUnits))
          ..nome = 'nota.txt';
        await carregar(tester, CategoriaDocumento.relatorioClinico, 'Ficheiro');

        expect(
          find.text(
            'Relatório clínico: ${FicheiroRecusadoErro.formatoInvalido}',
          ),
          findsOneWidget,
        );
        expect(find.text('nota.txt'), findsNothing);
        expect(app.fichas.ultimaCriacao, isNull);
      },
    );

    testWidgets(
      'documento não guardado: mensagem do protótipo e os dados mantêm-se (RF-023, Declaração de Âmbito)',
      (tester) async {
        await irParaDocumentos(tester);
        await carregar(tester, CategoriaDocumento.receitaMedica, 'Câmara');
        app.fichas.erroCriar = const ProblemaApi(
          codigo: 'DOCUMENTO_NAO_GUARDADO',
          mensagem: 'x',
          estadoHttp: 503,
        );

        await tocar(tester, find.text('Criar ficha'));

        expect(
          find.text(NovaFichaDocumentosController.documentoNaoGuardado),
          findsOneWidget,
        );
        expect(app.local, AppRouter.novaFichaDocumentos);
        expect(find.text('receita.pdf'), findsOneWidget);

        // A nova tentativa envia os mesmos dados e o mesmo ficheiro.
        app.fichas.erroCriar = null;
        await tocar(tester, find.text('Criar ficha'));
        expect(app.fichas.ultimosDocumentos, hasLength(1));
        expect(app.fichas.ultimaCriacao!.nomeCompleto, 'Maria Fictícia Santos');
        expect(app.local, AppRouter.ficha('f-nova'));
      },
    );

    testWidgets(
      'o servidor recusa um campo de E06: volta a E06 com o erro por baixo do campo e os dados (D4)',
      (tester) async {
        await irParaDocumentos(tester);
        app.fichas.erroCriar = const ProblemaApi(
          codigo: 'VALIDACAO_INVALIDA',
          mensagem: 'Os dados submetidos são inválidos.',
          estadoHttp: 400,
          campos: [
            ErroCampo(
              campo: 'contactoEmergencia',
              regra: 'Tem de ser um número de telefone válido.',
            ),
          ],
        );

        await tocar(tester, find.text('Criar ficha'));

        expect(app.local, AppRouter.novaFicha);
        expect(
          find.text(
            'Contacto de emergência: tem de ser um número de telefone válido.',
          ),
          findsOneWidget,
        );
        expect(find.text('Maria Fictícia Santos'), findsOneWidget);
        expect(find.text('+351 910 000 001'), findsOneWidget);
      },
    );

    testWidgets('erros sem campo de E06 ficam em E07', (tester) async {
      await irParaDocumentos(tester);
      await carregar(tester, CategoriaDocumento.receitaMedica, 'Ficheiro');
      app.fichas.erroCriar = const ProblemaApi(
        codigo: 'VALIDACAO_INVALIDA',
        mensagem: 'x',
        estadoHttp: 400,
        campos: [
          ErroCampo(
            campo: 'documentos[0].categoria',
            regra: 'É obrigatória e tem de ser ReceitaMedica, RelatorioClinico ou DocumentoIdentificacao.',
          ),
        ],
      );

      await tocar(tester, find.text('Criar ficha'));

      expect(app.local, AppRouter.novaFichaDocumentos);
      expect(
        find.textContaining('Documento 1 (receita.pdf): é obrigatória'),
        findsOneWidget,
      );
    });

    testWidgets('409 FICHA_PROPRIA_JA_EXISTE tem mensagem própria (D5)', (
      tester,
    ) async {
      await abrir(tester);
      await tocar(tester, find.text('Gerir a minha própria saúde'));
      await preencherDados(tester);
      await tocar(tester, find.text('Seguinte'));
      app.fichas.erroCriar = const ProblemaApi(
        codigo: 'FICHA_PROPRIA_JA_EXISTE',
        mensagem: 'x',
        estadoHttp: 409,
      );

      await tocar(tester, find.text('Criar ficha'));

      expect(
        find.text(NovaFichaDocumentosController.fichaPropriaJaExiste),
        findsOneWidget,
      );
    });

    testWidgets('413 e sem ligação mostram a mensagem e mantêm os dados', (
      tester,
    ) async {
      await irParaDocumentos(tester);
      app.fichas.erroCriar = const ProblemaApi(
        codigo: 'FICHEIRO_DEMASIADO_GRANDE',
        mensagem: 'x',
        estadoHttp: 413,
      );
      await tocar(tester, find.text('Criar ficha'));
      expect(
        find.text('O ficheiro tem mais de 10 MB; o tamanho máximo é 10 MB.'),
        findsOneWidget,
      );

      app.fichas.erroCriar = const SemLigacaoErro();
      await tocar(tester, find.text('Criar ficha'));
      expect(find.text(SemLigacaoErro.mensagemPadrao), findsOneWidget);
      expect(app.local, AppRouter.novaFichaDocumentos);
    });

    testWidgets('recuar e avançar mantém os dados e os documentos', (
      tester,
    ) async {
      await irParaDocumentos(tester);
      await carregar(
        tester,
        CategoriaDocumento.documentoIdentificacao,
        'Ficheiro',
      );

      app.router.pop();
      await tester.pumpAndSettle();
      expect(find.text('Maria Fictícia Santos'), findsOneWidget);

      await tocar(tester, find.text('Seguinte'));
      expect(find.text('receita.pdf'), findsOneWidget);
      expect(find.text('Documento de identificação'), findsWidgets);
    });
  });

  group('Acessibilidade (RNF-025, RNF-026)', () {
    testWidgets('E06: alvos de toque de pelo menos 48 x 48 dp', (tester) async {
      final semantica = tester.ensureSemantics();
      await abrir(tester);
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });

    testWidgets('E07: alvos de toque de pelo menos 48 x 48 dp', (tester) async {
      final semantica = tester.ensureSemantics();
      await irParaDocumentos(tester);
      await carregar(tester, CategoriaDocumento.receitaMedica, 'Ficheiro');
      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      semantica.dispose();
    });

    testWidgets('E06 e E07: letra a 150% sem erros de layout', (tester) async {
      await tester.pumpWidget(app.widget(escalaLetra: 1.5));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      await preencherDados(tester);
      await tocar(tester, find.text('Seguinte'));
      expect(tester.takeException(), isNull);
      expect(app.local, AppRouter.novaFichaDocumentos);
    });
  });
}
