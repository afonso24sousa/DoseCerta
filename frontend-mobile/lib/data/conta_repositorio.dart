import '../core/api_client.dart';
import '../models/utilizador_dto.dart';

/// Dados da conta e palavra-passe (OP-04, OP-05 e OP-06).
class ContaRepositorio {
  /// Cria o repositório sobre o [api].
  const ContaRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-04: `GET /conta`.
  Future<UtilizadorDto> obter() async {
    final corpo = await api.get('/conta');
    return UtilizadorDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-05: `PUT /conta`.
  Future<UtilizadorDto> atualizar(String nome, String email) async {
    final corpo = await api.put(
      '/conta',
      corpo: {'nome': nome, 'email': email},
    );
    return UtilizadorDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-06: `PUT /conta/palavra-passe`.
  Future<void> alterarPalavraPasse(String atual, String nova) async {
    await api.put(
      '/conta/palavra-passe',
      corpo: {'palavraPasseAtual': atual, 'palavraPasseNova': nova},
    );
  }
}
