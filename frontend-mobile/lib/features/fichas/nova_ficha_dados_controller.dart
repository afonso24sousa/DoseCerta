import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/dominios_necessidades_dto.dart';
import '../../models/ficha_resumo_dto.dart';
import '../../shared/nova_ficha_rascunho.dart';
import '../../shared/validador_ficha.dart';
import 'formulario_ficha.dart';

/// Estado do ecrã E06 Nova ficha: dados e necessidades.
class NovaFichaDadosEstado {
  /// Cria o estado.
  const NovaFichaDadosEstado({
    this.dominios,
    this.aCarregar = false,
    this.erroCarregar,
    this.propria = false,
    this.temFichaPropria = false,
    this.erros = const {},
  });

  /// Listas fechadas (OP-08).
  final DominiosNecessidadesDto? dominios;

  /// Indica que OP-08 e OP-07 estão em curso.
  final bool aCarregar;

  /// Erro ao obter as listas.
  final String? erroCarregar;

  /// Escolha «Gerir a minha própria saúde» (RF-030).
  final bool propria;

  /// Indica que o Utilizador já tem ficha própria: a opção fica indisponível (RF-031; decisão D5).
  final bool temFichaPropria;

  /// Erros da validação prévia, por campo da API.
  final Map<String, String> erros;

  /// Copia o estado, alterando os valores indicados. Os erros não indicados são limpos.
  NovaFichaDadosEstado copiar({
    DominiosNecessidadesDto? dominios,
    bool aCarregar = false,
    String? erroCarregar,
    bool? propria,
    bool? temFichaPropria,
    Map<String, String> erros = const {},
  }) {
    return NovaFichaDadosEstado(
      dominios: dominios ?? this.dominios,
      aCarregar: aCarregar,
      erroCarregar: erroCarregar,
      propria: propria ?? this.propria,
      temFichaPropria: temFichaPropria ?? this.temFichaPropria,
      erros: erros,
    );
  }
}

/// Controlador do ecrã E06 (OP-08; RF-020, RF-021, RF-030, RF-031, RNF-028, RNF-029). Para saber se a opção da
/// ficha própria fica disponível, pede também a lista completa das fichas (OP-07, decisão D5).
class NovaFichaDadosController extends Notifier<NovaFichaDadosEstado> {
  @override
  NovaFichaDadosEstado build() => const NovaFichaDadosEstado();

  /// OP-08 e OP-07: obtém as listas fechadas e verifica se o Utilizador já tem ficha própria.
  Future<void> carregarDominios() async {
    state = state.copiar(aCarregar: true, erros: state.erros);
    try {
      final pedidos = await Future.wait<Object>([
        ref.read(dominiosRepositorioProvider).obterNecessidades(),
        ref.read(fichasRepositorioProvider).listar(),
      ]);
      final dominios = pedidos[0] as DominiosNecessidadesDto;
      final fichas = pedidos[1] as List<FichaResumoDto>;
      final temPropria = fichas.any((f) => f.eMinhaFichaPropria);
      if (ref.mounted) {
        state = state.copiar(
          dominios: dominios,
          temFichaPropria: temPropria,
          propria: state.propria && !temPropria,
          erros: state.erros,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erroCarregar: MensagensErro.traduzir(erro));
      }
    }
  }

  /// Escolhe entre a ficha de uma terceira pessoa e a ficha própria.
  void escolherPropria({required bool propria}) {
    if (propria && state.temFichaPropria) {
      return;
    }
    state = state.copiar(propria: propria, erros: state.erros);
  }

  /// Valida o [formulario] e, se estiver certo, guarda-o no rascunho para E07. Devolve `true` se pode avançar.
  bool seguinte(FormularioFicha formulario) {
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
    state = state.copiar();
    ref
        .read(novaFichaRascunhoProvider.notifier)
        .guardar(formulario.dados(propria: state.propria));
    return true;
  }
}

/// Fornecedor do controlador de E06.
final novaFichaDadosControllerProvider =
    NotifierProvider.autoDispose<
      NovaFichaDadosController,
      NovaFichaDadosEstado
    >(NovaFichaDadosController.new);
