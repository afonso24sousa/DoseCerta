import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/notificacoes_repositorio.dart';
import 'package:dosecerta/models/estado_envio_email.dart';
import 'package:dosecerta/models/notificacao_dto.dart';
import 'package:dosecerta/models/tipo_notificacao.dart';

/// Notificação de exemplo (dados fictícios da documentação da API, OP-51).
NotificacaoDto notificacaoExemplo({
  String id = 'n-1',
  TipoNotificacao tipo = TipoNotificacao.tomaEmAtraso,
  String mensagem = 'A toma de Metformina de Maria Fictícia Santos das 08:00 não foi confirmada.',
  String nomeFicha = 'Maria Fictícia Santos',
  String? medicamento = 'Metformina',
  String? previstaEm = '2026-10-07T08:00:00+01:00',
  String criadaEm = '2026-10-07T09:00:00+01:00',
  EstadoEnvioEmail email = EstadoEnvioEmail.enviado,
}) {
  return NotificacaoDto(
    id: id,
    tipo: tipo,
    mensagem: mensagem,
    fichaId: 'f-1',
    nomeFicha: nomeFicha,
    medicamento: medicamento,
    previstaEm: previstaEm,
    criadaEm: criadaEm,
    email: email,
  );
}

/// Repositório de notificações falso: guarda os pedidos e devolve ou lança o que o teste indicar.
class NotificacoesRepositorioFalso implements NotificacoesRepositorio {
  List<NotificacaoDto> notificacoes = [];
  Object? erro;
  int pedidos = 0;
  String? ultimoDe;
  String? ultimoAte;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<NotificacaoDto>> listar({String? de, String? ate}) async {
    pedidos++;
    ultimoDe = de;
    ultimoAte = ate;
    if (erro != null) {
      throw erro!;
    }
    return notificacoes;
  }
}
