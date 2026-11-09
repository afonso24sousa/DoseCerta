import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../models/item_dominio_dto.dart';
import '../../models/problema_api.dart';
import '../../shared/apresentacao_papel.dart';
import '../../shared/banner_erro.dart';
import '../../shared/menu_separadores_ficha.dart';
import '../../shared/por_papel.dart';

/// Ecrã E08 Ficha (protótipo de baixa fidelidade v02, página 33): dados de identificação e necessidades
/// específicas (RF-028), o papel do Utilizador e o menu da ficha. «Editar» só aparece ao Cuidador numa ficha ativa
/// (RF-029) e a gestão do estado só ao Cuidador (RF-120 a RF-123); o Familiar só consulta (RF-048). Numa ficha
/// arquivada aparece o aviso de só consulta (RF-120).
///
/// O estado do ecrã é o [FichaAcessoProvider] (OP-10), partilhado com E09, E10 e E11; não há um `FichaController`
/// próprio (divergência do modelo, registada no Pull Request).
class FichaPage extends ConsumerWidget {
  /// Cria o ecrã da ficha [fichaId].
  const FichaPage({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  /// Mensagem quando a ficha não está acessível (404 ou 403): a API não diz se a ficha existe (RNF-005).
  static String mensagemSemAcesso(Object erro) => MensagensErro.traduzir(erro);

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
    final acesso = ref.watch(fichaAcessoProvider(fichaId));
    final fornecedor = ref.read(fichaAcessoProvider(fichaId).notifier);
    return Scaffold(
      appBar: AppBar(title: const Text('Ficha')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: fornecedor.recarregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: switch (acesso) {
              AsyncData(:final value) => _conteudo(context, value),
              AsyncError(:final error) => _erro(context, error, fornecedor),
              _ => const [LinearProgressIndicator()],
            },
          ),
        ),
      ),
    );
  }

  List<Widget> _erro(
    BuildContext context,
    Object erro,
    FichaAcessoProvider fornecedor,
  ) {
    final semAcesso =
        erro is ProblemaApi &&
        (erro.estadoHttp == 404 || erro.estadoHttp == 403);
    return [
      BannerErro(mensagemSemAcesso(erro)),
      if (semAcesso)
        FilledButton(
          onPressed: () => context.go(AppRouter.fichas),
          child: const Text('Voltar às fichas'),
        )
      else
        OutlinedButton(
          onPressed: fornecedor.recarregar,
          child: const Text('Tentar novamente'),
        ),
    ];
  }

  List<Widget> _conteudo(BuildContext context, FichaAcesso acesso) {
    final ficha = acesso.ficha;
    final tema = Theme.of(context);
    return [
      Row(
        children: [
          Expanded(
            child: Text(ficha.nomeCompleto, style: tema.textTheme.titleLarge),
          ),
          const SizedBox(width: 8),
          Chip(label: Text(ficha.ativa ? 'Ativa' : 'Arquivada')),
        ],
      ),
      const SizedBox(height: 8),
      MenuSeparadoresFicha(
        ativo: SeparadorFicha.dados,
        mudar: (separador) => _mudar(context, separador),
      ),
      const SizedBox(height: 16),
      if (!ficha.ativa) BannerErro(MensagensErro.catalogo['FICHA_ARQUIVADA']!),
      _Linha(
        'Nascimento',
        FormatadorDataHora.apresentarData(ficha.dataNascimento),
      ),
      _Linha('Contacto de emergência', ficha.contactoEmergencia),
      _Linha(
        'Autonomia/mobilidade',
        ficha.grauAutonomia?.designacao ?? 'Sem indicação',
      ),
      _Linha('Condições de saúde', _lista(ficha.condicoesSaude)),
      _Linha('Alergias', _lista(ficha.alergias)),
      _Linha(
        'O meu papel',
        rotuloPapel(ficha.meuPapel, principal: ficha.souPrincipal),
      ),
      if (ficha.eProprio) const _Linha('Tipo de ficha', 'Ficha própria'),
      const SizedBox(height: 16),
      PorPapel(
        papel: acesso.papel,
        soFichaAtiva: true,
        fichaAtiva: acesso.ativa,
        filho: Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: FilledButton(
            onPressed: () => context.go(AppRouter.editarFicha(ficha.id)),
            child: const Text('Editar'),
          ),
        ),
      ),
      PorPapel(
        papel: acesso.papel,
        filho: OutlinedButton(
          onPressed: () => context.go(AppRouter.estadoFicha(ficha.id)),
          child: Text(
            ficha.ativa ? 'Arquivar ou eliminar' : 'Reativar ou eliminar',
          ),
        ),
      ),
    ];
  }

  static String _lista(List<ItemDominioDto> itens) =>
      itens.isEmpty ? 'Nenhuma' : itens.map((i) => i.designacao).join(', ');
}

class _Linha extends StatelessWidget {
  const _Linha(this.rotulo, this.valor);

  final String rotulo;
  final String valor;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text.rich(
        TextSpan(
          children: [
            TextSpan(
              text: '$rotulo: ',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            TextSpan(text: valor),
          ],
        ),
      ),
    );
  }
}
