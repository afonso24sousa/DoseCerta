import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../models/pessoa_ficha_dto.dart';
import '../../shared/apresentacao_papel.dart';
import '../../shared/banner_erro.dart';
import '../../shared/dialogo_confirmacao.dart';
import 'pessoas_ficha_controller.dart';

/// Ecrã E12 Pessoas da ficha (protótipo de baixa fidelidade v02, página 35): as pessoas associadas, com o papel de
/// cada uma e o Cuidador principal, incluindo o provisório (RF-025). O Cuidador, numa ficha ativa, tem as ações num
/// menu por pessoa, com confirmação (RF-049, RF-130 a RF-132); o Familiar e a ficha arquivada só consultam (RF-048,
/// RF-120). Se o servidor recusar uma ação, a explicação aparece e a lista fica como estava (RF-033, RF-034,
/// RF-133, RF-134).
class PessoasFichaPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId].
  const PessoasFichaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  @override
  ConsumerState<PessoasFichaPage> createState() => _PessoasFichaPageState();
}

class _PessoasFichaPageState extends ConsumerState<PessoasFichaPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(pessoasFichaControllerProvider(widget.fichaId).notifier)
          .carregar(),
    );
  }

  Future<void> _executar(AcaoPessoa acao, PessoaFichaDto pessoa) async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: acao.rotulo,
      mensagem: acao.efeito(pessoa.nome),
      acao: acao.botaoConfirmar,
    );
    if (!confirmado || !mounted) {
      return;
    }
    final resultado = await ref
        .read(pessoasFichaControllerProvider(widget.fichaId).notifier)
        .executar(acao, pessoa);
    if (!mounted) {
      return;
    }
    switch (resultado) {
      case ResultadoAcaoPessoa.saiuDaFicha:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Deixou de ter acesso à ficha.')),
        );
        context.go(AppRouter.fichas);
      case ResultadoAcaoPessoa.feita:
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${acao.rotulo}: ${pessoa.nome}. Feito.')),
        );
      case ResultadoAcaoPessoa.falhou:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(pessoasFichaControllerProvider(widget.fichaId));
    final controlador = ref.read(
      pessoasFichaControllerProvider(widget.fichaId).notifier,
    );
    final podeGerir = acesso != null && acesso.podeEscrever;
    final fichaPropria = acesso?.ficha.eProprio ?? false;
    return Scaffold(
      appBar: AppBar(title: const Text('Pessoas da ficha')),
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
              Text(
                'Pessoas associadas',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 16),
              if (estado.erroLista != null) ...[
                BannerErro(estado.erroLista!),
                OutlinedButton(
                  onPressed: estado.aCarregar ? null : controlador.carregar,
                  child: const Text('Tentar novamente'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.erroAcao != null) BannerErro(estado.erroAcao!),
              if ((estado.aCarregar && !estado.carregada) || estado.aAlterar)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: LinearProgressIndicator(),
                ),
              for (final pessoa in estado.pessoas)
                _LinhaPessoa(
                  pessoa: pessoa,
                  acoes: podeGerir
                      ? AcaoPessoa.disponiveis(
                          pessoa,
                          fichaPropria: fichaPropria,
                        )
                      : const [],
                  ativo: !estado.aAlterar,
                  aoEscolher: (acao) => _executar(acao, pessoa),
                ),
              if (estado.pessoas.any((p) => p.provisorio))
                const Padding(
                  padding: EdgeInsets.only(top: 8),
                  child: Text(
                    'Com um convite para Cuidador principal pendente, o '
                    'criador aparece como Cuidador principal (provisório).',
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LinhaPessoa extends StatelessWidget {
  const _LinhaPessoa({
    required this.pessoa,
    required this.acoes,
    required this.ativo,
    required this.aoEscolher,
  });

  final PessoaFichaDto pessoa;
  final List<AcaoPessoa> acoes;
  final bool ativo;
  final ValueChanged<AcaoPessoa> aoEscolher;

  @override
  Widget build(BuildContext context) {
    final papel = rotuloPapel(pessoa.papel, principal: pessoa.principal);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        title: Text(pessoa.nome),
        subtitle: Text(pessoa.provisorio ? '$papel (provisório)' : papel),
        trailing: acoes.isEmpty
            ? null
            : PopupMenuButton<AcaoPessoa>(
                enabled: ativo,
                tooltip: 'Ações para ${pessoa.nome}',
                onSelected: aoEscolher,
                itemBuilder: (context) => [
                  for (final acao in acoes)
                    PopupMenuItem(value: acao, child: Text(acao.rotulo)),
                ],
              ),
      ),
    );
  }
}
