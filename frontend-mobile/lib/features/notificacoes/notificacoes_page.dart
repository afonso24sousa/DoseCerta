import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/estado_envio_email.dart';
import '../../models/notificacao_dto.dart';
import '../../models/tipo_notificacao.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_data.dart';
import 'notificacoes_controller.dart';

/// Ecrã E25 Notificações: histórico das notificações recebidas pelo Utilizador, de qualquer papel (RF-140), com o
/// tipo (texto e ícone), a ficha, a mensagem, o medicamento e a hora prevista nas tomas e o estado do e-mail
/// (RF-119). Atualiza ao abrir e ao puxar para baixo, sem push; o filtro por datas é validado antes do pedido.
class NotificacoesPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const NotificacoesPage({super.key});

  @override
  ConsumerState<NotificacoesPage> createState() => _NotificacoesPageState();
}

class _NotificacoesPageState extends ConsumerState<NotificacoesPage> {
  final TextEditingController _de = TextEditingController();
  final TextEditingController _ate = TextEditingController();
  FiltroNotificacoes _filtro = (de: null, ate: null);
  String? _erroDe;
  String? _erroAte;
  bool? _visivel;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // A barra inferior mantém o ramo montado, fora de cena e com o TickerMode desligado; voltar ao separador
    // conta como abrir o ecrã, por isso o histórico é pedido de novo (RF-140).
    final visivel = TickerMode.valuesOf(context).enabled;
    if (visivel && _visivel == false) {
      // Depois do frame: invalidar durante a construção marcaria o ecrã como sujo a meio do build.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          ref.invalidate(notificacoesProvider(_filtro));
        }
      });
    }
    _visivel = visivel;
  }

  @override
  void dispose() {
    _de.dispose();
    _ate.dispose();
    super.dispose();
  }

  /// Lê o campo: vazio dá `null`; uma data inválida devolve o erro do campo.
  (String?, String?) _ler(TextEditingController campo, String rotulo) {
    final texto = campo.text.trim();
    if (texto.isEmpty) {
      return (null, null);
    }
    final iso = FormatadorDataHora.paraIsoData(texto);
    return iso == null
        ? (null, '$rotulo: indique uma data válida no formato dd/mm/aaaa.')
        : (iso, null);
  }

  void _filtrar() {
    final (de, erroDe) = _ler(_de, 'De');
    var (ate, erroAte) = _ler(_ate, 'Até');
    // A API recusa `ate` anterior a `de` com 400; valida-se antes do pedido (RNF-028).
    if (de != null && ate != null && ate.compareTo(de) < 0) {
      erroAte = 'Até: tem de ser igual ou posterior à data De.';
    }
    setState(() {
      _erroDe = erroDe;
      _erroAte = erroAte;
      if (erroDe == null && erroAte == null) {
        _filtro = (de: de, ate: ate);
      }
    });
  }

  void _limpar() {
    _de.clear();
    _ate.clear();
    setState(() {
      _erroDe = null;
      _erroAte = null;
      _filtro = (de: null, ate: null);
    });
  }

  @override
  Widget build(BuildContext context) {
    final lista = ref.watch(notificacoesProvider(_filtro));
    final hoje = ref.read(relogioProvider).agora();
    Future<void> recarregar() =>
        ref.refresh(notificacoesProvider(_filtro).future);

    final List<Widget> conteudo;
    switch (lista) {
      case AsyncData(:final value) when value.isEmpty:
        conteudo = [
          Text(
            _filtro.de == null && _filtro.ate == null
                ? 'Não há notificações.'
                : 'Não há notificações neste período.',
          ),
        ];
      case AsyncData(:final value):
        conteudo = [for (final n in value) _ItemNotificacao(n)];
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
      appBar: AppBar(title: const Text('Notificações')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: recarregar,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              CampoData(
                rotulo: 'De',
                controlador: _de,
                ultimaData: hoje,
                primeiraData: DateTime(2026),
                erro: _erroDe,
              ),
              CampoData(
                rotulo: 'Até',
                controlador: _ate,
                ultimaData: hoje,
                primeiraData: DateTime(2026),
                erro: _erroAte,
              ),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  FilledButton.icon(
                    onPressed: _filtrar,
                    icon: const Icon(Icons.filter_alt_outlined),
                    label: const Text('Filtrar'),
                  ),
                  OutlinedButton(
                    onPressed: _limpar,
                    child: const Text('Limpar filtro'),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              ...conteudo,
              const SizedBox(height: 8),
              Text(
                'Atualizado ao abrir ou atualizar o ecrã.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Uma notificação: o tipo vai em texto e ícone, nunca só pela cor.
class _ItemNotificacao extends StatelessWidget {
  const _ItemNotificacao(this.notificacao);

  final NotificacaoDto notificacao;

  static IconData _icone(TipoNotificacao tipo) => switch (tipo) {
    TipoNotificacao.conviteRecebido => Icons.mail_outline,
    TipoNotificacao.tomaEmAtraso => Icons.medication_outlined,
    TipoNotificacao.tomaGraveFamiliares => Icons.warning_amber_outlined,
    TipoNotificacao.novaOcorrencia => Icons.report_outlined,
    TipoNotificacao.proximoCompromisso => Icons.event_outlined,
  };

  static IconData _iconeEmail(EstadoEnvioEmail estado) => switch (estado) {
    EstadoEnvioEmail.enviado => Icons.mark_email_read_outlined,
    EstadoEnvioEmail.pendente => Icons.schedule_send_outlined,
    EstadoEnvioEmail.naoEntregue => Icons.error_outline,
  };

  @override
  Widget build(BuildContext context) {
    final n = notificacao;
    final texto = Theme.of(context).textTheme;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(_icone(n.tipo)),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(n.tipo.rotulo, style: texto.titleSmall),
                  Text(
                    '${FormatadorDataHora.apresentarDataHora(n.criadaEm)} · ${n.nomeFicha}',
                    style: texto.bodySmall,
                  ),
                  const SizedBox(height: 4),
                  Text(n.mensagem),
                  if (n.medicamento != null && n.previstaEm != null)
                    Text(
                      'Medicamento: ${n.medicamento} · '
                      'prevista às ${FormatadorDataHora.apresentarHora(n.previstaEm!)}',
                    ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Icon(_iconeEmail(n.email), size: 18),
                      const SizedBox(width: 4),
                      Flexible(
                        child: Text(
                          n.email.rotulo,
                          style: n.email == EstadoEnvioEmail.naoEntregue
                              ? const TextStyle(fontWeight: FontWeight.bold)
                              : null,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
