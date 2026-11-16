import '../core/api_client.dart';
import '../models/convite_dto.dart';
import '../models/novo_convite_dto.dart';
import '../models/pessoa_ficha_dto.dart';

/// Convites (OP-19 a OP-22). Sem regras de negócio: o convite pendente duplicado (RF-043) é recusado pelo
/// servidor com `CONVITE_PENDENTE_EXISTE`.
class ConvitesRepositorio {
  /// Cria o repositório sobre o [api].
  const ConvitesRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-19: `POST /fichas/{fichaId}/convites` (RF-040).
  Future<ConviteDto> convidar(String fichaId, NovoConviteDto dados) async {
    final corpo = await api.post(
      '/fichas/$fichaId/convites',
      corpo: dados.toJson(),
    );
    return ConviteDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-20: `GET /convites`; só os pendentes dirigidos ao Utilizador (RF-042, RF-044).
  Future<List<ConviteDto>> listarRecebidos() async {
    final corpo = await api.get('/convites');
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(ConviteDto.fromJson)
        .toList(growable: false);
  }

  /// OP-21: `POST /convites/{conviteId}/aceitacao`; devolve a nova associação (RF-045, RF-026).
  Future<PessoaFichaDto> aceitar(String conviteId) async {
    final corpo = await api.post('/convites/$conviteId/aceitacao');
    return PessoaFichaDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-22: `POST /convites/{conviteId}/recusa` (RF-046, RF-027).
  Future<void> recusar(String conviteId) async {
    await api.post('/convites/$conviteId/recusa');
  }
}
