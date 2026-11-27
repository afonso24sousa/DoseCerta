import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/provedores.dart';
import '../../models/notificacao_dto.dart';

/// Filtro do histórico: datas `AAAA-MM-DD` (OP-51); `null` não limita.
typedef FiltroNotificacoes = ({String? de, String? ate});

/// Notificações recebidas pelo Utilizador para E25 (OP-51; RF-140), da mais recente para a mais antiga. São pedidas
/// de novo ao abrir o ecrã e ao puxar para atualizar: não há notificações push.
final notificacoesProvider = FutureProvider.autoDispose
    .family<List<NotificacaoDto>, FiltroNotificacoes>((ref, filtro) async {
      final notificacoes = await ref
          .read(notificacoesRepositorioProvider)
          .listar(de: filtro.de, ate: filtro.ate);
      return [...notificacoes]..sort(
        (a, b) =>
            DateTime.parse(b.criadaEm).compareTo(DateTime.parse(a.criadaEm)),
      );
    });
