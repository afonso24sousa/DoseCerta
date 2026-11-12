import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../shared/banner_erro.dart';
import '../../shared/campo_formulario.dart';
import 'conta_controller.dart';

/// Ecrã E03 Conta (protótipo de baixa fidelidade v02, página 31). A palavra-passe nunca é mostrada (RF-013).
class ContaPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const ContaPage({super.key});

  @override
  ConsumerState<ContaPage> createState() => _ContaPageState();
}

class _ContaPageState extends ConsumerState<ContaPage> {
  final _nome = TextEditingController();
  final _email = TextEditingController();
  final _atual = TextEditingController();
  final _nova = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(_carregar);
  }

  @override
  void dispose() {
    _nome.dispose();
    _email.dispose();
    _atual.dispose();
    _nova.dispose();
    super.dispose();
  }

  Future<void> _carregar() async {
    final controlador = ref.read(contaControllerProvider.notifier);
    await controlador.carregar();
    final conta = ref.read(contaControllerProvider).conta;
    if (conta != null && mounted) {
      _nome.text = conta.nome;
      _email.text = conta.email;
    }
  }

  void _informar(String mensagem) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(mensagem)));
  }

  Future<void> _guardarDados() async {
    FocusScope.of(context).unfocus();
    final conta = await ref
        .read(contaControllerProvider.notifier)
        .guardarDados(_nome.text, _email.text);
    if (conta != null && mounted) {
      _nome.text = conta.nome;
      _email.text = conta.email;
      _informar('Dados guardados.');
    }
  }

  Future<void> _alterarPalavraPasse() async {
    FocusScope.of(context).unfocus();
    final alterada = await ref
        .read(contaControllerProvider.notifier)
        .alterarPalavraPasse(_atual.text, _nova.text);
    if (alterada && mounted) {
      _atual.clear();
      _nova.clear();
      _informar('Palavra-passe alterada.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(contaControllerProvider);
    final Widget conteudo;
    if (estado.conta == null && estado.aCarregar) {
      conteudo = const Padding(
        padding: EdgeInsets.all(32),
        child: Center(child: CircularProgressIndicator()),
      );
    } else if (estado.conta == null && estado.erroCarregar != null) {
      conteudo = Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          BannerErro(estado.erroCarregar!),
          OutlinedButton(
            onPressed: _carregar,
            child: const Text('Tentar de novo'),
          ),
        ],
      );
    } else {
      conteudo = _formularios(context, estado);
    }
    return Scaffold(
      appBar: AppBar(title: const Text('Conta')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'A minha conta',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 16),
              conteudo,
            ],
          ),
        ),
      ),
    );
  }

  Widget _formularios(BuildContext context, ContaEstado estado) {
    final livre = !estado.ocupado;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        CampoFormulario(
          rotulo: 'Nome',
          controlador: _nome,
          erro: estado.errosDados['nome'],
          sugestoesPreenchimento: const [AutofillHints.name],
          ativo: livre,
        ),
        CampoFormulario(
          rotulo: 'E-mail',
          controlador: _email,
          erro: estado.errosDados['email'],
          tipoTeclado: TextInputType.emailAddress,
          sugestoesPreenchimento: const [AutofillHints.email],
          acaoTeclado: TextInputAction.done,
          ativo: livre,
        ),
        if (estado.erroDados != null) BannerErro(estado.erroDados!),
        Align(
          alignment: Alignment.centerLeft,
          child: FilledButton(
            onPressed: livre ? _guardarDados : null,
            child: const Text('Guardar dados'),
          ),
        ),
        const Divider(height: 40),
        CampoFormulario(
          rotulo: 'Palavra-passe atual',
          controlador: _atual,
          erro: estado.errosPalavraPasse['palavraPasseAtual'],
          palavraPasse: true,
          sugestoesPreenchimento: const [AutofillHints.password],
          ativo: livre,
        ),
        CampoFormulario(
          rotulo: 'Nova palavra-passe',
          ajuda: 'Mínimo 8 caracteres, com uma letra e um algarismo.',
          controlador: _nova,
          erro: estado.errosPalavraPasse['palavraPasseNova'],
          palavraPasse: true,
          sugestoesPreenchimento: const [AutofillHints.newPassword],
          acaoTeclado: TextInputAction.done,
          ativo: livre,
        ),
        if (estado.erroPalavraPasse != null)
          BannerErro(estado.erroPalavraPasse!),
        Align(
          alignment: Alignment.centerLeft,
          child: OutlinedButton(
            onPressed: livre ? _alterarPalavraPasse : null,
            child: const Text('Alterar palavra-passe'),
          ),
        ),
        const Divider(height: 40),
        if (estado.erroTerminar != null) BannerErro(estado.erroTerminar!),
        Align(
          alignment: Alignment.centerLeft,
          child: OutlinedButton(
            onPressed: livre
                ? ref.read(contaControllerProvider.notifier).terminarSessao
                : null,
            child: const Text('Terminar sessão'),
          ),
        ),
      ],
    );
  }
}
