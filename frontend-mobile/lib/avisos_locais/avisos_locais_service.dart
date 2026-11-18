import 'package:flutter/foundation.dart';

import '../core/relogio.dart';
import '../models/toma_calendario_dto.dart';
import 'agendador_avisos.dart';
import 'aviso_local.dart';

/// Mantém os avisos do sistema operativo iguais às tomas sincronizadas (modelo de classes, secção 7; UC-21).
///
/// A reconciliação cancela todos os avisos e volta a agendar um por cada toma ainda por vir (acordado com a I068 e
/// a I070). Assim, uma toma confirmada, de uma ficha arquivada (RF-069) ou de uma ficha onde o Utilizador passou a
/// Familiar (RF-061), que a OP-34 já não devolve, deixa de ter aviso sem regras no dispositivo.
class AvisosLocaisService {
  /// Cria o serviço sobre o [agendador].
  const AvisosLocaisService(this.agendador, this.relogio);

  /// Agendador do sistema operativo.
  final AgendadorAvisos agendador;

  /// Relógio, para não agendar tomas cuja hora já passou.
  final Relogio relogio;

  /// Substitui os avisos agendados pelos das [tomas] e devolve os avisos agendados. Cada agendamento fica no
  /// registo da aplicação (`flutter logs`), com a hora a que foi feito, para a evidência da RNF-041.
  Future<List<AvisoLocal>> reconciliar(List<TomaCalendarioDto> tomas) async {
    await agendador.cancelarTodos();
    final agora = relogio.agora();
    final agendados = <AvisoLocal>[];
    for (final toma in tomas) {
      final aviso = AvisoLocal.daToma(toma);
      if (!aviso.previstaEm.isAfter(agora)) {
        continue;
      }
      await agendador.agendar(aviso);
      agendados.add(aviso);
      debugPrint(
        '[avisos] ${agora.toIso8601String()} agendado ${aviso.idAviso} '
        '(toma ${aviso.tomaId}) para ${toma.previstaEm}: ${aviso.titulo} | ${aviso.corpo}',
      );
    }
    return agendados;
  }

  /// Cancela o aviso da toma [tomaId] (por exemplo, depois de a confirmar).
  Future<void> cancelarAviso(String tomaId) async {
    await agendador.cancelar(AvisoLocal.idDaToma(tomaId));
    debugPrint(
      '[avisos] ${relogio.agora().toIso8601String()} cancelado o aviso da toma $tomaId',
    );
  }

  /// Cancela todos os avisos (ao terminar a sessão).
  Future<void> cancelarTodos() async {
    await agendador.cancelarTodos();
    debugPrint(
      '[avisos] ${relogio.agora().toIso8601String()} cancelados todos os avisos',
    );
  }
}
