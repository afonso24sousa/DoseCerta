import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/categoria_documento.dart';
import '../../models/documento_para_envio.dart';
import '../../models/dominios_necessidades_dto.dart';
import '../../models/problema_api.dart';
import '../../shared/ficheiro_recusado_erro.dart';
import '../../shared/origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import '../../shared/validador_ficha.dart';
import 'formulario_ficha.dart';
import 'lista_fichas_controller.dart';

/// Estado do ecrã E09 Editar ficha.
class EditarFichaEstado {
  /// Cria o estado.
  const EditarFichaEstado({
    this.dominios,
    this.ficheiros = const [],
    this.aCarregar = false,
    this.erroCarregar,
    this.aEscolher = false,
    this.aGuardar = false,
    this.erros = const {},
    this.erro,
  });

  /// Listas fechadas (OP-08).
  final DominiosNecessidadesDto? dominios;

  /// Documentos a acrescentar com a edição.
  final List<DocumentoParaEnvio> ficheiros;

  /// Indica que OP-08 está em curso.
  final bool aCarregar;

  /// Erro ao obter as listas.
  final String? erroCarregar;

  /// Indica que a câmara, a galeria ou o seletor de ficheiros está aberto.
  final bool aEscolher;

  /// Indica que OP-11 está em curso.
  final bool aGuardar;

  /// Erros por campo da API.
  final Map<String, String> erros;

  /// Erro geral.
  final String? erro;

  /// Indica se alguma ação está em curso.
  bool get ocupado => aCarregar || aEscolher || aGuardar;

  /// Copia o estado, alterando os valores indicados. Os erros não indicados são limpos.
  EditarFichaEstado copiar({
    DominiosNecessidadesDto? dominios,
    List<DocumentoParaEnvio>? ficheiros,
    bool aCarregar = false,
    String? erroCarregar,
    bool aEscolher = false,
    bool aGuardar = false,
    Map<String, String> erros = const {},
    String? erro,
  }) {
    return EditarFichaEstado(
      dominios: dominios ?? this.dominios,
      ficheiros: ficheiros ?? this.ficheiros,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      aEscolher: aEscolher,
      aGuardar: aGuardar,
      erros: erros,
      erro: erro,
    );
  }
}

/// Controlador do ecrã E09 (OP-08, OP-10, OP-11; RF-021, RF-022, RF-023, RF-029, RNF-008, RNF-028). A ficha vem do
/// [FichaAcessoProvider] (OP-10), partilhado com E08.
class EditarFichaController extends Notifier<EditarFichaEstado> {
  /// Cria o controlador da ficha [fichaId].
  EditarFichaController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Mensagem do protótipo (E09) quando o documento não é guardado: a ficha fica como estava (RF-023, CA3).
  static const String documentoNaoGuardado =
      'O documento não foi guardado. Nenhuma alteração foi guardada.';

  @override
  EditarFichaEstado build() => const EditarFichaEstado();

  /// OP-08: obtém as listas fechadas.
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true);
    try {
      final dominios = await ref
          .read(dominiosRepositorioProvider)
          .obterNecessidades();
      if (ref.mounted) {
        state = state.copiar(dominios: dominios);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroCarregar: MensagensErro.traduzir(erro));
      }
    }
  }

  /// Junta um documento da [categoria], escolhido na [origem] (RF-022, RNF-008).
  Future<void> escolherFicheiro(
    CategoriaDocumento categoria,
    OrigemFicheiro origem,
  ) async {
    state = state.copiar(aEscolher: true, erros: state.erros);
    try {
      final ficheiro = await ref
          .read(selecionadorFicheirosProvider)
          .escolher(origem);
      if (ref.mounted) {
        state = state.copiar(
          erros: state.erros,
          ficheiros: ficheiro == null
              ? null
              : [
                  ...state.ficheiros,
                  DocumentoParaEnvio(categoria: categoria, ficheiro: ficheiro),
                ],
        );
      }
    } on FicheiroRecusadoErro catch (erro) {
      if (ref.mounted) {
        state = state.copiar(
          erros: state.erros,
          erro: '${categoria.rotulo}: ${erro.mensagem}',
        );
      }
    } on Object {
      if (ref.mounted) {
        state = state.copiar(
          erros: state.erros,
          erro:
              '${categoria.rotulo}: ${FicheiroRecusadoErro.origemIndisponivel}',
        );
      }
    }
  }

  /// Retira o documento na posição [indice].
  void retirarFicheiro(int indice) {
    state = state.copiar(
      erros: state.erros,
      ficheiros: [...state.ficheiros]..removeAt(indice),
    );
  }

  /// OP-11: valida e guarda a ficha com os documentos a acrescentar. Devolve `true` se ficou guardada.
  Future<bool> guardar(FormularioFicha formulario) async {
    final erros = ValidadorFicha.validar(
      nomeCompleto: formulario.nome.text,
      dataNascimento: formulario.dataNascimento.text,
      contactoEmergencia: formulario.contacto.text,
      hoje: ref.read(relogioProvider).agora(),
    );
    if (erros.isNotEmpty) {
      state = state.copiar(erros: erros);
      return false;
    }
    state = state.copiar(aGuardar: true);
    try {
      final ficha = await ref
          .read(fichasRepositorioProvider)
          .editar(
            fichaId,
            formulario.dados(propria: false),
            documentos: state.ficheiros,
          );
      ref.read(fichaAcessoProvider(fichaId).notifier).atualizar(ficha);
      ref.read(listaFichasControllerProvider.notifier).carregar().ignore();
      if (ref.mounted) {
        state = state.copiar(ficheiros: const []);
      }
      return true;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        final todos = MensagensErro.mensagensDeCampo(
          problema,
          ValidadorFicha.rotulos,
        );
        // Os erros dos campos da ficha ficam por baixo dos campos; os restantes (por exemplo
        // `documentos[0].categoria`) não têm campo no ecrã e vão para a mensagem geral.
        final porCampo = {
          for (final entrada in todos.entries)
            if (ValidadorFicha.camposDaFicha.contains(entrada.key))
              entrada.key: entrada.value,
        };
        final semCampo = [
          for (final entrada in todos.entries)
            if (!porCampo.containsKey(entrada.key)) entrada.value,
        ];
        state = state.copiar(
          erros: porCampo,
          erro: semCampo.isNotEmpty
              ? semCampo.join('\n')
              : porCampo.isNotEmpty
              ? null
              : problema.codigo == 'DOCUMENTO_NAO_GUARDADO'
              ? documentoNaoGuardado
              : MensagensErro.traduzir(problema),
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erro: MensagensErro.traduzir(erro));
      }
    }
    return false;
  }
}

/// Fornecedor do controlador de E09, por ficha.
final editarFichaControllerProvider = NotifierProvider.autoDispose
    .family<EditarFichaController, EditarFichaEstado, String>(
      EditarFichaController.new,
    );
