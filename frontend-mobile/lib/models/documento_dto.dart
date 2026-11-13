import 'categoria_documento.dart';

/// Metadados de um documento da ficha (OP-15 e OP-16).
class DocumentoDto {
  /// Cria os metadados.
  const DocumentoDto({
    required this.id,
    required this.categoria,
    required this.nomeOriginal,
    required this.formato,
    required this.tamanhoBytes,
    required this.carregadoEm,
  });

  /// Lê um documento da API.
  factory DocumentoDto.fromJson(Map<String, dynamic> json) {
    return DocumentoDto(
      id: json['id'] as String,
      categoria: CategoriaDocumento.fromJson(json['categoria']),
      nomeOriginal: json['nomeOriginal'] as String,
      formato: json['formato'] as String,
      tamanhoBytes: json['tamanhoBytes'] as int,
      carregadoEm: json['carregadoEm'] as String,
    );
  }

  /// Identificador do documento.
  final String id;

  /// Categoria do documento.
  final CategoriaDocumento categoria;

  /// Nome do ficheiro carregado.
  final String nomeOriginal;

  /// Formato (`PDF`, `JPG` ou `PNG`).
  final String formato;

  /// Tamanho em bytes.
  final int tamanhoBytes;

  /// Instante do carregamento (ISO 8601).
  final String carregadoEm;
}
