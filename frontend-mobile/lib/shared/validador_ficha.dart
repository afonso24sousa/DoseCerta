import '../core/formatador_data_hora.dart';
import '../core/mensagens_erro.dart';

/// Validação prévia dos dados de identificação da ficha (RF-020, RNF-028), em E06 e E09. Os erros usam os nomes
/// dos campos da API, para se juntarem aos que o servidor devolve. A decisão final é sempre do servidor.
class ValidadorFicha {
  const ValidadorFicha._();

  /// Rótulos dos campos da ficha, para as mensagens de erro (protótipo, E06 e E09).
  static const Map<String, String> rotulos = {
    'nomeCompleto': 'Nome completo',
    'dataNascimento': 'Data de nascimento',
    'contactoEmergencia': 'Contacto de emergência',
    'grauAutonomiaId': 'Grau de autonomia/mobilidade',
    'condicoesSaudeIds': 'Condições de saúde relevantes',
    'alergiasIds': 'Alergias',
  };

  /// Campos que pertencem ao formulário de dados (E06 e E09).
  static Set<String> get camposDaFicha => rotulos.keys.toSet();

  /// Mensagem do protótipo para a data de nascimento em falta, mal escrita ou futura (E06).
  static const String erroData =
      'Data de nascimento: indique uma data passada no formato dd/mm/aaaa.';

  /// Regra do contacto de emergência aplicada pelo servidor (não descrita na API v03; decisão D7).
  static const String regraContacto =
      'Tem de ser um número de telefone com algarismos, espaços e o prefixo + opcional, até 30 caracteres, '
      'por exemplo +351 910 000 001.';

  static final RegExp _contacto = RegExp(r'^\+?[0-9 ]+$');

  /// Valida o nome, a data de nascimento (`dd/mm/aaaa`, não futura em relação a [hoje]) e o contacto de
  /// emergência; devolve os erros por campo da API (vazio se tudo estiver certo).
  static Map<String, String> validar({
    required String nomeCompleto,
    required String dataNascimento,
    required String contactoEmergencia,
    required DateTime hoje,
  }) {
    final erros = <String, String>{};
    if (nomeCompleto.trim().isEmpty) {
      erros['nomeCompleto'] = MensagensErro.comRotulo(
        rotulos['nomeCompleto']!,
        'É obrigatório.',
      );
    }
    final data = FormatadorDataHora.lerData(dataNascimento);
    final dia = DateTime(hoje.year, hoje.month, hoje.day);
    if (data == null || data.isAfter(dia)) {
      erros['dataNascimento'] = erroData;
    }
    final contacto = contactoEmergencia.trim();
    if (contacto.isEmpty) {
      erros['contactoEmergencia'] = MensagensErro.comRotulo(
        rotulos['contactoEmergencia']!,
        'É obrigatório.',
      );
    } else if (contacto.length > 30 ||
        !_contacto.hasMatch(contacto) ||
        !contacto.contains(RegExp('[0-9]'))) {
      erros['contactoEmergencia'] = MensagensErro.comRotulo(
        rotulos['contactoEmergencia']!,
        regraContacto,
      );
    }
    return erros;
  }
}
