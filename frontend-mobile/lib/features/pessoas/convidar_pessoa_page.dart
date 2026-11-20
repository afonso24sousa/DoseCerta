import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../models/papel.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_formulario.dart';
import 'convidar_pessoa_controller.dart';

/// Ecrã E13 Convidar pessoa (protótipo de baixa fidelidade v02, página 36): o Cuidador indica o e-mail e o papel
/// oferecido (RF-040) e, numa ficha de terceiro, pode oferecer o papel de Cuidador principal (RF-025, RF-027); na
/// ficha própria essa opção não existe (RF-034). Enviado o convite, volta a E12 com o convite como resultado.
class ConvidarPessoaPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId].
  const ConvidarPessoaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  /// Explicação do envio do convite (protótipo, E13; RF-135, RF-136).
  static const String notaEnvio =
      'O convite é enviado por e-mail. Se o envio falhar, é reenviado até 3 '
      'vezes e o convite mantém-se na área de convites.';

  @override
  ConsumerState<ConvidarPessoaPage> createState() => _ConvidarPessoaPageState();
}

class _ConvidarPessoaPageState extends ConsumerState<ConvidarPessoaPage> {
  final TextEditingController _email = TextEditingController();
  Papel? _papel;
  bool _paraPrincipal = false;

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  Future<void> _enviar() async {
    final convite = await ref
        .read(convidarPessoaControllerProvider(widget.fichaId).notifier)
        .convidar(
          email: _email.text,
          papel: _papel,
          paraPrincipal: _paraPrincipal,
        );
    if (convite != null && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Convite enviado para ${_email.text.trim()}.')),
      );
      context.pop(convite);
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(convidarPessoaControllerProvider(widget.fichaId));
    final fichaPropria = acesso?.ficha.eProprio ?? false;
    final erroPapel = estado.erros['papel'];
    return Scaffold(
      appBar: AppBar(title: const Text('Convidar pessoa')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            CampoFormulario(
              rotulo: ConvidarPessoaController.rotulos['email']!,
              controlador: _email,
              erro: estado.erros['email'],
              tipoTeclado: TextInputType.emailAddress,
              sugestoesPreenchimento: const [AutofillHints.email],
              ativo: !estado.aEnviar,
            ),
            Text('Papel', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            SegmentedButton<Papel>(
              segments: const [
                ButtonSegment(value: Papel.cuidador, label: Text('Cuidador')),
                ButtonSegment(value: Papel.familiar, label: Text('Familiar')),
              ],
              selected: {?_papel},
              emptySelectionAllowed: true,
              showSelectedIcon: false,
              onSelectionChanged: estado.aEnviar
                  ? null
                  : (escolha) => setState(() {
                      _papel = escolha.isEmpty ? null : escolha.single;
                      if (_papel != Papel.cuidador) {
                        _paraPrincipal = false;
                      }
                    }),
            ),
            if (erroPapel != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  erroPapel,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            const SizedBox(height: 8),
            if (_papel == Papel.cuidador && !fichaPropria)
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                controlAffinity: ListTileControlAffinity.leading,
                value: _paraPrincipal,
                onChanged: estado.aEnviar
                    ? null
                    : (valor) =>
                          setState(() => _paraPrincipal = valor ?? false),
                title: Text(ConvidarPessoaController.rotulos['paraPrincipal']!),
              ),
            const SizedBox(height: 8),
            if (estado.erroGeral != null) BannerErro(estado.erroGeral!),
            const Text(ConvidarPessoaPage.notaEnvio),
            const SizedBox(height: 16),
            if (estado.aEnviar)
              const Padding(
                padding: EdgeInsets.only(bottom: 8),
                child: LinearProgressIndicator(),
              ),
            FilledButton(
              onPressed: estado.aEnviar ? null : _enviar,
              child: const Text('Enviar convite'),
            ),
          ],
        ),
      ),
    );
  }
}
