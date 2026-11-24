import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/nova_nota_dto.dart';
import '../../models/nova_ocorrencia_dto.dart';
import '../../models/problema_api.dart';
import '../../models/registo_dto.dart';
import '../../shared/validador_tratamento.dart';

/// Estado do ecrã E24 Nova ocorrência ou nota. Os dados escritos ficam nos campos da página, por isso um erro
/// nunca os apaga (RNF-028).
class NovoRegistoEstado {
  /// Cria o estado.
  const NovoRegistoEstado({
    this.erros = const {},
    this.erroGeral,
    this.aGuardar = false,
  });

  /// Erros por campo (`descricao`, `data`, `hora`), já com o rótulo e a correção esperada (RNF-028).
  final Map<String, String> erros;

  /// Erro que não pertence a um campo (por exemplo, sem ligação, Familiar ou ficha arquivada).
  final String? erroGeral;

  /// Indica que OP-46 ou OP-47 está em curso.
  final bool aGuardar;
}

/// Controlador do ecrã E24 Nova ocorrência ou nota (OP-46 e OP-47; RF-090, RF-091, RNF-028). Depois de guardado,
/// o registo não se altera nem se elimina (RF-090, RF-091).
class NovoRegistoController extends Notifier<NovoRegistoEstado> {
  /// Cria o controlador da ficha [fichaId].
  NovoRegistoController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Tamanho máximo da descrição (documentação da API, OP-46 e OP-47).
  static const int maximoDescricao = 2000;

  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'descricao': 'Descrição',
    'data': 'Data',
    'hora': 'Hora',
  };

  @override
  NovoRegistoEstado build() => const NovoRegistoEstado();

  /// OP-46: valida e regista uma ocorrência com a [descricao] e a [data] (`dd/mm/aaaa`) e [hora] (`hh:mm`) de
  /// Lisboa em que aconteceu (RF-090). Devolve o registo criado, ou `null` se houver erros, que ficam no estado.
  Future<RegistoDto?> registarOcorrencia({
    required String descricao,
    required String data,
    required String hora,
  }) async {
    final erros = validar(descricao: descricao, data: data, hora: hora);
    if (erros.isNotEmpty) {
      state = NovoRegistoEstado(erros: erros);
      return null;
    }
    final dia = FormatadorDataHora.lerData(data)!;
    final horaMinuto = ValidadorTratamento.normalizarHora(hora)!;
    final ocorridaEm = FormatadorDataHora.instanteDeLisboa(
      DateTime(
        dia.year,
        dia.month,
        dia.day,
        int.parse(horaMinuto.substring(0, 2)),
        int.parse(horaMinuto.substring(3)),
      ),
    );
    return _guardar(
      () => ref
          .read(registosRepositorioProvider)
          .registarOcorrencia(
            fichaId,
            NovaOcorrenciaDto(
              descricao: descricao.trim(),
              ocorridaEm: ocorridaEm,
            ),
          ),
    );
  }

  /// OP-47: valida e regista uma nota dirigida aos Cuidadores, só com o texto (RF-091). Devolve o registo criado,
  /// ou `null` se houver erros, que ficam no estado.
  Future<RegistoDto?> registarNota({required String descricao}) async {
    final erros = validar(descricao: descricao);
    if (erros.isNotEmpty) {
      state = NovoRegistoEstado(erros: erros);
      return null;
    }
    return _guardar(
      () => ref
          .read(registosRepositorioProvider)
          .registarNota(fichaId, NovaNotaDto(descricao: descricao.trim())),
    );
  }

  /// Validação prévia: descrição obrigatória, até [maximoDescricao] caracteres, e, na ocorrência (com [data] e
  /// [hora]), data e hora válidas (RF-090, RF-091, RNF-028). O servidor volta a validar tudo.
  static Map<String, String> validar({
    required String descricao,
    String? data,
    String? hora,
  }) {
    final erros = <String, String>{};
    final texto = descricao.trim();
    if (texto.isEmpty) {
      erros['descricao'] = MensagensErro.comRotulo(
        rotulos['descricao']!,
        'É obrigatória. Descreva o que aconteceu ou o que quer dizer aos '
        'Cuidadores.',
      );
    } else if (texto.length > maximoDescricao) {
      erros['descricao'] = MensagensErro.comRotulo(
        rotulos['descricao']!,
        'Tem no máximo $maximoDescricao caracteres.',
      );
    }
    if (data != null && FormatadorDataHora.lerData(data) == null) {
      erros['data'] = MensagensErro.comRotulo(
        rotulos['data']!,
        'Indique a data em que aconteceu, no formato dd/mm/aaaa.',
      );
    }
    if (hora != null && ValidadorTratamento.normalizarHora(hora) == null) {
      erros['hora'] = MensagensErro.comRotulo(
        rotulos['hora']!,
        'Indique a hora em que aconteceu, no formato hh:mm, por exemplo '
        '15:05.',
      );
    }
    return erros;
  }

  Future<RegistoDto?> _guardar(Future<RegistoDto> Function() pedido) async {
    if (state.aGuardar) {
      return null;
    }
    state = const NovoRegistoEstado(aGuardar: true);
    try {
      final registo = await pedido();
      if (ref.mounted) {
        state = const NovoRegistoEstado();
      }
      return registo;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        state = _estadoDoProblema(problema);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = NovoRegistoEstado(erroGeral: MensagensErro.traduzir(erro));
      }
    }
    return null;
  }

  /// Erros de validação do servidor: a descrição fica no seu campo e `ocorridaEm` no campo da data. As outras
  /// recusas (Familiar `403`, ficha arquivada `409`, ficha sem acesso `404`) mostram a mensagem respetiva.
  NovoRegistoEstado _estadoDoProblema(ProblemaApi problema) {
    final erros = MensagensErro.mensagensDeCampo(problema, const {
      'descricao': 'Descrição',
      'ocorridaEm': 'Data e hora em que aconteceu',
    });
    final ocorridaEm = erros.remove('ocorridaEm');
    if (ocorridaEm != null) {
      erros['data'] = ocorridaEm;
    }
    return NovoRegistoEstado(
      erros: erros,
      erroGeral: erros.isEmpty ? MensagensErro.traduzir(problema) : null,
    );
  }
}

/// Fornecedor do controlador de E24, por ficha (o estado é descartado ao sair do ecrã).
final novoRegistoControllerProvider = NotifierProvider.autoDispose
    .family<NovoRegistoController, NovoRegistoEstado, String>(
      NovoRegistoController.new,
    );
