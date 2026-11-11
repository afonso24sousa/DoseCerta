import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../models/categoria_documento.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_formulario.dart';
import '../../shared/folha_origem_ficheiro.dart';
import '../../shared/nova_ficha_rascunho.dart';
import '../../shared/selecionador_ficheiros.dart';
import 'nova_ficha_documentos_controller.dart';

/// Ecrã E07 Nova ficha: documentos e Cuidador principal (protótipo de baixa fidelidade v02, página 33). Cada
/// «Carregar» abre a folha com Câmara, Galeria e Ficheiro (decisão D2). Numa ficha de terceiro define-se o Cuidador
/// principal (RF-024, RF-025); na ficha própria esse passo não aparece (RF-030).
class NovaFichaDocumentosPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const NovaFichaDocumentosPage({super.key});

  @override
  ConsumerState<NovaFichaDocumentosPage> createState() =>
      _NovaFichaDocumentosPageState();
}

class _NovaFichaDocumentosPageState
    extends ConsumerState<NovaFichaDocumentosPage> {
  late final TextEditingController _email;

  @override
  void initState() {
    super.initState();
    _email = TextEditingController(
      text: ref.read(novaFichaRascunhoProvider).emailPrincipal,
    );
  }

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  Future<void> _carregar(CategoriaDocumento categoria) async {
    final origem = await FolhaOrigemFicheiro.mostrar(
      context,
      titulo: 'Carregar: ${categoria.rotulo}',
    );
    if (origem != null) {
      await ref
          .read(novaFichaDocumentosControllerProvider.notifier)
          .escolherFicheiro(categoria, origem);
    }
  }

  Future<void> _criar() async {
    FocusScope.of(context).unfocus();
    final controlador = ref.read(
      novaFichaDocumentosControllerProvider.notifier,
    );
    final resultado = await controlador.criarFicha();
    if (!mounted) {
      return;
    }
    switch (resultado) {
      case ResultadoNovaFicha.criada:
        final ficha = ref
            .read(novaFichaDocumentosControllerProvider)
            .fichaCriada;
        context.go(
          ficha == null ? AppRouter.fichas : AppRouter.ficha(ficha.id),
        );
      case ResultadoNovaFicha.voltarAosDados:
        if (context.canPop()) {
          context.pop();
        } else {
          context.go(AppRouter.novaFicha);
        }
      case ResultadoNovaFicha.recusada:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(novaFichaDocumentosControllerProvider);
    final rascunho = ref.watch(novaFichaRascunhoProvider);
    final rascunhoControlador = ref.read(novaFichaRascunhoProvider.notifier);
    final controlador = ref.read(
      novaFichaDocumentosControllerProvider.notifier,
    );
    final dados = rascunho.dados;
    return Scaffold(
      appBar: AppBar(title: const Text('Nova ficha')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Nova ficha (2 de 2): documentos e Cuidador principal',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 16),
              if (dados == null) ...[
                const BannerErro(
                  'Preencha primeiro os dados da ficha.',
                  informativo: true,
                ),
                OutlinedButton(
                  onPressed: () => context.go(AppRouter.novaFicha),
                  child: const Text('Voltar aos dados'),
                ),
              ] else ...[
                for (final categoria in CategoriaDocumento.values)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: OutlinedButton.icon(
                      onPressed: estado.ocupado
                          ? null
                          : () => _carregar(categoria),
                      icon: const Icon(Icons.upload_file_outlined),
                      label: Text('Carregar: ${categoria.rotulo}'),
                    ),
                  ),
                const Text(SelecionadorFicheiros.ficheirosAceites),
                const SizedBox(height: 8),
                for (final (indice, documento) in rascunho.documentos.indexed)
                  Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: const Icon(Icons.description_outlined),
                      title: Text(documento.ficheiro.nome),
                      subtitle: Text(documento.categoria.rotulo),
                      trailing: IconButton(
                        tooltip: 'Retirar ${documento.ficheiro.nome}',
                        icon: const Icon(Icons.close),
                        onPressed: estado.ocupado
                            ? null
                            : () => controlador.retirarDocumento(indice),
                      ),
                    ),
                  ),
                const SizedBox(height: 8),
                if (!dados.propria) ...[
                  Text(
                    'Cuidador principal',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  RadioGroup<bool>(
                    groupValue: rascunho.principalEu,
                    onChanged: (eu) =>
                        rascunhoControlador.escolherPrincipal(eu: eu ?? true),
                    child: const Column(
                      children: [
                        RadioListTile<bool>(
                          value: true,
                          title: Text('Eu sou o Cuidador principal'),
                          contentPadding: EdgeInsets.zero,
                        ),
                        RadioListTile<bool>(
                          value: false,
                          title: Text(
                            'Convidar outra pessoa por e-mail (fico Cuidador principal provisório até à resposta)',
                          ),
                          contentPadding: EdgeInsets.zero,
                        ),
                      ],
                    ),
                  ),
                  if (!rascunho.principalEu)
                    Padding(
                      padding: const EdgeInsets.only(top: 8),
                      child: CampoFormulario(
                        rotulo: NovaFichaDocumentosController.rotuloEmail,
                        controlador: _email,
                        erro: estado.erroEmail,
                        tipoTeclado: TextInputType.emailAddress,
                        sugestoesPreenchimento: const [AutofillHints.email],
                        acaoTeclado: TextInputAction.done,
                        aoMudar: rascunhoControlador.mudarEmail,
                      ),
                    ),
                ],
                if (estado.erro != null) BannerErro(estado.erro!),
                if (estado.aEnviar)
                  const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: LinearProgressIndicator(),
                  ),
                FilledButton(
                  onPressed: estado.ocupado ? null : _criar,
                  child: const Text('Criar ficha'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
