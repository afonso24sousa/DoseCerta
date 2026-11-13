import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/categoria_documento.dart';
import '../../models/definicao_principal_dto.dart';
import '../../models/documento_para_envio.dart';
import '../../models/ficha_dto.dart';
import '../../models/problema_api.dart';
import '../../shared/ficheiro_recusado_erro.dart';
import '../../shared/nova_ficha_rascunho.dart';
import '../../shared/origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import '../../shared/validador_ficha.dart';
import 'lista_fichas_controller.dart';

/// Resultado de «Criar ficha» em E07.
enum ResultadoNovaFicha {
  /// A ficha foi criada: abre-se E08.
  criada,

  /// O servidor recusou campos de E06: volta-se a E06, com os erros por baixo dos campos (decisão D4).
  voltarAosDados,

  /// A ficha não foi criada; o erro aparece em E07 e os dados mantêm-se (RF-023, RNF-028).
  recusada,
}

/// Estado do ecrã E07 Nova ficha: documentos e Cuidador principal.
class NovaFichaDocumentosEstado {
  /// Cria o estado.
  const NovaFichaDocumentosEstado({
    this.aEscolher = false,
    this.aEnviar = false,
    this.erro,
    this.erroEmail,
    this.fichaCriada,
  });

  /// Indica que a câmara, a galeria ou o seletor de ficheiros está aberto.
  final bool aEscolher;

  /// Indica que OP-09 está em curso.
  final bool aEnviar;

  /// Erro geral (ficheiro recusado, documento não guardado, sem ligação, ...).
  final String? erro;

  /// Erro do e-mail da pessoa a convidar para Cuidador principal.
  final String? erroEmail;

  /// Ficha criada.
  final FichaDto? fichaCriada;

  /// Indica se alguma ação está em curso.
  bool get ocupado => aEscolher || aEnviar;
}

/// Controlador do ecrã E07 (OP-09; RF-020, RF-022 a RF-025, RF-030, RNF-008, RNF-014). Os documentos, a escolha do
/// Cuidador principal e o e-mail ficam no [NovaFichaRascunho], por isso nenhuma falha apaga o que foi preenchido.
class NovaFichaDocumentosController
    extends Notifier<NovaFichaDocumentosEstado> {
  /// Mensagem do protótipo (E07) quando o documento não é guardado e a ficha não é criada (RF-023).
  static const String documentoNaoGuardado =
      'O documento não foi guardado. A ficha não foi criada e os dados mantêm-se. Tente novamente.';

  /// Mensagem própria para o `409` `FICHA_PROPRIA_JA_EXISTE`, que só chega se a lista de E06 estava
  /// desatualizada (decisão D5).
  static const String fichaPropriaJaExiste =
      'Já tem uma ficha própria. Só pode ter uma. Volte ao passo anterior e escolha «Ficha de uma terceira pessoa».';

  /// Rótulo do e-mail da pessoa a convidar.
  static const String rotuloEmail = 'E-mail da pessoa a convidar';

  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  @override
  NovaFichaDocumentosEstado build() => const NovaFichaDocumentosEstado();

  NovaFichaRascunho get _rascunho =>
      ref.read(novaFichaRascunhoProvider.notifier);

  /// Abre a [origem] e junta o ficheiro escolhido como documento da [categoria] (RF-022). Um ficheiro que não é
  /// PDF, JPG ou PNG, ou que tem mais de 10 MB, é recusado antes de qualquer pedido (RNF-008).
  Future<void> escolherFicheiro(
    CategoriaDocumento categoria,
    OrigemFicheiro origem,
  ) async {
    state = const NovaFichaDocumentosEstado(aEscolher: true);
    try {
      final ficheiro = await ref
          .read(selecionadorFicheirosProvider)
          .escolher(origem);
      if (ficheiro != null) {
        _rascunho.juntarDocumento(
          DocumentoParaEnvio(categoria: categoria, ficheiro: ficheiro),
        );
      }
      if (ref.mounted) {
        state = const NovaFichaDocumentosEstado();
      }
    } on FicheiroRecusadoErro catch (erro) {
      if (ref.mounted) {
        state = NovaFichaDocumentosEstado(
          erro: '${categoria.rotulo}: ${erro.mensagem}',
        );
      }
    } on Object {
      // Qualquer outra falha ao ler o ficheiro: o ecrã nunca fica bloqueado em «a escolher».
      if (ref.mounted) {
        state = NovaFichaDocumentosEstado(
          erro:
              '${categoria.rotulo}: ${FicheiroRecusadoErro.origemIndisponivel}',
        );
      }
    }
  }

  /// Retira o documento na posição [indice].
  void retirarDocumento(int indice) {
    _rascunho.retirarDocumento(indice);
    state = const NovaFichaDocumentosEstado();
  }

  /// OP-09: cria a ficha com os dados de E06, os documentos e, numa ficha de terceiro, o Cuidador principal.
  Future<ResultadoNovaFicha> criarFicha() async {
    final rascunho = ref.read(novaFichaRascunhoProvider);
    final dados = rascunho.dados;
    if (dados == null) {
      return ResultadoNovaFicha.voltarAosDados;
    }
    DefinicaoPrincipalDto? principal;
    if (!dados.propria) {
      if (rascunho.principalEu) {
        principal = const DefinicaoPrincipalDto.eu();
      } else {
        final email = rascunho.emailPrincipal.trim();
        if (email.isEmpty || !_email.hasMatch(email)) {
          state = NovaFichaDocumentosEstado(
            erroEmail: MensagensErro.comRotulo(
              rotuloEmail,
              email.isEmpty
                  ? 'É obrigatório.'
                  : 'Indique um e-mail válido, por exemplo rui@exemplo.pt.',
            ),
          );
          return ResultadoNovaFicha.recusada;
        }
        principal = DefinicaoPrincipalDto.convidar(email);
      }
    }

    state = const NovaFichaDocumentosEstado(aEnviar: true);
    try {
      final ficha = await ref
          .read(fichasRepositorioProvider)
          .criar(
            dados.comPrincipal(principal),
            documentos: rascunho.documentos,
          );
      _rascunho.limpar();
      // A lista de E05 passa a incluir a ficha nova.
      ref.read(listaFichasControllerProvider.notifier).carregar().ignore();
      if (ref.mounted) {
        state = NovaFichaDocumentosEstado(fichaCriada: ficha);
      }
      return ResultadoNovaFicha.criada;
    } on ProblemaApi catch (problema) {
      return _recusada(problema, rascunho.documentos);
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = NovaFichaDocumentosEstado(erro: MensagensErro.traduzir(erro));
      }
      return ResultadoNovaFicha.recusada;
    }
  }

  ResultadoNovaFicha _recusada(
    ProblemaApi problema,
    List<DocumentoParaEnvio> documentos,
  ) {
    if (problema.campos.isNotEmpty) {
      final camposFicha = ValidadorFicha.camposDaFicha;
      final daFicha = <String, String>{};
      final outros = <String>[];
      String? erroEmail;
      for (final erro in problema.campos) {
        if (camposFicha.contains(erro.campo)) {
          daFicha.putIfAbsent(
            erro.campo,
            () => MensagensErro.comRotulo(
              ValidadorFicha.rotulos[erro.campo]!,
              erro.regra,
            ),
          );
        } else if (erro.campo.startsWith('cuidadorPrincipal')) {
          erroEmail = MensagensErro.comRotulo(rotuloEmail, erro.regra);
        } else {
          outros.add(
            MensagensErro.comRotulo(
              _rotuloDocumento(erro.campo, documentos),
              erro.regra,
            ),
          );
        }
      }
      if (daFicha.isNotEmpty) {
        _rascunho.indicarErrosDados(daFicha);
      }
      if (ref.mounted) {
        state = NovaFichaDocumentosEstado(
          erroEmail: erroEmail,
          erro: outros.isEmpty ? null : outros.join('\n'),
        );
      }
      return daFicha.isNotEmpty
          ? ResultadoNovaFicha.voltarAosDados
          : ResultadoNovaFicha.recusada;
    }
    final mensagem = switch (problema.codigo) {
      'DOCUMENTO_NAO_GUARDADO' => documentoNaoGuardado,
      'FICHA_PROPRIA_JA_EXISTE' => fichaPropriaJaExiste,
      _ => MensagensErro.traduzir(problema),
    };
    if (ref.mounted) {
      state = NovaFichaDocumentosEstado(erro: mensagem);
    }
    return ResultadoNovaFicha.recusada;
  }

  /// Rótulo de um campo de documento devolvido pelo servidor (`documentos[i].categoria`, `dados`).
  static String _rotuloDocumento(
    String campo,
    List<DocumentoParaEnvio> documentos,
  ) {
    final posicao = RegExp(r'^documentos\[(\d+)\]').firstMatch(campo);
    if (posicao != null) {
      final indice = int.parse(posicao.group(1)!);
      final nome = indice < documentos.length
          ? ' (${documentos[indice].ficheiro.nome})'
          : '';
      return 'Documento ${indice + 1}$nome';
    }
    return 'Dados da ficha';
  }
}

/// Fornecedor do controlador de E07.
final novaFichaDocumentosControllerProvider =
    NotifierProvider.autoDispose<
      NovaFichaDocumentosController,
      NovaFichaDocumentosEstado
    >(NovaFichaDocumentosController.new);
