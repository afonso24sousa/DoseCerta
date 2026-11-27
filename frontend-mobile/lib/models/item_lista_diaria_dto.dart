import 'estado_toma.dart';
import 'tipo_item_agenda.dart';

/// Item da lista de tarefas diária: uma toma ou um compromisso, com o nome do utente (OP-50; RF-100, RF-101).
class ItemListaDiariaDto {
  /// Cria o item.
  const ItemListaDiariaDto({
    required this.tipo,
    required this.id,
    required this.fichaId,
    required this.nomeFicha,
    required this.titulo,
    required this.previstaEm,
    required this.confirmavel,
    this.estado,
    this.confirmadaPor,
    this.confirmadaEm,
  });

  /// Lê um item da API.
  factory ItemListaDiariaDto.fromJson(Map<String, dynamic> json) {
    final estado = json['estado'];
    return ItemListaDiariaDto(
      tipo: TipoItemAgenda.fromJson(json['tipo']),
      id: json['id'] as String,
      fichaId: json['fichaId'] as String,
      nomeFicha: json['nomeFicha'] as String,
      titulo: json['titulo'] as String,
      previstaEm: json['previstaEm'] as String,
      estado: estado == null ? null : EstadoToma.fromJson(estado),
      confirmadaPor: json['confirmadaPor'] as String?,
      confirmadaEm: json['confirmadaEm'] as String?,
      confirmavel: json['confirmavel'] as bool? ?? false,
    );
  }

  /// Toma ou compromisso.
  final TipoItemAgenda tipo;

  /// Identificador da toma ou do compromisso.
  final String id;

  /// Identificador da ficha.
  final String fichaId;

  /// Nome do utente da ficha (RF-101).
  final String nomeFicha;

  /// «Medicamento Dose» nas tomas; o motivo nos compromissos.
  final String titulo;

  /// Instante previsto, em ISO 8601 com o desvio de Lisboa.
  final String previstaEm;

  /// Estado da toma; `null` nos compromissos.
  final EstadoToma? estado;

  /// Nome de quem confirmou, nas tomas confirmadas (RF-104). Na OP-50 é só o nome, não um objeto.
  final String? confirmadaPor;

  /// Momento da confirmação, nas tomas confirmadas.
  final String? confirmadaEm;

  /// Indica se o servidor aceita agora a confirmação: só tomas `Pendente` com a janela aberta (RF-064, RF-068). Os
  /// compromissos nunca são confirmáveis (RF-087).
  final bool confirmavel;

  /// Indica se é uma toma.
  bool get eToma => tipo == TipoItemAgenda.toma;

  /// Devolve o item com o novo [estado] e a confirmação, depois de confirmado (OP-33), já não confirmável.
  ItemListaDiariaDto confirmado({
    required EstadoToma estado,
    required String? confirmadaPor,
    required String? confirmadaEm,
  }) {
    return ItemListaDiariaDto(
      tipo: tipo,
      id: id,
      fichaId: fichaId,
      nomeFicha: nomeFicha,
      titulo: titulo,
      previstaEm: previstaEm,
      estado: estado,
      confirmadaPor: confirmadaPor,
      confirmadaEm: confirmadaEm,
      confirmavel: false,
    );
  }
}
