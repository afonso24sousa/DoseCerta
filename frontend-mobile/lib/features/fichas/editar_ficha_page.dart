import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/dados_ficha_dto.dart';
import '../../models/papel.dart';
import '../../shared/banner_erro.dart';
import '../../shared/folha_categoria_documento.dart';
import '../../shared/folha_origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import 'campos_ficha.dart';
import 'editar_ficha_controller.dart';
import 'formulario_ficha.dart';

/// Ecrã E09 Editar ficha (protótipo de baixa fidelidade v02, página 34): só um Cuidador de uma ficha ativa edita
/// os dados (RF-029). Pode acrescentar documentos; se um não for guardado, nenhuma alteração é guardada (RF-023).
class EditarFichaPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId].
  const EditarFichaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  @override
  ConsumerState<EditarFichaPage> createState() => _EditarFichaPageState();
}

class _EditarFichaPageState extends ConsumerState<EditarFichaPage> {
  final _formulario = FormularioFicha();
  bool _preenchido = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(editarFichaControllerProvider(widget.fichaId).notifier)
          .carregar(),
    );
  }

  @override
  void dispose() {
    _formulario.dispose();
    super.dispose();
  }

  Future<void> _acrescentar() async {
    final categoria = await FolhaCategoriaDocumento.mostrar(context);
    if (categoria == null || !mounted) {
      return;
    }
    final origem = await FolhaOrigemFicheiro.mostrar(
      context,
      titulo: 'Carregar: ${categoria.rotulo}',
    );
    if (origem != null) {
      await ref
          .read(editarFichaControllerProvider(widget.fichaId).notifier)
          .escolherFicheiro(categoria, origem);
    }
  }

  Future<void> _guardar() async {
    FocusScope.of(context).unfocus();
    final guardada = await ref
        .read(editarFichaControllerProvider(widget.fichaId).notifier)
        .guardar(_formulario);
    if (guardada && mounted) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(const SnackBar(content: Text('Alterações guardadas.')));
      _voltar();
    }
  }

  void _voltar() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRouter.ficha(widget.fichaId));
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId));
    final estado = ref.watch(editarFichaControllerProvider(widget.fichaId));
    final controlador = ref.read(
      editarFichaControllerProvider(widget.fichaId).notifier,
    );
    final ficha = acesso.value?.ficha;
    if (ficha != null && !_preenchido) {
      _formulario.preencher(DadosFichaDto.daFicha(ficha));
      _preenchido = true;
    }
    final dominios = estado.dominios;

    final List<Widget> conteudo;
    if (acesso.hasError) {
      conteudo = [BannerErro(MensagensErro.traduzir(acesso.error!))];
    } else if (ficha == null) {
      conteudo = const [LinearProgressIndicator()];
    } else if (ficha.meuPapel != Papel.cuidador) {
      conteudo = [BannerErro(MensagensErro.catalogo['ACESSO_PROIBIDO']!)];
    } else if (!ficha.ativa) {
      conteudo = [BannerErro(MensagensErro.catalogo['FICHA_ARQUIVADA']!)];
    } else if (estado.erroCarregar != null) {
      conteudo = [
        BannerErro(estado.erroCarregar!),
        OutlinedButton(
          onPressed: estado.aCarregar ? null : controlador.carregar,
          child: const Text('Tentar novamente'),
        ),
      ];
    } else if (dominios == null) {
      conteudo = const [LinearProgressIndicator()];
    } else {
      conteudo = [
        CamposFicha(
          formulario: _formulario,
          dominios: dominios,
          erros: estado.erros,
          hoje: ref.read(relogioProvider).agora(),
        ),
        OutlinedButton.icon(
          onPressed: estado.ocupado ? null : _acrescentar,
          icon: const Icon(Icons.upload_file_outlined),
          label: const Text('Carregar: Documento a acrescentar (opcional)'),
        ),
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 8),
          child: Text(SelecionadorFicheiros.ficheirosAceites),
        ),
        for (final (indice, documento) in estado.ficheiros.indexed)
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
                    : () => controlador.retirarFicheiro(indice),
              ),
            ),
          ),
        if (estado.erro != null) BannerErro(estado.erro!),
        if (estado.aGuardar)
          const Padding(
            padding: EdgeInsets.only(bottom: 8),
            child: LinearProgressIndicator(),
          ),
        FilledButton(
          onPressed: estado.ocupado ? null : _guardar,
          child: const Text('Guardar'),
        ),
        const SizedBox(height: 8),
        OutlinedButton(
          onPressed: estado.aGuardar ? null : _voltar,
          child: const Text('Cancelar'),
        ),
      ];
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Editar ficha')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: conteudo,
          ),
        ),
      ),
    );
  }
}
