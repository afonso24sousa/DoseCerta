import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/registo_dto.dart';
import '../../models/tipo_registo.dart';

/// Estado do ecrã E23 Ocorrências e notas.
class RegistosEstado {
  /// Cria o estado.
  const RegistosEstado({
    this.separador = TipoRegisto.ocorrencia,
    this.ocorrencias = const [],
    this.notas = const [],
    this.comNotas = false,
    this.aCarregar = false,
    this.carregado = false,
    this.erro,
  });

  /// Separador aberto.
  final TipoRegisto separador;

  /// Ocorrências da ficha, da mais recente para a mais antiga (OP-48).
  final List<RegistoDto> ocorrencias;

  /// Notas partilhadas, da mais recente para a mais antiga (OP-49). Vazia para o Familiar.
  final List<RegistoDto> notas;

  /// Indica se o Utilizador vê as notas, isto é, se é Cuidador da ficha (RF-095).
  final bool comNotas;

  /// Indica que a consulta está em curso.
  final bool aCarregar;

  /// Indica que os registos já foram obtidos pelo menos uma vez.
  final bool carregado;

  /// Erro da última consulta.
  final String? erro;

  /// Registos do separador aberto.
  List<RegistoDto> get registos =>
      separador == TipoRegisto.nota ? notas : ocorrencias;

  /// Copia o estado, alterando os valores indicados. O erro não indicado é limpo.
  RegistosEstado copiar({
    TipoRegisto? separador,
    List<RegistoDto>? ocorrencias,
    List<RegistoDto>? notas,
    bool? comNotas,
    bool aCarregar = false,
    bool? carregado,
    String? erro,
  }) {
    return RegistosEstado(
      separador: separador ?? this.separador,
      ocorrencias: ocorrencias ?? this.ocorrencias,
      notas: notas ?? this.notas,
      comNotas: comNotas ?? this.comNotas,
      aCarregar: aCarregar,
      carregado: carregado ?? this.carregado,
      erro: erro,
    );
  }
}

/// Controlador do ecrã E23 Ocorrências e notas (OP-48 e OP-49; RF-092 a RF-095). Só consulta: os registos não se
/// alteram nem se eliminam (RF-090, RF-091).
class RegistosController extends Notifier<RegistosEstado> {
  /// Cria o controlador da ficha [fichaId].
  RegistosController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Número do último pedido, para ignorar as respostas de pedidos anteriores que cheguem depois.
  int _ultimoPedido = 0;

  @override
  RegistosEstado build() => const RegistosEstado();

  /// Obtém as ocorrências (OP-48) e, se o Utilizador for Cuidador da ficha, as notas (OP-49). O Familiar não
  /// pede as notas, que o servidor lhe recusaria com `403` (RF-095). Chamado ao abrir e ao atualizar o ecrã, para
  /// mostrar os registos feitos entretanto por outros Cuidadores (RF-093). Se houver pedidos sobrepostos (por
  /// exemplo, puxar para atualizar logo depois de voltar do E24), só a resposta do último conta.
  Future<void> carregar() async {
    final pedido = ++_ultimoPedido;
    state = state.copiar(aCarregar: true);
    try {
      final acesso = await ref.read(fichaAcessoProvider(fichaId).future);
      final comNotas = acesso.ficha.souCuidador;
      final repositorio = ref.read(registosRepositorioProvider);
      final listas = await Future.wait([
        repositorio.listarOcorrencias(fichaId),
        if (comNotas) repositorio.listarNotas(fichaId),
      ]);
      if (ref.mounted && pedido == _ultimoPedido) {
        state = state.copiar(
          ocorrencias: listas.first,
          notas: comNotas ? listas.last : const [],
          comNotas: comNotas,
          carregado: true,
          separador: comNotas ? null : TipoRegisto.ocorrencia,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted && pedido == _ultimoPedido) {
        state = state.copiar(erro: MensagensErro.traduzir(erro));
      }
    }
  }

  /// Abre o separador [tipo]. As notas só existem para o Cuidador (RF-095).
  void mudarSeparador(TipoRegisto tipo) {
    if (tipo == TipoRegisto.nota && !state.comNotas) {
      return;
    }
    state = state.copiar(
      separador: tipo,
      aCarregar: state.aCarregar,
      erro: state.erro,
    );
  }
}

/// Fornecedor do controlador de E23, por ficha.
final registosControllerProvider = NotifierProvider.autoDispose
    .family<RegistosController, RegistosEstado, String>(RegistosController.new);
