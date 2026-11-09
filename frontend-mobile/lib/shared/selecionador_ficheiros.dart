import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../models/ficheiro_escolhido.dart';
import 'ficheiro_recusado_erro.dart';
import 'origem_ficheiro.dart';

/// Origem dos ficheiros no dispositivo. Separada do [SelecionadorFicheiros] para os testes poderem substituir a
/// câmara, a galeria e o seletor de ficheiros.
abstract interface class FonteFicheiros {
  /// Abre a [origem]; devolve `null` se o Utilizador cancelar.
  Future<XFile?> escolher(OrigemFicheiro origem);
}

/// Câmara e galeria pelo `image_picker` e ficheiros pelo `file_picker`. Nenhum precisa de permissões: a câmara é a
/// aplicação do sistema e a galeria e os ficheiros usam os seletores do sistema (passo 1 da I060).
class FonteFicheirosDispositivo implements FonteFicheiros {
  /// Cria a fonte.
  FonteFicheirosDispositivo([ImagePicker? imagens])
    : imagens = imagens ?? ImagePicker();

  /// Largura máxima das fotografias, para ficarem bem abaixo dos 10 MB e serem rápidas a enviar (RNF-014).
  static const double larguraMaxima = 2560;

  /// Qualidade JPEG das fotografias redimensionadas.
  static const int qualidade = 85;

  /// Câmara e galeria.
  final ImagePicker imagens;

  @override
  Future<XFile?> escolher(OrigemFicheiro origem) async {
    switch (origem) {
      case OrigemFicheiro.camera:
      case OrigemFicheiro.galeria:
        return imagens.pickImage(
          source: origem == OrigemFicheiro.camera
              ? ImageSource.camera
              : ImageSource.gallery,
          maxWidth: larguraMaxima,
          maxHeight: larguraMaxima,
          imageQuality: qualidade,
        );
      case OrigemFicheiro.ficheiro:
        final ficheiro = await FilePicker.pickFile(
          type: FileType.custom,
          allowedExtensions: const ['pdf', 'jpg', 'jpeg', 'png'],
        );
        return ficheiro?.xFile;
    }
  }
}

/// Escolha do ficheiro a carregar, pela câmara, pela galeria ou pelo seletor de ficheiros, com validação prévia
/// de formato (PDF, JPG ou PNG) e tamanho (10 MB) (RF-022, RF-052, RNF-008). O formato é reconhecido pelo conteúdo,
/// como faz o servidor, e não só pela extensão.
class SelecionadorFicheiros {
  /// Cria o selecionador sobre a [fonte].
  const SelecionadorFicheiros(this.fonte);

  /// Tamanho máximo de cada ficheiro (RNF-008).
  static const int tamanhoMaximoBytes = 10 * 1024 * 1024;

  /// Indicação dos ficheiros aceites, apresentada junto do carregamento (protótipo, E07 e E10).
  static const String ficheirosAceites =
      'PDF, JPG ou PNG, até 10 MB por ficheiro.';

  /// Origem dos ficheiros.
  final FonteFicheiros fonte;

  /// Abre a [origem] e valida o ficheiro. Devolve `null` se o Utilizador cancelar; lança [FicheiroRecusadoErro]
  /// se o ficheiro não for aceite ou a origem falhar.
  Future<FicheiroEscolhido?> escolher(OrigemFicheiro origem) async {
    final XFile? ficheiro;
    try {
      ficheiro = await fonte.escolher(origem);
    } on PlatformException {
      throw const FicheiroRecusadoErro(FicheiroRecusadoErro.origemIndisponivel);
    }
    if (ficheiro == null) {
      return null;
    }
    return validar(ficheiro);
  }

  /// Ficheiro pelo seletor de ficheiros.
  Future<FicheiroEscolhido?> doFicheiro() => escolher(OrigemFicheiro.ficheiro);

  /// Fotografia pela câmara.
  Future<FicheiroEscolhido?> daCamara() => escolher(OrigemFicheiro.camera);

  /// Imagem pela galeria.
  Future<FicheiroEscolhido?> daGaleria() => escolher(OrigemFicheiro.galeria);

  /// Valida o [ficheiro]: tamanho até 10 MB e conteúdo PDF, JPG ou PNG. Lança [FicheiroRecusadoErro] se não for
  /// aceite.
  static Future<FicheiroEscolhido> validar(XFile ficheiro) async {
    final tamanho = await ficheiro.length();
    if (tamanho > tamanhoMaximoBytes) {
      throw const FicheiroRecusadoErro(FicheiroRecusadoErro.demasiadoGrande);
    }
    final cabecalho = <int>[];
    await for (final bloco in ficheiro.openRead(0, 8)) {
      cabecalho.addAll(bloco);
    }
    final tipo = tipoPeloConteudo(cabecalho);
    if (tipo == null) {
      throw const FicheiroRecusadoErro(FicheiroRecusadoErro.formatoInvalido);
    }
    final caminho = ficheiro.path;
    final temCaminho = caminho.isNotEmpty && File(caminho).existsSync();
    return FicheiroEscolhido(
      nome: _nomeSimples(ficheiro.name) ?? _nomePorOmissao(tipo),
      tipoConteudo: tipo,
      tamanhoBytes: tamanho,
      caminho: temCaminho ? caminho : null,
      bytes: temCaminho ? null : await ficheiro.readAsBytes(),
    );
  }

  /// Tipo de conteúdo pelos primeiros bytes do ficheiro: `application/pdf` (`%PDF`), `image/jpeg` (`FF D8 FF`) ou
  /// `image/png` (`89 50 4E 47 0D 0A 1A 0A`); `null` para qualquer outro conteúdo.
  static String? tipoPeloConteudo(List<int> cabecalho) {
    bool comeca(List<int> assinatura) =>
        cabecalho.length >= assinatura.length &&
        Iterable<int>.generate(assinatura.length)
            .every((i) => cabecalho[i] == assinatura[i]);

    if (comeca(const [0x25, 0x50, 0x44, 0x46])) {
      return 'application/pdf';
    }
    if (comeca(const [0xFF, 0xD8, 0xFF])) {
      return 'image/jpeg';
    }
    if (comeca(const [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) {
      return 'image/png';
    }
    return null;
  }

  /// Nome do ficheiro sem pastas, seja qual for o separador; `null` se ficar vazio.
  static String? _nomeSimples(String nome) {
    final simples = nome.split(RegExp(r'[/\\]')).last.trim();
    return simples.isEmpty ? null : simples;
  }

  static String _nomePorOmissao(String tipo) => switch (tipo) {
    'application/pdf' => 'documento.pdf',
    'image/png' => 'imagem.png',
    _ => 'fotografia.jpg',
  };
}

/// Selecionador de ficheiros da aplicação (substituído nos testes).
final selecionadorFicheirosProvider = Provider<SelecionadorFicheiros>(
  (ref) => SelecionadorFicheiros(FonteFicheirosDispositivo()),
);
