import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/provedores.dart';
import 'avisos_locais_controller.dart';

/// Texto do aviso de notificações desativadas (RF-062, acordado com a I068 e a I070).
const String textoNotificacoesDesativadas =
    'Os avisos de toma não vão aparecer porque as notificações da aplicação estão desativadas.';

/// Texto do aviso de alarmes exatos não autorizados (RNF-041).
const String textoAlarmesExatos =
    'Os avisos de toma podem chegar alguns minutos atrasados porque os alarmes exatos não estão autorizados.';

/// Envolve as rotas da aplicação: arranca os avisos locais, sincroniza quando a aplicação volta ao primeiro plano
/// e, com sessão, mostra no topo do ecrã o aviso das autorizações em falta (RF-062). O aviso tem «Ativar avisos»,
/// que pede a autorização só nesse toque, e pode ser fechado até à próxima abertura da aplicação.
class ObservadorAvisosLocais extends ConsumerStatefulWidget {
  /// Cria o observador sobre o [child] (as rotas).
  const ObservadorAvisosLocais({super.key, required this.child});

  /// Rotas da aplicação.
  final Widget child;

  @override
  ConsumerState<ObservadorAvisosLocais> createState() =>
      _ObservadorAvisosLocaisState();
}

class _ObservadorAvisosLocaisState extends ConsumerState<ObservadorAvisosLocais>
    with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState estado) {
    if (estado == AppLifecycleState.resumed) {
      unawaited(ref.read(avisosLocaisControllerProvider.notifier).aoAbrir());
    }
  }

  @override
  Widget build(BuildContext context) {
    final avisos = ref.watch(avisosLocaisControllerProvider);
    final comSessao = ref.watch(
      sessaoControllerProvider.select((sessao) => sessao.autenticada),
    );
    final String? texto;
    if (!comSessao) {
      texto = null;
    } else if (avisos.mostrarNotificacoesDesativadas) {
      texto = textoNotificacoesDesativadas;
    } else if (avisos.mostrarAlarmesExatos) {
      texto = textoAlarmesExatos;
    } else {
      texto = null;
    }
    if (texto == null) {
      return widget.child;
    }
    final controlador = ref.read(avisosLocaisControllerProvider.notifier);
    return Column(
      children: [
        _AvisoAutorizacoes(
          texto: texto,
          ativar: controlador.pedirPermissao,
          fechar: controlador.fecharAviso,
        ),
        Expanded(
          // As rotas ficam num contentor de semântica próprio: o `BlockSemantics` das rotas esconderia do leitor de
          // ecrã tudo o que é desenhado antes delas no mesmo contentor, incluindo este aviso.
          child: Semantics(
            container: true,
            child: MediaQuery.removePadding(
              context: context,
              removeTop: true,
              child: widget.child,
            ),
          ),
        ),
      ],
    );
  }
}

class _AvisoAutorizacoes extends StatelessWidget {
  const _AvisoAutorizacoes({
    required this.texto,
    required this.ativar,
    required this.fechar,
  });

  final String texto;
  final VoidCallback ativar;
  final VoidCallback fechar;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: const Color(0xFFFFF3D6),
      child: SafeArea(
        bottom: false,
        child: Semantics(
          liveRegion: true,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 8, 4, 4),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Padding(
                  padding: EdgeInsets.only(top: 12),
                  child: Icon(
                    Icons.notifications_off_outlined,
                    color: Colors.black87,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Padding(
                        padding: const EdgeInsets.only(top: 12),
                        child: Text(
                          texto,
                          style: const TextStyle(color: Colors.black87),
                        ),
                      ),
                      TextButton(
                        onPressed: ativar,
                        child: const Text('Ativar avisos'),
                      ),
                    ],
                  ),
                ),
                // Sem `tooltip`: este aviso fica fora do `Navigator`, onde não há `Overlay`.
                Semantics(
                  container: true,
                  label: 'Fechar o aviso',
                  button: true,
                  excludeSemantics: true,
                  child: IconButton(
                    onPressed: fechar,
                    icon: const Icon(Icons.close, color: Colors.black87),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
