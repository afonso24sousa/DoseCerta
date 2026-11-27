import 'estado_toma.dart';
import 'tipo_item_agenda.dart';

/// Evento da agenda, com o nome do utente (OP-45; RF-089).
class ItemAgendaDto {
  /// Cria o evento.
  const ItemAgendaDto({
    required this.id,
    required this.tipo,
    required this.fichaId,
    required this.nomeFicha,
    required this.titulo,
    required this.inicio,
    this.estado,
    this.confirmavel = false,
  });

  /// Lê um evento da API.
  factory ItemAgendaDto.fromJson(Map<String, dynamic> json) {
    final estado = json['estado'];
    return ItemAgendaDto(
      id: json['id'] as String,
      tipo: TipoItemAgenda.fromJson(json['tipo']),
      fichaId: json['fichaId'] as String,
      nomeFicha: json['nomeFicha'] as String,
      titulo: json['titulo'] as String,
      inicio: json['inicio'] as String,
      estado: estado == null ? null : EstadoToma.fromJson(estado),
      confirmavel: json['confirmavel'] as bool? ?? false,
    );
  }

  /// Identificador da toma ou do compromisso.
  final String id;

  /// Toma ou compromisso.
  final TipoItemAgenda tipo;

  /// Identificador da ficha.
  final String fichaId;

  /// Nome do utente da ficha (RF-089).
  final String nomeFicha;

  /// «Medicamento Dose» nas tomas; o motivo nos compromissos.
  final String titulo;

  /// Instante, em ISO 8601 com o desvio de Lisboa.
  final String inicio;

  /// Estado da toma; não existe nos compromissos.
  final EstadoToma? estado;

  /// Indica se a toma pode ser confirmada pelo Utilizador; sempre `false` nos compromissos (RF-087).
  final bool confirmavel;
}
