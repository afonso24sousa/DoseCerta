import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/mensagens_erro.dart';
import '../../models/tratamento_dto.dart';
import '../../shared/banner_erro.dart';
import '../../shared/menu_separadores_ficha.dart';
import 'apresentacao_tratamento.dart';
import 'lista_tomas.dart';
import 'lista_tomas_controller.dart';
import 'medicacao_ficha_controller.dart';
import 'vista_medicacao.dart';

/// Ecrã E15 Medicação da ficha: Cuidadores e Familiares associados consultam os tratamentos em curso e os
/// terminados (RF-054, RF-057). «Registar tratamento» só aparece ao Cuidador numa ficha ativa (RF-050, RF-120); o
/// Familiar só consulta (RF-048). O separador «Calendário de tomas» mostra o `ListaTomas` (I069).
class MedicacaoFichaPage extends ConsumerWidget {
  /// Cria o ecrã da ficha [fichaId], no separador [vista].
  const MedicacaoFichaPage({
    super.key,
    required this.fichaId,
    this.vista = VistaMedicacao.tratamentos,
  });

  /// Identificador da ficha.
  final String fichaId;

  /// Separador aberto (`?vista=calendario` abre o calendário de tomas).
  final VistaMedicacao vista;

  void _mudar(BuildContext context, SeparadorFicha separador) {
    final destino = switch (separador) {
      SeparadorFicha.dados => AppRouter.ficha(fichaId),
      SeparadorFicha.medicacao => AppRouter.medicacaoFicha(fichaId),
      SeparadorFicha.agenda => AppRouter.agenda,
      SeparadorFicha.pessoas => AppRouter.pessoasFicha(fichaId),
      SeparadorFicha.registos => AppRouter.registosFicha(fichaId),
      SeparadorFicha.documentos => AppRouter.documentosFicha(fichaId),
    };
    context.go(destino);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final acesso = ref.watch(fichaAcessoProvider(fichaId)).value;
    final calendario = vista == VistaMedicacao.calendario;
    final lista = ref.watch(tratamentosFichaProvider(fichaId));
    Future<void> recarregar() => calendario
        ? ref.read(listaTomasProvider(fichaId).notifier).atualizar()
        : ref.refresh(tratamentosFichaProvider(fichaId).future);

    final List<Widget> conteudo;
    switch (lista) {
      case AsyncData(:final value):
        conteudo = [
          _Seccao(
            titulo: 'Em curso',
            vazio: 'Não há tratamentos em curso.',
            fichaId: fichaId,
            tratamentos: [
              for (final t in value)
                if (t.emCurso) t,
            ],
          ),
          _Seccao(
            titulo: 'Terminados',
            vazio: 'Não há tratamentos terminados.',
            fichaId: fichaId,
            tratamentos: [
              for (final t in value)
                if (!t.emCurso) t,
            ],
          ),
        ];
      case AsyncError(:final error):
        conteudo = [
          BannerErro(MensagensErro.traduzir(error)),
          OutlinedButton(
            onPressed: recarregar,
            child: const Text('Tentar novamente'),
          ),
        ];
      default:
        conteudo = const [LinearProgressIndicator()];
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Medicação')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: recarregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              MenuSeparadoresFicha(
                ativo: SeparadorFicha.medicacao,
                mudar: (separador) => _mudar(context, separador),
              ),
              const SizedBox(height: 16),
              SegmentedButton<VistaMedicacao>(
                segments: [
                  for (final opcao in VistaMedicacao.values)
                    ButtonSegment(value: opcao, label: Text(opcao.rotulo)),
                ],
                selected: {vista},
                showSelectedIcon: false,
                onSelectionChanged: (escolha) => context.go(
                  escolha.single == VistaMedicacao.calendario
                      ? AppRouter.calendarioTomas(fichaId)
                      : AppRouter.medicacaoFicha(fichaId),
                ),
              ),
              const SizedBox(height: 16),
              if (calendario)
                ListaTomas(fichaId: fichaId)
              else ...[
                if (acesso != null && !acesso.ativa)
                  BannerErro(
                    MensagensErro.catalogo['FICHA_ARQUIVADA']!,
                    informativo: true,
                  ),
                if (acesso != null && acesso.podeRegistar) ...[
                  FilledButton.icon(
                    onPressed: () =>
                        context.go(AppRouter.novoTratamento(fichaId)),
                    icon: const Icon(Icons.add),
                    label: const Text('Registar tratamento'),
                  ),
                  const SizedBox(height: 16),
                ],
                ...conteudo,
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _Seccao extends StatelessWidget {
  const _Seccao({
    required this.titulo,
    required this.vazio,
    required this.fichaId,
    required this.tratamentos,
  });

  final String titulo;
  final String vazio;
  final String fichaId;
  final List<TratamentoDto> tratamentos;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Semantics(
            header: true,
            child: Text(titulo, style: Theme.of(context).textTheme.titleMedium),
          ),
          const SizedBox(height: 8),
          if (tratamentos.isEmpty) Text(vazio),
          for (final tratamento in tratamentos)
            Card(
              margin: const EdgeInsets.only(bottom: 8),
              child: ListTile(
                title: Text(ApresentacaoTratamento.titulo(tratamento)),
                subtitle: Text(
                  '${tratamento.grauImportancia.valor} · '
                  '${ApresentacaoTratamento.horarios(tratamento.horarios)}\n'
                  '${ApresentacaoTratamento.periodo(tratamento)}',
                ),
                isThreeLine: true,
                trailing: const Icon(Icons.chevron_right),
                onTap: () =>
                    context.go(AppRouter.tratamento(fichaId, tratamento.id)),
              ),
            ),
        ],
      ),
    );
  }
}
