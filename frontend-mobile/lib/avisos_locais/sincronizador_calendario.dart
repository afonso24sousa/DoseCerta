import '../core/relogio.dart';
import '../core/sem_ligacao_erro.dart';
import '../data/calendario_repositorio.dart';
import 'avisos_locais_service.dart';
import 'tomas_sincronizadas_store.dart';

/// Resultado de uma sincronização.
enum ResultadoSincronizacao {
  /// As tomas foram recebidas, guardadas e agendadas.
  sucesso,

  /// Sem ligação: ficam as tomas e os avisos da última sincronização (RF-061, CA3).
  semLigacao,

  /// O servidor recusou o pedido (por exemplo, sessão expirada); ficam as tomas e os avisos anteriores.
  falhou,
}

/// Caso de uso da sincronização (modelo de classes, secção 7; RF-061, RF-062): pede ao servidor as tomas `Pendente`
/// das 48 horas seguintes das fichas ativas em que o Utilizador é Cuidador (OP-34), guarda-as no dispositivo e
/// volta a agendar os avisos.
class SincronizadorCalendario {
  /// Cria o sincronizador.
  const SincronizadorCalendario({
    required this.repositorio,
    required this.store,
    required this.avisos,
    required this.relogio,
  });

  /// Repositório da OP-34.
  final CalendarioRepositorio repositorio;

  /// Tomas guardadas no dispositivo.
  final TomasSincronizadasStore store;

  /// Avisos do sistema operativo.
  final AvisosLocaisService avisos;

  /// Relógio, para o instante da sincronização.
  final Relogio relogio;

  /// Sincroniza as tomas do Utilizador [utilizadorId]. Se as tomas guardadas forem de outro Utilizador (outra
  /// pessoa iniciou sessão no mesmo telemóvel), os avisos dele são cancelados antes, mesmo sem ligação.
  Future<ResultadoSincronizacao> sincronizar(String utilizadorId) async {
    try {
      final anteriores = await store.ler();
      if (anteriores != null && anteriores.utilizadorId != utilizadorId) {
        await terminarSessao();
      }
      final tomas = await repositorio.sincronizar();
      await store.guardar(utilizadorId, tomas, relogio.agora());
      await avisos.reconciliar(tomas);
      return ResultadoSincronizacao.sucesso;
    } on SemLigacaoErro {
      return ResultadoSincronizacao.semLigacao;
    } on Object {
      return ResultadoSincronizacao.falhou;
    }
  }

  /// Ao terminar a sessão: cancela todos os avisos e apaga as tomas guardadas (critério da I069).
  Future<void> terminarSessao() async {
    await avisos.cancelarTodos();
    await store.limpar();
  }
}
