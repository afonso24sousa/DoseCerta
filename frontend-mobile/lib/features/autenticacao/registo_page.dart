import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_formulario.dart';
import 'registo_controller.dart';

/// Ecrã E01 Registo (protótipo de baixa fidelidade v02, página 30).
class RegistoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const RegistoPage({super.key});

  @override
  ConsumerState<RegistoPage> createState() => _RegistoPageState();
}

class _RegistoPageState extends ConsumerState<RegistoPage> {
  final _nome = TextEditingController();
  final _email = TextEditingController();
  final _palavraPasse = TextEditingController();

  @override
  void dispose() {
    _nome.dispose();
    _email.dispose();
    _palavraPasse.dispose();
    super.dispose();
  }

  Future<void> _criarConta() async {
    FocusScope.of(context).unfocus();
    final conta = await ref
        .read(registoControllerProvider.notifier)
        .submeter(
          nome: _nome.text,
          email: _email.text,
          palavraPasse: _palavraPasse.text,
        );
    if (conta != null && mounted) {
      context.go(AppRouter.entrar, extra: conta);
    }
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(registoControllerProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Registo')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: AutofillGroup(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Criar conta',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 16),
                CampoFormulario(
                  rotulo: 'Nome',
                  controlador: _nome,
                  erro: estado.erros['nome'],
                  sugestoesPreenchimento: const [AutofillHints.name],
                  ativo: !estado.aEnviar,
                ),
                CampoFormulario(
                  rotulo: 'E-mail',
                  controlador: _email,
                  erro: estado.erros['email'],
                  tipoTeclado: TextInputType.emailAddress,
                  sugestoesPreenchimento: const [AutofillHints.email],
                  ativo: !estado.aEnviar,
                ),
                CampoFormulario(
                  rotulo: 'Palavra-passe',
                  ajuda: 'Mínimo 8 caracteres, com uma letra e um algarismo.',
                  controlador: _palavraPasse,
                  erro: estado.erros['palavraPasse'],
                  palavraPasse: true,
                  sugestoesPreenchimento: const [AutofillHints.newPassword],
                  acaoTeclado: TextInputAction.done,
                  aoSubmeter: (_) => _criarConta(),
                  ativo: !estado.aEnviar,
                ),
                if (estado.erroGeral != null) BannerErro(estado.erroGeral!),
                Align(
                  alignment: Alignment.centerLeft,
                  child: FilledButton(
                    onPressed: estado.aEnviar ? null : _criarConta,
                    child: estado.aEnviar
                        ? const SizedBox.square(
                            dimension: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Criar conta'),
                  ),
                ),
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton(
                    onPressed: estado.aEnviar
                        ? null
                        : () => context.go(AppRouter.entrar),
                    child: const Text('Já tenho conta'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
