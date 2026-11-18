import '../models/toma_calendario_dto.dart';

/// Aviso local de uma toma sincronizada (modelo de classes, secção 7; RF-062): a ficha, o medicamento e a dose, à
/// hora prevista.
class AvisoLocal {
  /// Cria o aviso.
  const AvisoLocal({
    required this.idAviso,
    required this.tomaId,
    required this.fichaId,
    required this.titulo,
    required this.corpo,
    required this.previstaEm,
  });

  /// Cria o aviso da [toma] (OP-34). O título e o corpo seguem o acordado com a I068 e a I070: `Toma de {ficha}` e
  /// `{medicamento} {dose} às hh:mm`, com a hora de Lisboa tal como vem da API (RNF-029).
  factory AvisoLocal.daToma(TomaCalendarioDto toma) {
    return AvisoLocal(
      idAviso: idDaToma(toma.tomaId),
      tomaId: toma.tomaId,
      fichaId: toma.fichaId,
      titulo: 'Toma de ${toma.nomeFicha}',
      corpo:
          '${toma.medicamento} ${toma.dose} às ${toma.previstaEm.substring(11, 16)}',
      previstaEm: DateTime.parse(toma.previstaEm),
    );
  }

  /// Identificador do aviso no sistema operativo, que tem de ser um inteiro. É calculado a partir do `tomaId`
  /// (FNV-1a de 31 bits), por isso a mesma toma tem sempre o mesmo aviso e pode ser cancelada sem guardar uma
  /// tabela de correspondências.
  static int idDaToma(String tomaId) {
    var hash = 0x811c9dc5;
    for (final unidade in tomaId.codeUnits) {
      hash ^= unidade;
      hash = (hash * 0x01000193) & 0xFFFFFFFF;
    }
    return hash & 0x7FFFFFFF;
  }

  /// Identificador do aviso no sistema operativo.
  final int idAviso;

  /// Toma avisada.
  final String tomaId;

  /// Ficha da toma.
  final String fichaId;

  /// Título do aviso.
  final String titulo;

  /// Texto do aviso.
  final String corpo;

  /// Instante previsto da toma (o aviso dispara a esta hora).
  final DateTime previstaEm;
}
