import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../models/documento_dto.dart';
import '../../shared/banner_erro.dart';
import '../../shared/folha_categoria_documento.dart';
import '../../shared/folha_origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import 'documentos_ficha_controller.dart';

/// Ecrã E10 Documentos da ficha (protótipo de baixa fidelidade v02, página 34): Cuidadores e Familiares associados
/// consultam e abrem os documentos (RF-028, RNF-007); só o Cuidador, numa ficha ativa, acrescenta documentos
/// (RF-022, RF-048). Uma falha ao obter um documento aparece no lugar dele, com «Tentar novamente» (RF-124).
class DocumentosFichaPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId].
  const DocumentosFichaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  @override
  ConsumerState<DocumentosFichaPage> createState() =>
      _DocumentosFichaPageState();
}

class _DocumentosFichaPageState extends ConsumerState<DocumentosFichaPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(documentosFichaControllerProvider(widget.fichaId).notifier)
          .carregar(),
    );
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
          .read(documentosFichaControllerProvider(widget.fichaId).notifier)
          .carregarDocumento(categoria, origem);
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(documentosFichaControllerProvider(widget.fichaId));
    final controlador = ref.read(
      documentosFichaControllerProvider(widget.fichaId).notifier,
    );
    return Scaffold(
      appBar: AppBar(title: const Text('Documentos da ficha')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.carregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              if (acesso != null && !acesso.ativa)
                BannerErro(
                  MensagensErro.catalogo['FICHA_ARQUIVADA']!,
                  informativo: true,
                ),
              if (estado.erroLista != null) ...[
                BannerErro(estado.erroLista!),
                OutlinedButton(
                  onPressed: estado.aCarregar ? null : controlador.carregar,
                  child: const Text('Tentar novamente'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.aCarregar && !estado.carregada)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: LinearProgressIndicator(),
                ),
              if (estado.carregada && estado.documentos.isEmpty)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: Text('Esta ficha ainda não tem documentos.'),
                ),
              for (final documento in estado.documentos)
                _LinhaDocumento(
                  documento: documento,
                  aAbrir: estado.aAbrir == documento.id,
                  erro: estado.errosAbertura[documento.id],
                  abrir: () => controlador.abrirDocumento(documento),
                  repetir: () => controlador.repetirAbertura(documento),
                ),
              if (acesso != null && acesso.podeEscrever) ...[
                const SizedBox(height: 8),
                if (estado.erro != null) BannerErro(estado.erro!),
                if (estado.aEnviar)
                  const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: LinearProgressIndicator(),
                  ),
                OutlinedButton.icon(
                  onPressed: estado.aEnviar ? null : _acrescentar,
                  icon: const Icon(Icons.upload_file_outlined),
                  label: const Text('Carregar: Documento a acrescentar'),
                ),
                const Padding(
                  padding: EdgeInsets.only(top: 8),
                  child: Text(SelecionadorFicheiros.ficheirosAceites),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _LinhaDocumento extends StatelessWidget {
  const _LinhaDocumento({
    required this.documento,
    required this.aAbrir,
    required this.erro,
    required this.abrir,
    required this.repetir,
  });

  final DocumentoDto documento;
  final bool aAbrir;
  final String? erro;
  final VoidCallback abrir;
  final VoidCallback repetir;

  @override
  Widget build(BuildContext context) {
    final data = FormatadorDataHora.apresentarData(documento.carregadoEm);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              documento.categoria.rotulo,
              style: Theme.of(context).textTheme.titleSmall,
            ),
            Text('${documento.nomeOriginal} · $data'),
            const SizedBox(height: 8),
            if (erro != null) ...[
              BannerErro(erro!),
              OutlinedButton(
                onPressed: aAbrir ? null : repetir,
                child: const Text('Tentar novamente'),
              ),
            ] else
              Align(
                alignment: Alignment.centerLeft,
                child: OutlinedButton(
                  onPressed: aAbrir ? null : abrir,
                  child: Text(aAbrir ? 'A abrir…' : 'Abrir'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
