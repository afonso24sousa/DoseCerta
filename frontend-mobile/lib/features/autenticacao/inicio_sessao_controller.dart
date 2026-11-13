import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/problema_api.dart';

/// Estado do ecrã E02 Início de sessão.
class InicioSessaoEstado {
  /// Cria o estado.
  const InicioSessaoEstado({
    this.erros = const {},
    this.erro,
    this.aEnviar = false,
  });

  /// Erros por campo (`email`, `palavraPasse`).
  final Map<String, String> erros;

  /// Erro geral; nas credenciais inválidas não indica qual dos dados está errado (RF-012).
  final String? erro;

  /// Indica que o pedido está em curso.
  final bool aEnviar;
}

/// Controlador do ecrã E02 Início de sessão (OP-02; RF-012, RF-017, RNF-013). Depois do início de sessão, as
/// rotas levam o Utilizador para a lista diária.
class InicioSessaoController extends Notifier<InicioSessaoEstado> {
  /// Rótulos dos campos, para as mensagens de erro.
  static const Map<String, String> rotulos = {
    'email': 'E-mail',
    'palavraPasse': 'Palavra-passe',
  };

  @override
  InicioSessaoEstado build() => const InicioSessaoEstado();

  /// Inicia sessão. Devolve `true` se a sessão foi iniciada.
  Future<bool> entrar({
    required String email,
    required String palavraPasse,
  }) async {
    final erros = <String, String>{};
    if (email.trim().isEmpty) {
      erros['email'] = MensagensErro.comRotulo(
        rotulos['email']!,
        'É obrigatório.',
      );
    }
    if (palavraPasse.isEmpty) {
      erros['palavraPasse'] = MensagensErro.comRotulo(
        rotulos['palavraPasse']!,
        'É obrigatória.',
      );
    }
    if (erros.isNotEmpty) {
      state = InicioSessaoEstado(erros: erros);
      return false;
    }
    state = const InicioSessaoEstado(aEnviar: true);
    try {
      await ref
          .read(sessaoControllerProvider.notifier)
          .iniciarSessao(email.trim(), palavraPasse);
      if (ref.mounted) {
        state = const InicioSessaoEstado();
      }
      return true;
    } on ProblemaApi catch (problema) {
      if (ref.mounted) {
        final erros = MensagensErro.mensagensDeCampo(problema, rotulos);
        state = InicioSessaoEstado(
          erros: erros,
          erro: erros.isEmpty ? MensagensErro.traduzir(problema) : null,
        );
      }
    } on Exception catch (erro) {
      if (ref.mounted) {
        state = InicioSessaoEstado(erro: MensagensErro.traduzir(erro));
      }
    }
    return false;
  }
}

/// Fornecedor do controlador de E02.
final inicioSessaoControllerProvider =
    NotifierProvider.autoDispose<InicioSessaoController, InicioSessaoEstado>(
      InicioSessaoController.new,
    );
