import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../models/registo_dto.dart';
import '../../models/tipo_registo.dart';
import '../../shared/banner_erro.dart';
import '../../shared/menu_separadores_ficha.dart';
import 'registos_controller.dart';

/// Ecrã E23 Ocorrências e notas (protótipo de baixa fidelidade v02): as ocorrências da ficha, para Cuidadores e
/// Familiares (RF-094), e, só para o Cuidador, o separador Notas (RF-095), da mais recente para a mais antiga, com o
/// autor e o momento do registo e, nas ocorrências, a data e hora em que aconteceu (RF-092). Atualiza ao puxar para
/// baixo, para mostrar os registos dos outros Cuidadores (RF-093). Não há alteração nem eliminação (RF-090,
/// RF-091); o botão «Novo registo» só aparece ao Cuidador numa ficha ativa (RF-048, RF-120).
class RegistosPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId].
  const RegistosPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  /// Apresenta um instante da API como `dd/mm/aaaa hh:mm` (RNF-029).
  static String dataHora(String iso) =>
      '${FormatadorDataHora.apresentarData(iso)} '
      '${FormatadorDataHora.apresentarHora(iso)}';

  @override
  ConsumerState<RegistosPage> createState() => _RegistosPageState();
}

class _RegistosPageState extends ConsumerState<RegistosPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(registosControllerProvider(widget.fichaId).notifier)
          .carregar(),
    );
  }

  void _mudar(SeparadorFicha separador) {
    final fichaId = widget.fichaId;
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

  /// Abre E24 com o tipo do separador aberto e, guardado o registo, volta a pedir os registos e abre o separador
  /// do tipo registado.
  Future<void> _novo(TipoRegisto tipo) async {
    final registo = await context.push<RegistoDto>(
      AppRouter.novoRegisto(widget.fichaId, tipo: tipo),
    );
    if (registo == null || !mounted) {
      return;
    }
    final controlador = ref.read(
      registosControllerProvider(widget.fichaId).notifier,
    );
    controlador.mudarSeparador(registo.tipo);
    await controlador.carregar();
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(registosControllerProvider(widget.fichaId));
    final controlador = ref.read(
      registosControllerProvider(widget.fichaId).notifier,
    );
    final ocorrencias = estado.separador == TipoRegisto.ocorrencia;
    return Scaffold(
      appBar: AppBar(title: const Text('Registos')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controlador.carregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              MenuSeparadoresFicha(
                ativo: SeparadorFicha.registos,
                mudar: _mudar,
              ),
              const SizedBox(height: 16),
              if (acesso != null && !acesso.ativa)
                BannerErro(
                  MensagensErro.catalogo['FICHA_ARQUIVADA']!,
                  informativo: true,
                ),
              if (estado.comNotas) ...[
                SegmentedButton<TipoRegisto>(
                  segments: const [
                    ButtonSegment(
                      value: TipoRegisto.ocorrencia,
                      label: Text('Ocorrências'),
                    ),
                    ButtonSegment(
                      value: TipoRegisto.nota,
                      label: Text('Notas'),
                    ),
                  ],
                  selected: {estado.separador},
                  showSelectedIcon: false,
                  onSelectionChanged: (escolha) =>
                      controlador.mudarSeparador(escolha.single),
                ),
                const SizedBox(height: 16),
              ],
              if (acesso != null && acesso.podeRegistar) ...[
                FilledButton.icon(
                  onPressed: () => _novo(estado.separador),
                  icon: const Icon(Icons.add),
                  label: const Text('Novo registo'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.erro != null) ...[
                BannerErro(estado.erro!),
                OutlinedButton(
                  onPressed: estado.aCarregar ? null : controlador.carregar,
                  child: const Text('Tentar novamente'),
                ),
                const SizedBox(height: 16),
              ],
              if (estado.aCarregar && !estado.carregado)
                const Padding(
                  padding: EdgeInsets.only(bottom: 16),
                  child: LinearProgressIndicator(),
                ),
              if (estado.carregado && estado.registos.isEmpty)
                Text(
                  ocorrencias
                      ? 'Ainda não há ocorrências registadas nesta ficha.'
                      : 'Ainda não há notas registadas nesta ficha.',
                ),
              for (final registo in estado.registos)
                _CartaoRegisto(registo: registo),
            ],
          ),
        ),
      ),
    );
  }
}

/// Um registo, só para consulta: a descrição, a data e hora em que aconteceu (ocorrências) e o autor e o momento
/// do registo (RF-092).
class _CartaoRegisto extends StatelessWidget {
  const _CartaoRegisto({required this.registo});

  final RegistoDto registo;

  @override
  Widget build(BuildContext context) {
    final ocorridaEm = registo.ocorridaEm;
    final secundario = Theme.of(context).textTheme.bodySmall;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(registo.descricao),
            const SizedBox(height: 8),
            if (ocorridaEm != null)
              Text(
                'Aconteceu em ${RegistosPage.dataHora(ocorridaEm)}',
                style: secundario,
              ),
            Text(
              'Registada em ${RegistosPage.dataHora(registo.criadaEm)} '
              'por ${registo.autor.nome}',
              style: secundario,
            ),
          ],
        ),
      ),
    );
  }
}
