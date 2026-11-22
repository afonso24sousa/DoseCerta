import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/problema_api.dart';
import '../../models/tratamento_dto.dart';
import '../../shared/abridor_documentos.dart';
import '../../shared/selecionador_ficheiros.dart';
import 'apresentacao_tratamento.dart';
import 'medicacao_ficha_controller.dart';

/// Estado do ecrã E17 Detalhe do tratamento.
class TratamentoEstado {
  /// Cria o estado.
  const TratamentoEstado({
    this.tratamento,
    this.aCarregar = false,
    this.erroCarregar,
    this.aTerminar = false,
    this.erro,
    this.aAbrirReceita = false,
    this.erroReceita,
  });

  /// Tratamento (OP-29).
  final TratamentoDto? tratamento;

  /// Indica que OP-29 está em curso.
  final bool aCarregar;

  /// Erro ao obter o tratamento.
  final String? erroCarregar;

  /// Indica que OP-31 está em curso.
  final bool aTerminar;

  /// Erro ao terminar.
  final String? erro;

  /// Indica que a receita está a ser descarregada e aberta (OP-17).
  final bool aAbrirReceita;

  /// Erro ao abrir a receita, mostrado no lugar dela (RF-124).
  final String? erroReceita;

  /// Copia o estado, alterando os valores indicados. Os indicadores e os erros não indicados são limpos.
  TratamentoEstado copiar({
    TratamentoDto? tratamento,
    bool aCarregar = false,
    String? erroCarregar,
    bool aTerminar = false,
    String? erro,
    bool aAbrirReceita = false,
    String? erroReceita,
  }) {
    return TratamentoEstado(
      tratamento: tratamento ?? this.tratamento,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      aTerminar: aTerminar,
      erro: erro,
      aAbrirReceita: aAbrirReceita,
      erroReceita: erroReceita,
    );
  }
}

/// Controlador de E17 (OP-17, OP-29, OP-31; RF-054, RF-056, RF-057, RF-124).
class TratamentoController extends Notifier<TratamentoEstado> {
  /// Cria o controlador do tratamento [tratamentoId].
  TratamentoController(this.tratamentoId);

  /// Identificador do tratamento.
  final String tratamentoId;

  /// Mensagem do frontend web (I066) quando o tratamento é terminado.
  static const String terminado =
      'Tratamento terminado. As tomas futuras deixaram de existir; o histórico mantém-se.';

  /// Mensagem quando a receita não se obtém (RF-124).
  static const String receitaIndisponivel = 'Não foi possível obter a receita.';

  // Outro Cuidador terminou o tratamento ou mudou o papel: volta a pedir o tratamento para mostrar o estado atual.
  static const Set<String> _codigosRecarregar = {
    'TRATAMENTO_JA_TERMINADO',
    'ACESSO_PROIBIDO',
  };

  @override
  TratamentoEstado build() => const TratamentoEstado();

  /// OP-29: obtém o tratamento.
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true);
    try {
      final tratamento = await ref
          .read(tratamentosRepositorioProvider)
          .consultar(tratamentoId);
      if (ref.mounted) {
        state = state.copiar(tratamento: tratamento);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroCarregar: _traduzir(erro));
      }
    }
  }

  /// Substitui o tratamento pelo devolvido numa edição (OP-30), sem novo pedido.
  void substituir(TratamentoDto tratamento) {
    state = state.copiar(tratamento: tratamento);
  }

  /// OP-31: termina o tratamento antes do fim (RF-056). Devolve `true` se ficou terminado.
  Future<bool> terminar() async {
    state = state.copiar(aTerminar: true);
    try {
      final tratamento = await ref
          .read(tratamentosRepositorioProvider)
          .terminar(tratamentoId);
      if (ref.mounted) {
        ref.invalidate(tratamentosFichaProvider(tratamento.fichaId));
        state = state.copiar(tratamento: tratamento);
      }
      return true;
    } on Exception catch (erro) {
      if (ref.mounted) {
        final mensagem = _traduzir(erro);
        state = state.copiar(erro: mensagem);
        if (erro is ProblemaApi && _codigosRecarregar.contains(erro.codigo)) {
          await _recarregarMantendo(mensagem);
        }
      }
    }
    return false;
  }

  /// OP-17: descarrega a receita e abre-a no visualizador do dispositivo; se falhar, o erro aparece no lugar da
  /// receita, com a possibilidade de repetir (RF-124).
  Future<void> abrirReceita() async {
    final tratamento = state.tratamento;
    final documentoId = tratamento?.receitaDocumentoId;
    if (tratamento == null || documentoId == null) {
      return;
    }
    state = state.copiar(aAbrirReceita: true);
    String? mensagem;
    try {
      final bytes = await ref
          .read(documentosRepositorioProvider)
          .descarregar(documentoId);
      final tipo =
          SelecionadorFicheiros.tipoPeloConteudo(bytes) ??
          'application/octet-stream';
      await ref
          .read(abridorDocumentosProvider)
          .abrir(
            bytes,
            nome: 'receita${_extensoes[tipo] ?? ''}',
            tipoConteudo: tipo,
          );
    } on ProblemaApi catch (problema) {
      mensagem = problema.codigo == 'DOCUMENTO_INDISPONIVEL'
          ? receitaIndisponivel
          : MensagensErro.traduzir(problema);
    } on DocumentoNaoAbertoErro catch (erro) {
      mensagem = erro.mensagem;
    } on Exception catch (erro) {
      mensagem = MensagensErro.traduzir(erro);
    }
    if (ref.mounted) {
      state = state.copiar(erroReceita: mensagem);
    }
  }

  static const Map<String, String> _extensoes = {
    'application/pdf': '.pdf',
    'image/jpeg': '.jpg',
    'image/png': '.png',
  };

  Future<void> _recarregarMantendo(String mensagem) async {
    await carregar();
    if (ref.mounted && state.erroCarregar == null) {
      state = state.copiar(erro: mensagem);
    }
  }

  static String _traduzir(Object erro) => MensagensErro.traduzir(
    erro,
    especificas: ApresentacaoTratamento.mensagensErro,
  );
}

/// Fornecedor do controlador de E17, por tratamento.
final tratamentoControllerProvider = NotifierProvider.autoDispose
    .family<TratamentoController, TratamentoEstado, String>(
      TratamentoController.new,
    );
