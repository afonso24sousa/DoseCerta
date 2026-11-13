import 'package:dosecerta/core/app_router.dart';
import 'package:dosecerta/core/mensagens_erro.dart';
import 'package:dosecerta/features/fichas/documentos_ficha_controller.dart';
import 'package:dosecerta/models/categoria_documento.dart';
import 'package:dosecerta/models/documento_dto.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/problema_api.dart';
import 'package:dosecerta/shared/abridor_documentos.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../suporte/app_fichas.dart';
import '../suporte/falsos_fichas.dart';

const DocumentoDto receita = DocumentoDto(
  id: 'd-r',
  categoria: CategoriaDocumento.receitaMedica,
  nomeOriginal: 'receita.pdf',
  formato: 'PDF',
  tamanhoBytes: 125,
  carregadoEm: '2026-11-09T20:38:11+00:00',
);

const DocumentoDto identificacao = DocumentoDto(
  id: 'd-i',
  categoria: CategoriaDocumento.documentoIdentificacao,
  nomeOriginal: 'id.png',
  formato: 'PNG',
  tamanhoBytes: 69,
  carregadoEm: '2026-11-09T20:38:11+00:00',
);

void main() {
  // Um toque que não acerta no alvo faz o teste falhar.
  WidgetController.hitTestWarningShouldBeFatal = true;

  late AppFichas app;

  setUp(() {
    app = AppFichas(inicio: AppRouter.documentosFicha('f-1'));
    app.documentos.documentos.addAll([receita, identificacao]);
  });

  Future<void> abrir(WidgetTester tester) async {
    await tester.pumpWidget(app.widget());
    await tester.pumpAndSettle();
  }

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await tester.ensureVisible(alvo);
    await tester.pumpAndSettle();
    await tester.tap(alvo);
    await tester.pumpAndSettle();
  }

  Finder abrirDe(String nome) => find.descendant(
    of: find.ancestor(
      of: find.textContaining(nome),
      matching: find.byType(Card),
    ),
    matching: find.widgetWithText(OutlinedButton, 'Abrir'),
  );

  group('E10 Documentos da ficha', () {
    testWidgets(
      'lista os documentos com a categoria e abre-os no visualizador (RF-028, OP-17)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);

        expect(find.text('Receita médica'), findsOneWidget);
        expect(find.text('receita.pdf · 09/11/2026'), findsOneWidget);
        expect(find.text('Documento de identificação'), findsOneWidget);

        await tocar(tester, abrirDe('id.png'));
        expect(app.abridor.abertos.single, ('id.png', 'image/png', 4));
      },
    );

    testWidgets(
      'documento indisponível: erro no lugar do ficheiro e tentar de novo, o resto mantém-se (RF-124)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        app.documentos.erroDescarregar = const ProblemaApi(
          codigo: 'DOCUMENTO_INDISPONIVEL',
          mensagem: 'x',
          estadoHttp: 503,
        );
        await abrir(tester);

        await tocar(tester, abrirDe('receita.pdf'));
        expect(
          find.text(DocumentosFichaController.documentoIndisponivel),
          findsOneWidget,
        );
        expect(find.text('id.png · 09/11/2026'), findsOneWidget);
        expect(abrirDe('id.png'), findsOneWidget);

        app.documentos.erroDescarregar = null;
        await tocar(tester, find.text('Tentar novamente'));
        expect(
          find.text(DocumentosFichaController.documentoIndisponivel),
          findsNothing,
        );
        expect(app.abridor.abertos.single.$1, 'receita.pdf');
        expect(app.documentos.descargas, 2);
      },
    );

    testWidgets('sem aplicação para abrir: mensagem no lugar do ficheiro', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo());
      app.abridor.erro = const DocumentoNaoAbertoErro(
        DocumentoNaoAbertoErro.semAplicacao,
      );
      await abrir(tester);
      await tocar(tester, abrirDe('receita.pdf'));
      expect(find.text(DocumentoNaoAbertoErro.semAplicacao), findsOneWidget);
    });

    testWidgets(
      'o Cuidador acrescenta um documento: categoria, depois origem (RF-022, D3)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        await abrir(tester);

        await tocar(tester, find.text('Carregar: Documento a acrescentar'));
        await tester.tap(find.text('Relatório clínico'));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Ficheiro'));
        await tester.pumpAndSettle();

        final enviado = app.documentos.ultimoCarregado!;
        expect(enviado.categoria, CategoriaDocumento.relatorioClinico);
        expect(enviado.ficheiro.nome, 'receita.pdf');
        expect(find.text('Relatório clínico'), findsOneWidget);
      },
    );

    testWidgets(
      'documento não guardado: mensagem e a lista fica como estava (RF-023, CA3)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        app.documentos.erroCarregar = const ProblemaApi(
          codigo: 'DOCUMENTO_NAO_GUARDADO',
          mensagem: 'x',
          estadoHttp: 503,
        );
        await abrir(tester);

        await tocar(tester, find.text('Carregar: Documento a acrescentar'));
        await tester.tap(find.text('Receita médica').last);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Galeria'));
        await tester.pumpAndSettle();

        expect(
          find.text(DocumentosFichaController.documentoNaoGuardado),
          findsOneWidget,
        );
        expect(find.byType(Card), findsNWidgets(2));
      },
    );

    testWidgets('o Familiar abre documentos mas não os carrega (RF-048)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(papel: Papel.familiar, principal: false));
      await abrir(tester);

      expect(find.text('Carregar: Documento a acrescentar'), findsNothing);
      await tocar(tester, abrirDe('receita.pdf'));
      expect(app.abridor.abertos, hasLength(1));
    });

    testWidgets('ficha arquivada: só consulta, sem carregar (RF-120)', (
      tester,
    ) async {
      app.fichas.juntar(fichaExemplo(estado: EstadoFicha.arquivada));
      await abrir(tester);

      expect(
        find.text(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
        findsOneWidget,
      );
      expect(find.text('Carregar: Documento a acrescentar'), findsNothing);
      expect(abrirDe('receita.pdf'), findsOneWidget);
    });

    testWidgets(
      'alvos de toque de 48 x 48 dp e letra a 150% (RNF-025, RNF-026)',
      (tester) async {
        app.fichas.juntar(fichaExemplo());
        final semantica = tester.ensureSemantics();
        await abrir(tester);
        await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
        semantica.dispose();

        await tester.pumpWidget(app.widget(escalaLetra: 1.5));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
      },
    );
  });
}
