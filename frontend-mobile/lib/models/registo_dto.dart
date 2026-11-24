import 'pessoa_ref_dto.dart';
import 'tipo_registo.dart';

/// Ocorrência ou nota partilhada de uma ficha (OP-46 a OP-49). Não tem alteração nem eliminação (RF-090, RF-091).
class RegistoDto {
  /// Cria o registo.
  const RegistoDto({
    required this.id,
    required this.fichaId,
    required this.tipo,
    required this.descricao,
    required this.autor,
    required this.criadaEm,
    this.ocorridaEm,
  });

  /// Lê um registo da API.
  factory RegistoDto.fromJson(Map<String, dynamic> json) {
    return RegistoDto(
      id: json['id'] as String,
      fichaId: json['fichaId'] as String,
      tipo: TipoRegisto.fromJson(json['tipo']),
      descricao: json['descricao'] as String,
      autor: PessoaRefDto.fromJson(json['autor'] as Map<String, dynamic>),
      ocorridaEm: json['ocorridaEm'] as String?,
      criadaEm: json['criadaEm'] as String,
    );
  }

  /// Identificador do registo.
  final String id;

  /// Identificador da ficha.
  final String fichaId;

  /// Ocorrência ou nota.
  final TipoRegisto tipo;

  /// Texto do registo.
  final String descricao;

  /// Quem registou, com o nome atual da conta (RF-092).
  final PessoaRefDto autor;

  /// Data e hora em que a ocorrência aconteceu, em ISO 8601 com o desvio; `null` nas notas (RF-090).
  final String? ocorridaEm;

  /// Momento do registo, em ISO 8601 com o desvio (RF-092).
  final String criadaEm;
}
