import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../shared/banner_erro.dart';
import '../../shared/nova_ficha_rascunho.dart';
import 'campos_ficha.dart';
import 'formulario_ficha.dart';
import 'nova_ficha_dados_controller.dart';

/// Ecrã E06 Nova ficha: dados e necessidades (protótipo de baixa fidelidade v02, página 32). Primeiro de dois
/// passos: ficha de uma terceira pessoa ou «Gerir a minha própria saúde» (indisponível para quem já tem ficha
/// própria, RF-031), dados de identificação e necessidades das listas fechadas.
class NovaFichaDadosPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const NovaFichaDadosPage({super.key});

  @override
  ConsumerState<NovaFichaDadosPage> createState() => _NovaFichaDadosPageState();
}

class _NovaFichaDadosPageState extends ConsumerState<NovaFichaDadosPage> {
  final _formulario = FormularioFicha();

  @override
  void initState() {
    super.initState();
    // O rascunho é limpo em E05, ao tocar em «Nova ficha». Aqui só se repõe, para o ecrã mostrar os mesmos dados
    // se for criado de novo ao voltar de E07 (D4).
    final guardados = ref.read(novaFichaRascunhoProvider).dados;
    if (guardados != null) {
      _formulario.preencher(guardados);
    }
    Future.microtask(() {
      final controlador = ref.read(novaFichaDadosControllerProvider.notifier);
      if (guardados != null) {
        controlador.escolherPropria(propria: guardados.propria);
      }
      return controlador.carregarDominios();
    });
  }

  @override
  void dispose() {
    _formulario.dispose();
    super.dispose();
  }

  void _seguinte() {
    FocusScope.of(context).unfocus();
    final avancar = ref
        .read(novaFichaDadosControllerProvider.notifier)
        .seguinte(_formulario);
    if (avancar) {
      context.go(AppRouter.novaFichaDocumentos);
    }
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(novaFichaDadosControllerProvider);
    final controlador = ref.read(novaFichaDadosControllerProvider.notifier);
    final errosServidor = ref.watch(
      novaFichaRascunhoProvider.select((r) => r.errosDados),
    );
    final erros = {...errosServidor, ...estado.erros};
    final dominios = estado.dominios;
    return Scaffold(
      appBar: AppBar(title: const Text('Nova ficha')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Nova ficha (1 de 2): dados e necessidades',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              if (errosServidor.isNotEmpty)
                const BannerErro(
                  'O servidor recusou alguns dados. Corrija os campos assinalados e avance de novo.',
                ),
              RadioGroup<bool>(
                groupValue: estado.propria,
                onChanged: (propria) =>
                    controlador.escolherPropria(propria: propria ?? false),
                child: Column(
                  children: [
                    const RadioListTile<bool>(
                      value: false,
                      title: Text('Ficha de uma terceira pessoa'),
                      contentPadding: EdgeInsets.zero,
                    ),
                    RadioListTile<bool>(
                      value: true,
                      enabled: !estado.temFichaPropria,
                      title: const Text('Gerir a minha própria saúde'),
                      subtitle: estado.temFichaPropria
                          ? Text(
                              MensagensErro
                                  .catalogo['FICHA_PROPRIA_JA_EXISTE']!,
                            )
                          : null,
                      contentPadding: EdgeInsets.zero,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              if (estado.erroCarregar != null) ...[
                BannerErro(estado.erroCarregar!),
                OutlinedButton(
                  onPressed: estado.aCarregar
                      ? null
                      : controlador.carregarDominios,
                  child: const Text('Tentar novamente'),
                ),
              ] else if (dominios == null)
                const LinearProgressIndicator()
              else ...[
                CamposFicha(
                  formulario: _formulario,
                  dominios: dominios,
                  erros: erros,
                  hoje: ref.read(relogioProvider).agora(),
                ),
                FilledButton(
                  onPressed: _seguinte,
                  child: const Text('Seguinte'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
