import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/conta_criada_dto.dart';
import '../../models/nova_conta_dto.dart';
import '../../models/problema_api.dart';
import '../../shared/validador_palavra_passe.dart';

/// Estado do ecrã E01 Registo.
class RegistoEstado {
  /// Cria o estado.
  const RegistoEstado({
    this.erros = const {},
    this.erroGeral,
    this.aEnviar = false,
  });

  /// Erros por campo (`nome`, `email`, `palavraPasse`), já com o rótulo e a regra (RNF-028).
  final Map<String, String> erros;

  /// Erro que não pertence a um campo (por exemplo, sem ligação).
  final String? erroGeral;

  /// Indica que o pedido está em curso.
  final bool aEnviar;
}

/// Controlador do ecrã E01 Registo (OP-01; RF-010, RF-011, RF-044, RNF-003, RNF-028). Usa o `AuthRepositorio`
/// diretamente (modelo de classes, secção 8.3): o registo não inicia sessão.
class RegistoController extends Notifier<RegistoEstado> {
  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'nome': 'Nome',
    'email': 'E-mail',
    'palavraPasse': 'Palavra-passe',
  };

  /// Mensagens deste ecrã que diferem do catálogo (protótipo, E01).
  static const Map<String, String> mensagensDoEcra = {
    'EMAIL_JA_REGISTADO': 'Já existe uma conta com este e-mail.',
  };

  @override
  RegistoEstado build() => const RegistoEstado();

  /// Valida os dados no dispositivo e, se estiverem completos, cria a conta. Devolve a conta criada (com o número
  /// de convites pendentes) ou `null` se houver erros, que ficam no estado.
  Future<ContaCriadaDto?> submeter({
    required String nome,
    required String email,
    required String palavraPasse,
  }) async {
    final erros = validar(nome: nome, email: email, palavraPasse: palavraPasse);
    if (erros.isNotEmpty) {
      state = RegistoEstado(erros: erros);
      return null;
    }
    state = const RegistoEstado(aEnviar: true);
    try {
      final conta = await ref
          .read(authRepositorioProvider)
          .registar(
            NovaContaDto(
              nome: nome.trim(),
              email: email.trim(),
              palavraPasse: palavraPasse,
            ),
          );
      if (ref.mounted) {
        state = const RegistoEstado();
      }
      return conta;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        state = _estadoDoProblema(problema);
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = RegistoEstado(erroGeral: MensagensErro.traduzir(erro));
      }
    }
    return null;
  }

  /// Validação prévia: campos obrigatórios e palavra-passe (RNF-003). O servidor volta a validar tudo.
  static Map<String, String> validar({
    required String nome,
    required String email,
    required String palavraPasse,
  }) {
    final erros = <String, String>{};
    if (nome.trim().isEmpty) {
      erros['nome'] = MensagensErro.comRotulo(
        rotulos['nome']!,
        'É obrigatório.',
      );
    }
    if (email.trim().isEmpty) {
      erros['email'] = MensagensErro.comRotulo(
        rotulos['email']!,
        'É obrigatório.',
      );
    }
    final regra = ValidadorPalavraPasse.validar(palavraPasse);
    if (regra != null) {
      erros['palavraPasse'] = MensagensErro.comRotulo(
        rotulos['palavraPasse']!,
        regra,
      );
    }
    return erros;
  }

  RegistoEstado _estadoDoProblema(ProblemaApi problema) {
    if (problema.codigo == 'EMAIL_JA_REGISTADO') {
      return RegistoEstado(
        erros: {'email': mensagensDoEcra['EMAIL_JA_REGISTADO']!},
      );
    }
    final erros = MensagensErro.mensagensDeCampo(problema, rotulos);
    return RegistoEstado(
      erros: erros,
      erroGeral: erros.isEmpty
          ? MensagensErro.traduzir(problema, especificas: mensagensDoEcra)
          : null,
    );
  }
}

/// Fornecedor do controlador de E01 (o estado é descartado ao sair do ecrã).
final registoControllerProvider =
    NotifierProvider.autoDispose<RegistoController, RegistoEstado>(
      RegistoController.new,
    );
