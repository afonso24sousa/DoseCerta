import 'package:dosecerta/avisos_locais/agendador_avisos.dart';
import 'package:dosecerta/avisos_locais/aviso_local.dart';
import 'package:dosecerta/avisos_locais/permissao_notificacoes.dart';
import 'package:dosecerta/avisos_locais/tomas_sincronizadas_store.dart';
import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/calendario_repositorio.dart';
import 'package:dosecerta/data/tomas_repositorio.dart';
import 'package:dosecerta/models/estado_toma.dart';
import 'package:dosecerta/models/grau_importancia.dart';
import 'package:dosecerta/models/pessoa_ref_dto.dart';
import 'package:dosecerta/models/toma_calendario_dto.dart';
import 'package:dosecerta/models/toma_dto.dart';

/// Toma de exemplo (dados fictícios da documentação da API, OP-32).
TomaDto tomaExemplo({
  String id = 't-1',
  String medicamento = 'Metformina',
  String dose = '500 mg',
  GrauImportancia grau = GrauImportancia.moderado,
  String previstaEm = '2026-11-09T08:00:00+00:00',
  EstadoToma estado = EstadoToma.pendente,
  PessoaRefDto? confirmadaPor,
  String? confirmadaEm,
}) {
  return TomaDto(
    id: id,
    tratamentoId: 'tr-1',
    medicamento: medicamento,
    dose: dose,
    grauImportancia: grau,
    previstaEm: previstaEm,
    estado: estado,
    confirmadaPor: confirmadaPor,
    confirmadaEm: confirmadaEm,
  );
}

/// Toma sincronizada de exemplo (OP-34).
TomaCalendarioDto tomaSincronizadaExemplo({
  String tomaId = 't-1',
  String previstaEm = '2026-11-09T20:00:00+00:00',
}) {
  return TomaCalendarioDto(
    tomaId: tomaId,
    fichaId: 'f-1',
    nomeFicha: 'Maria Fictícia Santos',
    medicamento: 'Metformina',
    dose: '500 mg',
    previstaEm: previstaEm,
  );
}

/// Repositório de tomas falso: guarda os pedidos e devolve ou lança o que o teste indicar.
class TomasRepositorioFalso implements TomasRepositorio {
  /// Tomas devolvidas pela OP-32.
  List<TomaDto> tomas = [];

  /// Erro da OP-32.
  Object? erroListar;

  /// Erro da OP-33.
  Object? erroConfirmar;

  /// Se indicado, a OP-33 só responde quando este futuro terminar.
  Future<void>? espera;

  /// Estado devolvido pela OP-33.
  EstadoToma estadoConfirmado = EstadoToma.confirmada;

  /// Períodos pedidos, como `de..ate`.
  final List<String> periodos = [];

  /// Tomas confirmadas.
  final List<String> confirmadas = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<TomaDto>> listar(String fichaId, String de, String ate) async {
    periodos.add('$de..$ate');
    final erro = erroListar;
    if (erro != null) {
      throw erro;
    }
    return List.of(tomas);
  }

  @override
  Future<TomaDto> confirmar(String tomaId) async {
    confirmadas.add(tomaId);
    await espera;
    final erro = erroConfirmar;
    if (erro != null) {
      throw erro;
    }
    final toma = tomas.firstWhere((t) => t.id == tomaId);
    return TomaDto(
      id: toma.id,
      tratamentoId: toma.tratamentoId,
      medicamento: toma.medicamento,
      dose: toma.dose,
      grauImportancia: toma.grauImportancia,
      previstaEm: toma.previstaEm,
      estado: estadoConfirmado,
      confirmadaPor: const PessoaRefDto(id: 'u-eu', nome: 'Ana Fictícia'),
      confirmadaEm: '2026-11-09T12:00:00+00:00',
    );
  }
}

/// Repositório da OP-34 falso.
class CalendarioRepositorioFalso implements CalendarioRepositorio {
  /// Tomas devolvidas.
  List<TomaCalendarioDto> tomas = [];

  /// Erro a lançar.
  Object? erro;

  /// Número de pedidos.
  int pedidos = 0;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<TomaCalendarioDto>> sincronizar() async {
    pedidos++;
    final e = erro;
    if (e != null) {
      throw e;
    }
    return List.of(tomas);
  }
}

/// Agendador falso: guarda os avisos agendados como o sistema operativo os guardaria.
class AgendadorAvisosFalso implements AgendadorAvisos {
  /// Avisos agendados, por identificador.
  final Map<int, AvisoLocal> agendados = {};

  /// Avisos cancelados um a um.
  final List<int> cancelados = [];

  /// Número de vezes que todos os avisos foram cancelados.
  int cancelamentosTotais = 0;

  @override
  Future<void> agendar(AvisoLocal aviso) async =>
      agendados[aviso.idAviso] = aviso;

  @override
  Future<void> cancelar(int idAviso) async {
    cancelados.add(idAviso);
    agendados.remove(idAviso);
  }

  @override
  Future<void> cancelarTodos() async {
    cancelamentosTotais++;
    agendados.clear();
  }
}

/// Autorizações falsas.
class PermissaoNotificacoesFalsa implements PermissaoNotificacoes {
  /// Notificações autorizadas.
  bool? notificacoes = true;

  /// Alarmes exatos autorizados.
  bool? alarmesExatos = true;

  /// Resposta ao pedido das notificações.
  bool respostaAoPedido = true;

  /// Pedidos das notificações.
  int pedidos = 0;

  /// Pedidos dos alarmes exatos.
  int pedidosAlarmes = 0;

  /// Se existir, a leitura do estado das notificações só termina quando este futuro terminar.
  Future<void>? esperaEstado;

  @override
  Future<bool?> estado() async {
    await esperaEstado;
    return notificacoes;
  }

  @override
  Future<bool?> pedir() async {
    pedidos++;
    notificacoes = respostaAoPedido;
    return notificacoes;
  }

  @override
  Future<bool?> estadoAlarmesExatos() async => alarmesExatos;

  @override
  Future<bool?> pedirAlarmesExatos() async {
    pedidosAlarmes++;
    alarmesExatos = true;
    return true;
  }
}

/// Tomas sincronizadas em memória.
class TomasSincronizadasStoreEmMemoria implements TomasSincronizadasStore {
  /// Última sincronização guardada.
  TomasSincronizadas? guardadas;

  @override
  Future<TomasSincronizadas?> ler() async => guardadas;

  @override
  Future<void> guardar(
    String utilizadorId,
    List<TomaCalendarioDto> tomas,
    DateTime instante,
  ) async {
    guardadas = TomasSincronizadas(
      utilizadorId: utilizadorId,
      tomas: tomas,
      ultimaSincronizacao: instante,
    );
  }

  @override
  Future<void> limpar() async => guardadas = null;
}
