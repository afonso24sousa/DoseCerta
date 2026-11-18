import 'aviso_local.dart';

/// Agenda e cancela avisos no sistema operativo (modelo de classes, secção 7). É uma interface para os testes e os
/// cenários de sistema (I070) poderem usar um agendador falso, pelo provedor.
abstract interface class AgendadorAvisos {
  /// Agenda o [aviso] para a hora prevista.
  Future<void> agendar(AvisoLocal aviso);

  /// Cancela o aviso [idAviso], se existir.
  Future<void> cancelar(int idAviso);

  /// Cancela todos os avisos agendados pela aplicação.
  Future<void> cancelarTodos();
}
