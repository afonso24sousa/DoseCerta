import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/categoria_documento.dart';
import '../../models/documento_dto.dart';
import '../../models/documento_para_envio.dart';
import '../../models/problema_api.dart';
import '../../shared/abridor_documentos.dart';
import '../../shared/ficheiro_recusado_erro.dart';
import '../../shared/origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';

/// Estado do ecrã E10 Documentos da ficha.
class DocumentosFichaEstado {
  /// Cria o estado.
  const DocumentosFichaEstado({
    this.documentos = const [],
    this.aCarregar = false,
    this.carregada = false,
    this.erroLista,
    this.aEnviar = false,
    this.erro,
    this.aAbrir,
    this.errosAbertura = const {},
  });

  /// Documentos da ficha (OP-16).
  final List<DocumentoDto> documentos;

  /// Indica que OP-16 está em curso.
  final bool aCarregar;

  /// Indica que a lista já foi obtida pelo menos uma vez.
  final bool carregada;

  /// Erro ao obter a lista.
  final String? erroLista;

  /// Indica que um ficheiro está a ser escolhido ou enviado (OP-15).
  final bool aEnviar;

  /// Erro do carregamento de um documento.
  final String? erro;

  /// Documento a ser descarregado e aberto (OP-17).
  final String? aAbrir;

  /// Erro de abertura de cada documento, mostrado no lugar do ficheiro (RF-124).
  final Map<String, String> errosAbertura;

  /// Copia o estado, alterando os valores indicados.
  DocumentosFichaEstado copiar({
    List<DocumentoDto>? documentos,
    bool? aCarregar,
    bool? carregada,
    String? Function()? erroLista,
    bool? aEnviar,
    String? Function()? erro,
    String? Function()? aAbrir,
    Map<String, String>? errosAbertura,
  }) {
    return DocumentosFichaEstado(
      documentos: documentos ?? this.documentos,
      aCarregar: aCarregar ?? this.aCarregar,
      carregada: carregada ?? this.carregada,
      erroLista: erroLista == null ? this.erroLista : erroLista(),
      aEnviar: aEnviar ?? this.aEnviar,
      erro: erro == null ? this.erro : erro(),
      aAbrir: aAbrir == null ? this.aAbrir : aAbrir(),
      errosAbertura: errosAbertura ?? this.errosAbertura,
    );
  }
}

/// Controlador do ecrã E10 (OP-15, OP-16, OP-17; RF-022, RF-023, RF-028, RF-124, RNF-007, RNF-008, RNF-014).
class DocumentosFichaController extends Notifier<DocumentosFichaEstado> {
  /// Cria o controlador da ficha [fichaId].
  DocumentosFichaController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Mensagem quando o documento acrescentado não é guardado: a ficha fica como estava (RF-023, CA3).
  static const String documentoNaoGuardado =
      'O documento não foi guardado. A ficha ficou como estava. Tente novamente.';

  /// Mensagem do protótipo (E10) quando o documento não se obtém (RF-124).
  static const String documentoIndisponivel =
      'Não foi possível obter o documento.';

  static const Map<String, String> _tiposConteudo = {
    'PDF': 'application/pdf',
    'JPG': 'image/jpeg',
    'PNG': 'image/png',
  };

  @override
  DocumentosFichaEstado build() => const DocumentosFichaEstado();

  /// OP-16: obtém a lista de documentos.
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true, erroLista: () => null);
    try {
      final documentos = await ref
          .read(documentosRepositorioProvider)
          .listar(fichaId);
      if (ref.mounted) {
        state = state.copiar(
          documentos: documentos,
          aCarregar: false,
          carregada: true,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(
          aCarregar: false,
          erroLista: () => MensagensErro.traduzir(erro),
        );
      }
    }
  }

  /// Escolhe um ficheiro na [origem] e carrega-o como documento da [categoria] (OP-15). O ficheiro é validado no
  /// dispositivo antes do envio (RNF-008).
  Future<void> carregarDocumento(
    CategoriaDocumento categoria,
    OrigemFicheiro origem,
  ) async {
    state = state.copiar(aEnviar: true, erro: () => null);
    try {
      final ficheiro = await ref
          .read(selecionadorFicheirosProvider)
          .escolher(origem);
      if (ficheiro == null) {
        state = state.copiar(aEnviar: false);
        return;
      }
      final documento = await ref
          .read(documentosRepositorioProvider)
          .carregar(
            fichaId,
            DocumentoParaEnvio(categoria: categoria, ficheiro: ficheiro),
          );
      if (ref.mounted) {
        state = state.copiar(
          documentos: [...state.documentos, documento],
          aEnviar: false,
        );
      }
    } on FicheiroRecusadoErro catch (erro) {
      _erroEnvio('${categoria.rotulo}: ${erro.mensagem}');
    } on ProblemaApi catch (problema) {
      _erroEnvio(_mensagemEnvio(problema));
    } on Exception catch (erro) {
      _erroEnvio(MensagensErro.traduzir(erro));
    } on Object {
      _erroEnvio(
        '${categoria.rotulo}: ${FicheiroRecusadoErro.origemIndisponivel}',
      );
    }
  }

  /// OP-17: descarrega o documento e abre-o no visualizador do dispositivo. Se falhar, o erro aparece no lugar do
  /// documento, com a possibilidade de repetir, e o resto da ficha não é afetado (RF-124).
  Future<void> abrirDocumento(DocumentoDto documento) async {
    final erros = {...state.errosAbertura}..remove(documento.id);
    state = state.copiar(aAbrir: () => documento.id, errosAbertura: erros);
    String? mensagem;
    try {
      final bytes = await ref
          .read(documentosRepositorioProvider)
          .descarregar(documento.id);
      await ref
          .read(abridorDocumentosProvider)
          .abrir(
            bytes,
            nome: documento.nomeOriginal,
            tipoConteudo:
                _tiposConteudo[documento.formato.toUpperCase()] ??
                'application/octet-stream',
          );
    } on ProblemaApi catch (problema) {
      mensagem = problema.codigo == 'DOCUMENTO_INDISPONIVEL'
          ? documentoIndisponivel
          : MensagensErro.traduzir(problema);
    } on DocumentoNaoAbertoErro catch (erro) {
      mensagem = erro.mensagem;
    } on Exception catch (erro) {
      mensagem = MensagensErro.traduzir(erro);
    }
    if (ref.mounted) {
      state = state.copiar(
        aAbrir: () => null,
        errosAbertura: mensagem == null
            ? state.errosAbertura
            : {...state.errosAbertura, documento.id: mensagem},
      );
    }
  }

  /// Repete a abertura de um documento que falhou (RF-124).
  Future<void> repetirAbertura(DocumentoDto documento) =>
      abrirDocumento(documento);

  void _erroEnvio(String mensagem) {
    if (ref.mounted) {
      state = state.copiar(aEnviar: false, erro: () => mensagem);
    }
  }

  static String _mensagemEnvio(ProblemaApi problema) {
    if (problema.campos.isNotEmpty) {
      return MensagensErro.mensagensDeCampo(problema, const {
        'categoria': 'Categoria',
        'ficheiro': 'Ficheiro',
      }).values.join('\n');
    }
    if (problema.codigo == 'DOCUMENTO_NAO_GUARDADO') {
      return documentoNaoGuardado;
    }
    return MensagensErro.traduzir(problema);
  }
}

/// Fornecedor do controlador de E10, por ficha.
final documentosFichaControllerProvider = NotifierProvider.autoDispose
    .family<DocumentosFichaController, DocumentosFichaEstado, String>(
      DocumentosFichaController.new,
    );
