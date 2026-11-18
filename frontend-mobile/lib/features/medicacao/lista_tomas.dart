import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../models/estado_toma.dart';
import '../../models/toma_dto.dart';
import '../../shared/banner_erro.dart';
import 'apresentacao_toma.dart';
import 'lista_tomas_controller.dart';

/// Calendário de tomas de uma ficha, no separador «Calendário de tomas» do E15 (contrato de interface da F006;
/// RF-063 a RF-065, RF-067, RF-068, RF-105, RF-048, RF-120). Mostra 7 dias de cada vez, agrupados por dia, e deixa o
/// Cuidador confirmar cada toma `Pendente` com um só toque. Não é deslizável: fica dentro da lista do E15.
class ListaTomas extends ConsumerWidget {
  /// Cria o calendário da ficha [fichaId].
  const ListaTomas({super.key, required this.fichaId});

  /// Identificador da ficha.
  final String fichaId;

  /// Aviso ao Familiar (RF-048).
  static const String textoFamiliar =
      'Como Familiar, pode consultar as tomas, mas não as confirmar.';

  /// Aviso da ficha arquivada (RF-120, RF-121).
  static const String textoArquivada =
      'Ficha arquivada: não são geradas tomas novas. Ainda pode confirmar as tomas que ficaram por confirmar.';

  Future<void> _confirmar(
    BuildContext context,
    ListaTomasController controlador,
    String tomaId,
  ) async {
    final mensageiro = ScaffoldMessenger.of(context);
    final estado = await controlador.confirmar(tomaId);
    if (estado != null) {
      mensageiro.showSnackBar(
        SnackBar(content: Text(ApresentacaoToma.mensagemConfirmada(estado))),
      );
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final acesso = ref.watch(fichaAcessoProvider(fichaId)).value;
    final estado = ref.watch(listaTomasProvider(fichaId));
    final controlador = ref.read(listaTomasProvider(fichaId).notifier);
    // O botão aparece ao Cuidador, também numa ficha arquivada: a OP-33 não recusa por a ficha estar arquivada e as
    // tomas que ficaram por confirmar ainda podem ser confirmadas (contrato, secção 7).
    final podeConfirmar = acesso != null && acesso.ficha.souCuidador;
    final tema = Theme.of(context);

    final List<Widget> conteudo;
    final tomas = estado.tomas;
    if (estado.erroCarregar != null) {
      conteudo = [
        BannerErro(estado.erroCarregar!),
        OutlinedButton(
          onPressed: controlador.carregar,
          child: const Text('Tentar novamente'),
        ),
      ];
    } else if (tomas == null || estado.aCarregar) {
      conteudo = [
        const Text('A carregar as tomas…'),
        const SizedBox(height: 8),
        const LinearProgressIndicator(),
      ];
    } else if (tomas.isEmpty) {
      conteudo = [const Text('Não há tomas previstas neste período.')];
    } else {
      conteudo = [
        for (final dia in _porDia(tomas).entries) ...[
          Semantics(
            header: true,
            child: Text(
              FormatadorDataHora.apresentarData(dia.key),
              style: tema.textTheme.titleMedium,
            ),
          ),
          const SizedBox(height: 8),
          for (final toma in dia.value)
            _CartaoToma(
              toma: toma,
              podeConfirmar: podeConfirmar,
              aConfirmar: estado.aConfirmar.contains(toma.id),
              confirmar: () => _confirmar(context, controlador, toma.id),
            ),
          const SizedBox(height: 8),
        ],
      ];
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Semantics(
          header: true,
          child: Text(
            'Tomas de ${FormatadorDataHora.deData(estado.inicio)} a '
            '${FormatadorDataHora.deData(estado.fim)}',
            style: tema.textTheme.titleMedium,
          ),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            OutlinedButton.icon(
              onPressed: controlador.periodoAnterior,
              icon: const Icon(Icons.chevron_left),
              label: const Text('Período anterior'),
            ),
            OutlinedButton.icon(
              onPressed: controlador.periodoSeguinte,
              icon: const Icon(Icons.chevron_right),
              label: const Text('Período seguinte'),
            ),
          ],
        ),
        const SizedBox(height: 16),
        if (acesso != null && !acesso.ficha.souCuidador)
          const BannerErro(textoFamiliar, informativo: true),
        if (acesso != null && !acesso.ativa)
          const BannerErro(textoArquivada, informativo: true),
        if (estado.erro != null) BannerErro(estado.erro!),
        ...conteudo,
      ],
    );
  }

  /// Agrupa as tomas pelo dia de Lisboa (os 10 primeiros caracteres de `previstaEm`), pela ordem recebida. Os dias
  /// sem tomas não aparecem.
  static Map<String, List<TomaDto>> _porDia(List<TomaDto> tomas) {
    final dias = <String, List<TomaDto>>{};
    for (final toma in tomas) {
      dias.putIfAbsent(toma.previstaEm.substring(0, 10), () => []).add(toma);
    }
    return dias;
  }
}

class _CartaoToma extends StatelessWidget {
  const _CartaoToma({
    required this.toma,
    required this.podeConfirmar,
    required this.aConfirmar,
    required this.confirmar,
  });

  final TomaDto toma;
  final bool podeConfirmar;
  final bool aConfirmar;
  final VoidCallback confirmar;

  @override
  Widget build(BuildContext context) {
    final confirmacao = ApresentacaoToma.confirmacao(toma);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        title: Text(
          '${FormatadorDataHora.apresentarHora(toma.previstaEm)}  '
          '${toma.medicamento}',
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${toma.dose} · ${toma.grauImportancia.valor}'),
            const SizedBox(height: 4),
            Row(
              children: [
                Icon(ApresentacaoToma.icone(toma.estado), size: 20),
                const SizedBox(width: 6),
                Flexible(child: Text(ApresentacaoToma.texto(toma.estado))),
              ],
            ),
            if (confirmacao != null) ...[
              const SizedBox(height: 4),
              Text(confirmacao),
            ],
            if (podeConfirmar && toma.estado == EstadoToma.pendente) ...[
              const SizedBox(height: 8),
              FilledButton(
                onPressed: aConfirmar ? null : confirmar,
                child: const Text('Confirmar toma'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
