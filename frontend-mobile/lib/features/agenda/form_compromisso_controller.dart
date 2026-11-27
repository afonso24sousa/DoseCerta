import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/compromisso_dto.dart';
import '../../models/dados_compromisso_dto.dart';
import '../../models/problema_api.dart';
import 'agenda_controller.dart';

/// Ficha e, na edição, o compromisso de E22.
typedef AlvoCompromisso = ({String fichaId, String? compromissoId});

/// Valores do formulário de E22, como o Utilizador os escreveu.
typedef ValoresCompromisso = ({
  String data,
  String hora,
  String local,
  String motivo,
  int antecedencia,
});

/// Estado do ecrã E22 Registar ou editar compromisso.
class FormCompromissoEstado {
  /// Cria o estado.
  const FormCompromissoEstado({
    this.original,
    this.aCarregar = false,
    this.erroCarregar,
    this.aGuardar = false,
    this.erros = const {},
    this.erro,
  });

  /// Compromisso a editar; `null` no registo.
  final CompromissoDto? original;

  /// Indica que OP-42 está em curso.
  final bool aCarregar;

  /// Erro ao obter o compromisso.
  final String? erroCarregar;

  /// Indica que OP-41, OP-43 ou OP-44 está em curso.
  final bool aGuardar;

  /// Erros por campo (`data`, `hora`, `local`, `motivo`, `antecedenciaAvisoMinutos`).
  final Map<String, String> erros;

  /// Erro geral.
  final String? erro;

  /// Indica se alguma ação está em curso.
  bool get ocupado => aCarregar || aGuardar;
}

/// Controlador de E22 (OP-41 a OP-44; RF-080 a RF-082, RF-116, RNF-028). A edição envia todos os campos (OP-43).
class FormCompromissoController extends Notifier<FormCompromissoEstado> {
  /// Cria o controlador para o [alvo].
  FormCompromissoController(this.alvo);

  /// Ficha e compromisso (na edição).
  final AlvoCompromisso alvo;

  /// Máximo de caracteres do local (RF-080).
  static const int maxLocal = 150;

  /// Máximo de caracteres do motivo (RF-080).
  static const int maxMotivo = 250;

  /// Rótulos dos campos, também para os erros devolvidos pela API.
  static const Map<String, String> rotulos = {
    'data': 'Data',
    'hora': 'Hora',
    'local': 'Local',
    'motivo': 'Motivo',
    'antecedenciaAvisoMinutos': 'Antecedência do aviso',
  };

  static const Map<String, String> _mensagens = {
    'RECURSO_NAO_ENCONTRADO': 'O compromisso já não existe.',
  };

  static final RegExp _hora = RegExp(r'^([01]?\d|2[0-3]):([0-5]\d)$');

  @override
  FormCompromissoEstado build() => const FormCompromissoEstado();

  /// OP-42: na edição, obtém o compromisso a partir da lista da ficha (a API não tem consulta individual).
  Future<void> carregar() async {
    final id = alvo.compromissoId;
    if (id == null) {
      return;
    }
    state = const FormCompromissoEstado(aCarregar: true);
    try {
      final lista = await ref
          .read(compromissosRepositorioProvider)
          .listar(alvo.fichaId);
      final original = lista.where((c) => c.id == id).firstOrNull;
      if (ref.mounted) {
        state = original == null
            ? FormCompromissoEstado(
                erroCarregar: _mensagens['RECURSO_NAO_ENCONTRADO'],
              )
            : FormCompromissoEstado(original: original);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = FormCompromissoEstado(erroCarregar: _traduzir(erro));
      }
    }
  }

  /// Valida os [valores]; devolve os erros por campo (vazio se estiverem certos).
  static Map<String, String> validar(ValoresCompromisso valores) {
    final erros = <String, String>{};
    if (FormatadorDataHora.lerData(valores.data) == null) {
      erros['data'] = 'Data: indique uma data válida (dd/mm/aaaa).';
    }
    if (!_hora.hasMatch(valores.hora.trim())) {
      erros['hora'] = 'Hora: indique a hora no formato hh:mm.';
    }
    void texto(String campo, String valor, int maximo) {
      final limpo = valor.trim();
      if (limpo.isEmpty) {
        erros[campo] = '${rotulos[campo]}: é obrigatório.';
      } else if (limpo.length > maximo) {
        erros[campo] = '${rotulos[campo]}: tem no máximo $maximo caracteres.';
      }
    }

    texto('local', valores.local, maxLocal);
    texto('motivo', valores.motivo, maxMotivo);
    return erros;
  }

  /// Converte os [valores] (já validados) no corpo de OP-41 e OP-43, com a hora de Lisboa.
  static DadosCompromissoDto dados(ValoresCompromisso valores) {
    final hora = _hora.firstMatch(valores.hora.trim())!;
    return DadosCompromissoDto(
      dataHora: FormatadorDataHora.instanteDeLisboa(
        FormatadorDataHora.lerData(valores.data)!,
        int.parse(hora.group(1)!),
        int.parse(hora.group(2)!),
      ),
      local: valores.local.trim(),
      motivo: valores.motivo.trim(),
      antecedenciaAvisoMinutos: valores.antecedencia,
    );
  }

  /// OP-41 ou OP-43: valida e guarda. Devolve `true` se ficou guardado; caso contrário o formulário mantém os
  /// dados e mostra os erros (RNF-028).
  Future<bool> guardar(ValoresCompromisso valores) async {
    final erros = validar(valores);
    if (erros.isNotEmpty) {
      state = FormCompromissoEstado(original: state.original, erros: erros);
      return false;
    }
    return _executar(() async {
      final repositorio = ref.read(compromissosRepositorioProvider);
      final id = alvo.compromissoId;
      if (id == null) {
        await repositorio.registar(alvo.fichaId, dados(valores));
      } else {
        await repositorio.editar(id, dados(valores));
      }
    });
  }

  /// OP-44: remove o compromisso; devolve `true` se ficou removido (RF-082).
  Future<bool> remover() => _executar(
    () =>
        ref.read(compromissosRepositorioProvider).remover(alvo.compromissoId!),
  );

  Future<bool> _executar(Future<void> Function() pedido) async {
    final original = state.original;
    state = FormCompromissoEstado(original: original, aGuardar: true);
    try {
      await pedido();
      if (ref.mounted) {
        ref.invalidate(itensAgendaProvider);
        state = FormCompromissoEstado(original: original);
      }
      return true;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        final porCampo = <String, String>{};
        final semCampo = <String>[];
        for (final erro in problema.campos) {
          // `dataHora` aparece junto da data.
          final campo = erro.campo == 'dataHora' ? 'data' : erro.campo;
          final rotulo = rotulos[campo];
          final mensagem = MensagensErro.comRotulo(rotulo ?? campo, erro.regra);
          if (rotulo == null) {
            semCampo.add(mensagem);
          } else {
            porCampo.putIfAbsent(campo, () => mensagem);
          }
        }
        state = FormCompromissoEstado(
          original: original,
          erros: porCampo,
          erro: semCampo.isNotEmpty
              ? semCampo.join('\n')
              : porCampo.isEmpty
              ? _traduzir(problema)
              : null,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = FormCompromissoEstado(
          original: original,
          erro: _traduzir(erro),
        );
      }
    }
    return false;
  }

  static String _traduzir(Object erro) =>
      MensagensErro.traduzir(erro, especificas: _mensagens);
}

/// Fornecedor do controlador de E22, por ficha e compromisso.
final formCompromissoControllerProvider = NotifierProvider.autoDispose
    .family<FormCompromissoController, FormCompromissoEstado, AlvoCompromisso>(
      FormCompromissoController.new,
    );
