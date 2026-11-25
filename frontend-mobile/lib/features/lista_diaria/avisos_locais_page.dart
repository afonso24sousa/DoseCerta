import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../avisos_locais/avisos_locais_controller.dart';
import '../../avisos_locais/sincronizador_calendario.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/provedores.dart';
import '../../shared/banner_erro.dart';

/// Ecrã E26 Calendário sincronizado e avisos locais (protótipo de baixa fidelidade v02, página 41), só no mobile:
/// a data e hora da última sincronização, as tomas das 48 horas seguintes guardadas no dispositivo, os avisos
/// agendados e o estado das autorizações de notificações e de alarmes exatos, com «Sincronizar agora» e o pedido das
/// autorizações em falta (RF-061, RF-062, RF-069, RNF-033, RNF-041).
class AvisosLocaisPage extends ConsumerStatefulWidget {
  /// Cria o ecrã.
  const AvisosLocaisPage({super.key});

  /// Aviso quando as notificações não estão autorizadas (protótipo, E26).
  static const String notificacoesNaoAutorizadas =
      'As notificações não estão autorizadas. Ative-as nas definições do '
      'dispositivo.';

  /// Aviso quando os alarmes exatos não estão permitidos (RNF-041).
  static const String alarmesNaoPermitidos =
      'Os alarmes exatos não estão permitidos: os avisos podem chegar alguns '
      'minutos atrasados.';

  /// Aviso sem ligação (protótipo, E26; RNF-033).
  static const String semLigacao =
      'Sem ligação ao servidor. Os avisos já agendados continuam a funcionar.';

  /// Aviso quando o servidor recusou a sincronização.
  static const String falhou =
      'Não foi possível sincronizar. Os avisos já agendados continuam a '
      'funcionar.';

  @override
  ConsumerState<AvisosLocaisPage> createState() => _AvisosLocaisPageState();
}

class _AvisosLocaisPageState extends ConsumerState<AvisosLocaisPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(avisosLocaisControllerProvider.notifier).carregarEstado(),
    );
  }

  Future<void> _sincronizar() async {
    final controlador = ref.read(avisosLocaisControllerProvider.notifier);
    await controlador.sincronizar();
    if (!mounted) {
      return;
    }
    if (ref.read(avisosLocaisControllerProvider).ultimoResultado ==
        ResultadoSincronizacao.sucesso) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(content: Text('Calendário sincronizado.')),
        );
    }
  }

  static String _autorizacao(bool? valor, String sim, String nao) =>
      switch (valor) {
        true => sim,
        false => nao,
        null => 'a verificar',
      };

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(avisosLocaisControllerProvider);
    final controlador = ref.read(avisosLocaisControllerProvider.notifier);
    final sincronizadas = estado.sincronizadas;
    final agora = ref.read(relogioProvider).agora();
    final ultima = sincronizadas == null
        ? null
        : FormatadorDataHora.emLisboa(sincronizadas.ultimaSincronizacao);
    final tomas = sincronizadas?.tomas.length ?? 0;
    // Os avisos agendados são as tomas guardadas ainda por vir: é a regra do agendamento (AvisosLocaisService).
    final agendados =
        sincronizadas?.tomas
            .where((toma) => DateTime.parse(toma.previstaEm).isAfter(agora))
            .length ??
        0;
    final faltaNotificacoes = estado.notificacoesAutorizadas == false;
    final faltaAlarmes =
        !faltaNotificacoes && estado.alarmesExatosAutorizados == false;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Calendário sincronizado e avisos locais'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Semantics(
              header: true,
              child: Text(
                'Avisos de toma',
                style: Theme.of(context).textTheme.titleLarge,
              ),
            ),
            const SizedBox(height: 16),
            _Linha(
              ultima == null
                  ? 'Último calendário sincronizado: ainda não sincronizado'
                  : 'Último calendário sincronizado: '
                        '${FormatadorDataHora.deData(ultima)} '
                        '${ultima.hour.toString().padLeft(2, '0')}:'
                        '${ultima.minute.toString().padLeft(2, '0')}',
            ),
            _Linha('Tomas sincronizadas neste dispositivo: $tomas'),
            _Linha('Avisos agendados neste dispositivo: $agendados'),
            _Linha(
              'Notificações do sistema: '
              '${_autorizacao(estado.notificacoesAutorizadas, 'autorizadas', 'não autorizadas')}',
            ),
            _Linha(
              'Alarmes exatos: '
              '${_autorizacao(estado.alarmesExatosAutorizados, 'permitidos', 'não permitidos')}',
            ),
            const SizedBox(height: 8),
            if (estado.aSincronizar)
              const Padding(
                padding: EdgeInsets.only(bottom: 8),
                child: LinearProgressIndicator(),
              ),
            Align(
              alignment: Alignment.centerLeft,
              child: FilledButton(
                onPressed: estado.aSincronizar ? null : _sincronizar,
                child: const Text('Sincronizar agora'),
              ),
            ),
            const SizedBox(height: 16),
            if (faltaNotificacoes || faltaAlarmes) ...[
              BannerErro(
                faltaNotificacoes
                    ? AvisosLocaisPage.notificacoesNaoAutorizadas
                    : AvisosLocaisPage.alarmesNaoPermitidos,
              ),
              Align(
                alignment: Alignment.centerLeft,
                child: OutlinedButton(
                  onPressed: controlador.pedirPermissao,
                  child: const Text('Pedir autorização'),
                ),
              ),
              const SizedBox(height: 16),
            ],
            if (!estado.aSincronizar &&
                estado.ultimoResultado == ResultadoSincronizacao.semLigacao)
              const BannerErro(AvisosLocaisPage.semLigacao),
            if (!estado.aSincronizar &&
                estado.ultimoResultado == ResultadoSincronizacao.falhou)
              const BannerErro(AvisosLocaisPage.falhou),
            const Text(
              'Só as tomas das fichas ativas em que é Cuidador geram avisos. '
              'Os avisos tocam à hora prevista, mesmo sem ligação.',
            ),
          ],
        ),
      ),
    );
  }
}

class _Linha extends StatelessWidget {
  const _Linha(this.texto);

  final String texto;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Text(texto),
    );
  }
}
