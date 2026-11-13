import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/estado_ficha.dart';
import '../../models/ficha_resumo_dto.dart';

/// Estado do ecrã E05 Fichas.
class ListaFichasEstado {
  /// Cria o estado.
  const ListaFichasEstado({
    this.fichas = const [],
    this.filtroEstado = EstadoFicha.ativa,
    this.aCarregar = false,
    this.carregada = false,
    this.erro,
  });

  /// Todas as fichas do Utilizador, ativas e arquivadas (OP-07 sem filtro).
  final List<FichaResumoDto> fichas;

  /// Separador escolhido: Ativas ou Arquivadas.
  final EstadoFicha filtroEstado;

  /// Indica que OP-07 está em curso.
  final bool aCarregar;

  /// Indica que a lista já foi obtida pelo menos uma vez.
  final bool carregada;

  /// Erro ao obter a lista.
  final String? erro;

  /// Fichas do separador escolhido.
  List<FichaResumoDto> get fichasVisiveis =>
      fichas.where((f) => f.estado == filtroEstado).toList(growable: false);

  /// Indica se o Utilizador já tem a sua ficha própria, ativa ou arquivada (RF-031; decisão D5).
  bool get temFichaPropria => fichas.any((f) => f.eMinhaFichaPropria);

  /// Copia o estado, alterando os valores indicados. O erro não indicado é limpo.
  ListaFichasEstado copiar({
    List<FichaResumoDto>? fichas,
    EstadoFicha? filtroEstado,
    bool aCarregar = false,
    bool? carregada,
    String? erro,
  }) {
    return ListaFichasEstado(
      fichas: fichas ?? this.fichas,
      filtroEstado: filtroEstado ?? this.filtroEstado,
      aCarregar: aCarregar,
      carregada: carregada ?? this.carregada,
      erro: erro,
    );
  }
}

/// Controlador do ecrã E05 Fichas (OP-07; RF-028, RF-030, RF-031, RF-120, RF-121). Pede a lista completa uma vez
/// e filtra no dispositivo pelos separadores Ativas e Arquivadas, porque precisa das duas para saber se o
/// Utilizador já tem ficha própria.
class ListaFichasController extends Notifier<ListaFichasEstado> {
  @override
  ListaFichasEstado build() => const ListaFichasEstado();

  /// OP-07: obtém as fichas a que o Utilizador está associado (RNF-005).
  Future<void> carregar() async {
    state = state.copiar(aCarregar: true);
    try {
      final fichas = await ref.read(fichasRepositorioProvider).listar();
      if (ref.mounted) {
        state = state.copiar(fichas: fichas, carregada: true);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = state.copiar(erro: MensagensErro.traduzir(erro));
      }
    }
  }

  /// Muda entre os separadores Ativas e Arquivadas.
  void mudarFiltro(EstadoFicha estado) {
    state = state.copiar(filtroEstado: estado, erro: state.erro);
  }
}

/// Fornecedor do controlador de E05. Não é descartado ao sair do ecrã, porque o ramo Fichas da barra inferior se
/// mantém; os ecrãs que alteram fichas pedem-lhe que volte a carregar.
final listaFichasControllerProvider =
    NotifierProvider<ListaFichasController, ListaFichasEstado>(
      ListaFichasController.new,
    );
