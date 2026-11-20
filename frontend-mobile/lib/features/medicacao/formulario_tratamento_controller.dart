import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/dados_tratamento_dto.dart';
import '../../models/ficheiro_escolhido.dart';
import '../../models/grau_importancia.dart';
import '../../models/problema_api.dart';
import '../../models/tratamento_dto.dart';
import '../../shared/ficheiro_recusado_erro.dart';
import '../../shared/origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import '../../shared/validador_tratamento.dart';
import 'apresentacao_tratamento.dart';
import 'medicacao_ficha_controller.dart';
import 'tratamento_controller.dart';

/// Ficha e, na edição, o tratamento de E16.
typedef AlvoFormulario = ({String fichaId, String? tratamentoId});

/// Valores do formulário de E16. O texto vive nos controladores da página, por isso um erro nunca apaga o que foi
/// preenchido (RNF-028).
typedef ValoresTratamento = ({
  String medicamento,
  String dose,
  GrauImportancia? grau,
  String dataInicio,
  String dataFim,
  List<String> horarios,
});

/// Estado do ecrã E16 Registar ou editar tratamento.
class FormularioTratamentoEstado {
  /// Cria o estado.
  const FormularioTratamentoEstado({
    this.original,
    this.aCarregar = false,
    this.erroCarregar,
    this.receita,
    this.aEscolher = false,
    this.aGuardar = false,
    this.erros = const {},
    this.erro,
  });

  /// Tratamento a editar (OP-29); `null` no registo.
  final TratamentoDto? original;

  /// Indica que OP-29 está em curso.
  final bool aCarregar;

  /// Erro ao obter o tratamento.
  final String? erroCarregar;

  /// Receita escolhida e já validada (RNF-008).
  final FicheiroEscolhido? receita;

  /// Indica que a câmara, a galeria ou o seletor de ficheiros está aberto.
  final bool aEscolher;

  /// Indica que OP-27 ou OP-30 está em curso.
  final bool aGuardar;

  /// Erros por campo da API.
  final Map<String, String> erros;

  /// Erro geral.
  final String? erro;

  /// Indica se alguma ação está em curso.
  bool get ocupado => aCarregar || aEscolher || aGuardar;

  /// Copia o estado, alterando os valores indicados. Os indicadores e os erros não indicados são limpos.
  FormularioTratamentoEstado copiar({
    TratamentoDto? original,
    bool aCarregar = false,
    String? erroCarregar,
    FicheiroEscolhido? Function()? receita,
    bool aEscolher = false,
    bool aGuardar = false,
    Map<String, String> erros = const {},
    String? erro,
  }) {
    return FormularioTratamentoEstado(
      original: original ?? this.original,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      receita: receita == null ? this.receita : receita(),
      aEscolher: aEscolher,
      aGuardar: aGuardar,
      erros: erros,
      erro: erro,
    );
  }
}

/// Controlador de E16 (OP-27, OP-29, OP-30; RF-050 a RF-053, RF-055, RNF-008, RNF-028). No registo envia todos os
/// campos; na edição só os alterados, como o frontend web (I066), e nunca a data de início (RF-055).
class FormularioTratamentoController
    extends Notifier<FormularioTratamentoEstado> {
  /// Cria o controlador para o [alvo].
  FormularioTratamentoController(this.alvo);

  /// Ficha e tratamento (na edição).
  final AlvoFormulario alvo;

  /// Mensagem quando a receita não é guardada no registo: nada é gravado (RF-053).
  static const String receitaNaoGuardadaRegisto =
      'A receita não foi guardada e o tratamento não foi registado. Tente novamente.';

  /// Mensagem quando a receita não é guardada na edição: o tratamento fica como estava (RF-053).
  static const String receitaNaoGuardadaEdicao =
      'A receita não foi guardada. Nenhuma alteração foi guardada. Tente novamente.';

  static const Set<String> _codigosReceita = {
    'FICHEIRO_DEMASIADO_GRANDE',
    'FORMATO_NAO_SUPORTADO',
  };

  /// Indica se é uma edição.
  bool get edicao => alvo.tratamentoId != null;

  @override
  FormularioTratamentoEstado build() => const FormularioTratamentoEstado();

  /// OP-29: na edição, obtém o tratamento a editar.
  Future<void> carregar() async {
    final tratamentoId = alvo.tratamentoId;
    if (tratamentoId == null) {
      return;
    }
    state = state.copiar(aCarregar: true);
    try {
      final tratamento = await ref
          .read(tratamentosRepositorioProvider)
          .consultar(tratamentoId);
      if (ref.mounted) {
        state = state.copiar(original: tratamento);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroCarregar: _traduzir(erro));
      }
    }
  }

  /// Escolhe a receita na [origem] e valida-a antes do envio (RF-052, RNF-008).
  Future<void> escolherReceita(OrigemFicheiro origem) async {
    state = state.copiar(aEscolher: true, erros: state.erros);
    try {
      final ficheiro = await ref
          .read(selecionadorFicheirosProvider)
          .escolher(origem);
      if (ref.mounted) {
        state = state.copiar(
          erros: _semReceita(state.erros),
          receita: ficheiro == null ? null : () => ficheiro,
        );
      }
    } on FicheiroRecusadoErro catch (erro) {
      _erroReceita(erro.mensagem);
    } on Object {
      _erroReceita(FicheiroRecusadoErro.origemIndisponivel);
    }
  }

  /// Retira a receita escolhida.
  void retirarReceita() {
    state = state.copiar(erros: _semReceita(state.erros), receita: () => null);
  }

  /// OP-27 ou OP-30: valida e guarda. Devolve o tratamento guardado, ou `null` se não ficou guardado; nesse caso o
  /// formulário mantém os dados e a receita escolhida (RF-053, RNF-028).
  Future<TratamentoDto?> guardar(ValoresTratamento valores) async {
    final original = state.original;
    final erros = ValidadorTratamento.validar(
      medicamento: valores.medicamento,
      dose: valores.dose,
      grau: valores.grau,
      dataInicio: valores.dataInicio,
      dataFim: valores.dataFim,
      horarios: valores.horarios,
      edicao: edicao,
      comDataFim: original?.dataFim != null,
    );
    if (erros.isNotEmpty) {
      state = state.copiar(erros: erros);
      return null;
    }
    final receita = state.receita;
    final dados = original == null
        ? _dadosRegisto(valores)
        : _alteracoes(original, valores);
    if (original != null && dados.vazio && receita == null) {
      return original;
    }
    state = state.copiar(aGuardar: true);
    try {
      final repositorio = ref.read(tratamentosRepositorioProvider);
      final tratamento = original == null
          ? await repositorio.registar(alvo.fichaId, dados, receita: receita)
          : await repositorio.atualizar(original.id, dados, receita: receita);
      if (ref.mounted) {
        ref.invalidate(tratamentosFichaProvider(alvo.fichaId));
        if (original != null) {
          ref
              .read(tratamentoControllerProvider(original.id).notifier)
              .substituir(tratamento);
        }
        state = state.copiar(original: tratamento, receita: () => null);
      }
      return tratamento;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        _erroGuardar(problema);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erro: _traduzir(erro));
      }
    }
    return null;
  }

  DadosTratamentoDto _dadosRegisto(ValoresTratamento valores) {
    final fim = valores.dataFim.trim();
    return DadosTratamentoDto(
      medicamento: valores.medicamento.trim(),
      dose: valores.dose.trim(),
      grauImportancia: valores.grau,
      dataInicio: FormatadorDataHora.paraIsoData(valores.dataInicio),
      dataFim: fim.isEmpty ? null : FormatadorDataHora.paraIsoData(fim),
      horarios: valores.horarios,
    );
  }

  static DadosTratamentoDto _alteracoes(
    TratamentoDto original,
    ValoresTratamento valores,
  ) {
    final medicamento = valores.medicamento.trim();
    final dose = valores.dose.trim();
    final fim = valores.dataFim.trim().isEmpty
        ? null
        : FormatadorDataHora.paraIsoData(valores.dataFim);
    return DadosTratamentoDto(
      medicamento: medicamento == original.medicamento ? null : medicamento,
      dose: dose == original.dose ? null : dose,
      grauImportancia: valores.grau == original.grauImportancia
          ? null
          : valores.grau,
      dataFim: fim == original.dataFim ? null : fim,
      horarios: listEquals(valores.horarios, original.horarios)
          ? null
          : valores.horarios,
    );
  }

  void _erroGuardar(ProblemaApi problema) {
    final porCampo = <String, String>{};
    final semCampo = <String>[];
    for (final erro in problema.campos) {
      // `horarios[1]` aparece junto da lista de horários.
      final campo = erro.campo.startsWith('horarios') ? 'horarios' : erro.campo;
      final rotulo = ValidadorTratamento.rotulos[campo];
      final mensagem = MensagensErro.comRotulo(rotulo ?? campo, erro.regra);
      if (rotulo == null) {
        semCampo.add(mensagem);
      } else {
        porCampo.putIfAbsent(campo, () => mensagem);
      }
    }
    if (_codigosReceita.contains(problema.codigo)) {
      porCampo['receita.ficheiro'] = MensagensErro.comRotulo(
        ValidadorTratamento.rotulos['receita.ficheiro']!,
        MensagensErro.traduzir(problema),
      );
    }
    final String? geral;
    if (semCampo.isNotEmpty) {
      geral = semCampo.join('\n');
    } else if (porCampo.isNotEmpty) {
      geral = null;
    } else if (problema.codigo == 'DOCUMENTO_NAO_GUARDADO') {
      geral = edicao ? receitaNaoGuardadaEdicao : receitaNaoGuardadaRegisto;
    } else {
      geral = _traduzir(problema);
    }
    state = state.copiar(erros: porCampo, erro: geral);
  }

  void _erroReceita(String mensagem) {
    if (ref.mounted) {
      state = state.copiar(
        erros: {
          ...state.erros,
          'receita.ficheiro': MensagensErro.comRotulo(
            ValidadorTratamento.rotulos['receita.ficheiro']!,
            mensagem,
          ),
        },
      );
    }
  }

  static Map<String, String> _semReceita(Map<String, String> erros) =>
      {...erros}..remove('receita.ficheiro');

  static String _traduzir(Object erro) => MensagensErro.traduzir(
    erro,
    especificas: ApresentacaoTratamento.mensagensErro,
  );
}

/// Fornecedor do controlador de E16, por ficha e tratamento.
final formularioTratamentoControllerProvider = NotifierProvider.autoDispose
    .family<
      FormularioTratamentoController,
      FormularioTratamentoEstado,
      AlvoFormulario
    >(FormularioTratamentoController.new);
