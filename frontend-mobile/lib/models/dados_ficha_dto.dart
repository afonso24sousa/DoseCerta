import 'definicao_principal_dto.dart';
import 'ficha_dto.dart';

/// Dados do formulário de ficha, usados para criar (OP-09) e editar (OP-11).
///
/// Divergência do modelo (D1 da I060): o corpo de OP-11 não tem `propria` nem `cuidadorPrincipal`, por isso há
/// dois `toJson`, um para cada operação.
class DadosFichaDto {
  /// Cria os dados.
  const DadosFichaDto({
    required this.propria,
    required this.nomeCompleto,
    required this.dataNascimento,
    required this.contactoEmergencia,
    this.grauAutonomiaId,
    this.condicoesSaudeIds = const [],
    this.alergiasIds = const [],
    this.cuidadorPrincipal,
  });

  /// Dados atuais de uma ficha, para começar a edição.
  factory DadosFichaDto.daFicha(FichaDto ficha) {
    return DadosFichaDto(
      propria: ficha.eProprio,
      nomeCompleto: ficha.nomeCompleto,
      dataNascimento: ficha.dataNascimento,
      contactoEmergencia: ficha.contactoEmergencia,
      grauAutonomiaId: ficha.grauAutonomia?.id,
      condicoesSaudeIds: ficha.condicoesSaude.map((c) => c.id).toList(),
      alergiasIds: ficha.alergias.map((a) => a.id).toList(),
    );
  }

  /// Indica se é a ficha própria do criador (RF-030).
  final bool propria;

  /// Nome completo do utente.
  final String nomeCompleto;

  /// Data de nascimento (`AAAA-MM-DD`), que não pode ser futura (RF-020).
  final String dataNascimento;

  /// Contacto de emergência.
  final String contactoEmergencia;

  /// Grau de autonomia/mobilidade, das listas de OP-08.
  final int? grauAutonomiaId;

  /// Condições de saúde, das listas de OP-08.
  final List<int> condicoesSaudeIds;

  /// Alergias, das listas de OP-08.
  final List<int> alergiasIds;

  /// Cuidador principal da ficha de terceiro (obrigatório sem [propria], RF-024).
  final DefinicaoPrincipalDto? cuidadorPrincipal;

  /// Copia os dados, mudando o Cuidador principal.
  DadosFichaDto comPrincipal(DefinicaoPrincipalDto? principal) {
    return DadosFichaDto(
      propria: propria,
      nomeCompleto: nomeCompleto,
      dataNascimento: dataNascimento,
      contactoEmergencia: contactoEmergencia,
      grauAutonomiaId: grauAutonomiaId,
      condicoesSaudeIds: condicoesSaudeIds,
      alergiasIds: alergiasIds,
      cuidadorPrincipal: principal,
    );
  }

  /// Corpo de OP-09 (ou parte `dados` do pedido multipart). Na ficha própria não se envia o Cuidador principal.
  Map<String, dynamic> toJsonCriar() => {
    'propria': propria,
    ..._comuns(),
    if (!propria && cuidadorPrincipal != null)
      'cuidadorPrincipal': cuidadorPrincipal!.toJson(),
  };

  /// Corpo de OP-11 (ou parte `dados` do pedido multipart).
  Map<String, dynamic> toJsonEditar() => _comuns();

  Map<String, dynamic> _comuns() => {
    'nomeCompleto': nomeCompleto,
    'dataNascimento': dataNascimento,
    'contactoEmergencia': contactoEmergencia,
    'grauAutonomiaId': grauAutonomiaId,
    'condicoesSaudeIds': condicoesSaudeIds,
    'alergiasIds': alergiasIds,
  };
}
