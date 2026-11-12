import '../core/api_client.dart';
import '../models/conta_criada_dto.dart';
import '../models/nova_conta_dto.dart';
import '../models/sessao_dto.dart';

/// Criação de conta, início e fim de sessão (OP-01, OP-02 e OP-03). Sem regras de negócio: forma o pedido,
/// chama a API e devolve o DTO.
class AuthRepositorio {
  /// Cria o repositório sobre o [api].
  const AuthRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-01: `POST /auth/registo`. O registo não inicia sessão.
  Future<ContaCriadaDto> registar(NovaContaDto dados) async {
    final corpo = await api.post('/auth/registo', corpo: dados.toJson());
    return ContaCriadaDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-02: `POST /auth/login`.
  Future<SessaoDto> iniciarSessao(String email, String palavraPasse) async {
    final corpo = await api.post(
      '/auth/login',
      corpo: {'email': email, 'palavraPasse': palavraPasse},
    );
    return SessaoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-03: `POST /auth/logout`. O servidor deixa de aceitar o token (RF-016).
  Future<void> terminarSessao() async {
    await api.post('/auth/logout');
  }
}
