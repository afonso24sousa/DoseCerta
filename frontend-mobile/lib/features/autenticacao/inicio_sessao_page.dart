import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../core/sessao_controller.dart';
import '../../models/conta_criada_dto.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_formulario.dart';
import 'inicio_sessao_controller.dart';

/// Ecrã E02 Início de sessão (protótipo de baixa fidelidade v02, página 30).
class InicioSessaoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã. [contaCriada] chega do registo (E01), para indicar os convites pendentes (RF-044).
  const InicioSessaoPage({super.key, this.contaCriada});

  /// Conta acabada de criar, se o Utilizador vem do registo.
  final ContaCriadaDto? contaCriada;

  /// Mensagem apresentada depois do registo (RF-044).
  static String mensagemContaCriada(ContaCriadaDto conta) {
    final n = conta.convitesPendentes;
    final convites = switch (n) {
      0 => '',
      1 => ' Tem 1 convite à sua espera na área de convites.',
      _ => ' Tem $n convites à sua espera na área de convites.',
    };
    return 'Conta criada. Inicie sessão.$convites';
  }

  @override
  ConsumerState<InicioSessaoPage> createState() => _InicioSessaoPageState();
}

class _InicioSessaoPageState extends ConsumerState<InicioSessaoPage> {
  late final TextEditingController _email = TextEditingController(
    text: widget.contaCriada?.email,
  );
  final _palavraPasse = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _palavraPasse.dispose();
    super.dispose();
  }

  Future<void> _entrar() async {
    FocusScope.of(context).unfocus();
    await ref
        .read(inicioSessaoControllerProvider.notifier)
        .entrar(email: _email.text, palavraPasse: _palavraPasse.text);
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(inicioSessaoControllerProvider);
    final expirada =
        ref.watch(sessaoControllerProvider).motivoFim ==
        MotivoFimSessao.expirada;
    return Scaffold(
      appBar: AppBar(title: const Text('Início de sessão')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: AutofillGroup(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Iniciar sessão',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 16),
                if (widget.contaCriada != null)
                  BannerErro(
                    InicioSessaoPage.mensagemContaCriada(widget.contaCriada!),
                    informativo: true,
                  ),
                if (expirada && widget.contaCriada == null)
                  const BannerErro(MensagensErro.sessaoTerminada),
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
                  controlador: _palavraPasse,
                  erro: estado.erros['palavraPasse'],
                  palavraPasse: true,
                  sugestoesPreenchimento: const [AutofillHints.password],
                  acaoTeclado: TextInputAction.done,
                  aoSubmeter: (_) => _entrar(),
                  ativo: !estado.aEnviar,
                ),
                Align(
                  alignment: Alignment.centerLeft,
                  child: FilledButton(
                    onPressed: estado.aEnviar ? null : _entrar,
                    child: estado.aEnviar
                        ? const SizedBox.square(
                            dimension: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Entrar'),
                  ),
                ),
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton(
                    onPressed: estado.aEnviar
                        ? null
                        : () => context.go(AppRouter.registo),
                    child: const Text('Criar conta'),
                  ),
                ),
                const SizedBox(height: 8),
                if (estado.erro != null) BannerErro(estado.erro!),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
