import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/convite_dto.dart';
import '../../models/novo_convite_dto.dart';
import '../../models/papel.dart';
import '../../models/problema_api.dart';

/// Estado do ecrã E13 Convidar pessoa.
class ConvidarPessoaEstado {
  /// Cria o estado.
  const ConvidarPessoaEstado({
    this.erros = const {},
    this.erroGeral,
    this.aEnviar = false,
  });

  /// Erros por campo (`email`, `papel`), já com o rótulo e a regra (RNF-028).
  final Map<String, String> erros;

  /// Erro que não pertence a um campo (por exemplo, sem ligação ou ficha arquivada).
  final String? erroGeral;

  /// Indica que OP-19 está em curso.
  final bool aEnviar;
}

/// Controlador do ecrã E13 Convidar pessoa (OP-19; RF-025, RF-032, RF-034, RF-040, RF-041, RF-043, RF-135,
/// RF-136, RNF-028).
class ConvidarPessoaController extends Notifier<ConvidarPessoaEstado> {
  /// Cria o controlador da ficha [fichaId].
  ConvidarPessoaController(this.fichaId);

  /// Identificador da ficha.
  final String fichaId;

  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'email': 'E-mail da pessoa',
    'papel': 'Papel',
    'paraPrincipal': 'Oferecer o papel de Cuidador principal',
  };

  /// Mensagem do protótipo (E13) para o convite pendente duplicado (RF-043).
  static const String convitePendenteDuplicado =
      'Já existe um convite pendente para este e-mail nesta ficha.';

  /// Mensagem quando já há um convite para Cuidador principal pendente nesta ficha, para qualquer e-mail
  /// (documentação da API, OP-19).
  static const String principalPendente =
      'Já existe um convite para Cuidador principal pendente nesta ficha. '
      'Aguarde a resposta antes de convidar outra pessoa para esse papel.';

  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  @override
  ConvidarPessoaEstado build() => const ConvidarPessoaEstado();

  /// Valida os dados no dispositivo e, se estiverem completos, envia o convite. Devolve o convite criado ou
  /// `null` se houver erros, que ficam no estado; os dados do formulário ficam na página.
  Future<ConviteDto?> convidar({
    required String email,
    required Papel? papel,
    required bool paraPrincipal,
  }) async {
    final erros = validar(email: email, papel: papel);
    if (erros.isNotEmpty) {
      state = ConvidarPessoaEstado(erros: erros);
      return null;
    }
    state = const ConvidarPessoaEstado(aEnviar: true);
    try {
      final convite = await ref
          .read(convitesRepositorioProvider)
          .convidar(
            fichaId,
            NovoConviteDto(
              email: email.trim(),
              papel: papel!,
              paraPrincipal: papel == Papel.cuidador && paraPrincipal,
            ),
          );
      if (ref.mounted) {
        state = const ConvidarPessoaEstado();
      }
      return convite;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        state = _estadoDoProblema(problema, paraPrincipal: paraPrincipal);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = ConvidarPessoaEstado(erroGeral: MensagensErro.traduzir(erro));
      }
    }
    return null;
  }

  /// Validação prévia: e-mail obrigatório e válido e papel Cuidador ou Familiar (RF-040, RNF-028). O servidor
  /// volta a validar tudo.
  static Map<String, String> validar({
    required String email,
    required Papel? papel,
  }) {
    final erros = <String, String>{};
    final texto = email.trim();
    if (texto.isEmpty) {
      erros['email'] = MensagensErro.comRotulo(
        rotulos['email']!,
        'É obrigatório.',
      );
    } else if (!_email.hasMatch(texto)) {
      erros['email'] = MensagensErro.comRotulo(
        rotulos['email']!,
        'Indique um e-mail válido, por exemplo rui@exemplo.pt.',
      );
    }
    if (papel == null) {
      erros['papel'] = MensagensErro.comRotulo(
        rotulos['papel']!,
        'Escolha Cuidador ou Familiar.',
      );
    }
    return erros;
  }

  ConvidarPessoaEstado _estadoDoProblema(
    ProblemaApi problema, {
    required bool paraPrincipal,
  }) {
    switch (problema.codigo) {
      case 'CONVITE_PENDENTE_EXISTE':
        return paraPrincipal
            ? const ConvidarPessoaEstado(erroGeral: principalPendente)
            : const ConvidarPessoaEstado(
                erros: {'email': convitePendenteDuplicado},
              );
      case 'PESSOA_JA_ASSOCIADA':
        return ConvidarPessoaEstado(
          erros: {'email': MensagensErro.catalogo['PESSOA_JA_ASSOCIADA']!},
        );
    }
    final erros = MensagensErro.mensagensDeCampo(problema, rotulos);
    return ConvidarPessoaEstado(
      erros: erros,
      erroGeral: erros.isEmpty ? MensagensErro.traduzir(problema) : null,
    );
  }
}

/// Fornecedor do controlador de E13, por ficha (o estado é descartado ao sair do ecrã).
final convidarPessoaControllerProvider = NotifierProvider.autoDispose
    .family<ConvidarPessoaController, ConvidarPessoaEstado, String>(
      ConvidarPessoaController.new,
    );
