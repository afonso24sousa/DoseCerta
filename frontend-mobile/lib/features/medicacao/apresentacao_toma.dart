import 'package:flutter/material.dart';

import '../../core/formatador_data_hora.dart';
import '../../models/estado_toma.dart';
import '../../models/toma_dto.dart';

/// Texto e ícone de cada estado da toma e textos da toma no calendário (contrato de interface da F006, secção 5;
/// RF-063, RF-065, RNF-027, RNF-029). O estado é sempre identificado por texto e ícone, nunca só pela cor.
class ApresentacaoToma {
  const ApresentacaoToma._();

  /// Texto do [estado].
  static String texto(EstadoToma estado) => switch (estado) {
    EstadoToma.pendente => 'Pendente',
    EstadoToma.confirmada => 'Confirmada',
    EstadoToma.confirmadaComAtraso => 'Confirmada com atraso',
    EstadoToma.naoConfirmada => 'Não confirmada',
  };

  /// Ícone do [estado].
  static IconData icone(EstadoToma estado) => switch (estado) {
    EstadoToma.pendente => Icons.schedule,
    EstadoToma.confirmada => Icons.check_circle,
    EstadoToma.confirmadaComAtraso => Icons.history,
    EstadoToma.naoConfirmada => Icons.cancel,
  };

  /// Quem confirmou e quando, nas tomas confirmadas (também com atraso): `Confirmada por {nome} em dd/mm/aaaa às
  /// hh:mm`; `null` nas restantes.
  static String? confirmacao(TomaDto toma) {
    final pessoa = toma.confirmadaPor;
    final instante = toma.confirmadaEm;
    if (pessoa == null || instante == null) {
      return null;
    }
    return 'Confirmada por ${pessoa.nome} em '
        '${FormatadorDataHora.apresentarData(instante)} às '
        '${FormatadorDataHora.apresentarHora(instante)}';
  }

  /// Mensagem depois de confirmar, conforme o estado devolvido pela OP-33 (RF-064, RF-067).
  static String mensagemConfirmada(EstadoToma estado) =>
      estado == EstadoToma.confirmadaComAtraso
      ? 'Toma confirmada com atraso.'
      : 'Toma confirmada.';
}
