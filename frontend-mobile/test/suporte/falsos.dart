import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/core/relogio.dart';
import 'package:dosecerta/core/sessao_store.dart';
import 'package:dosecerta/data/auth_repositorio.dart';
import 'package:dosecerta/data/conta_repositorio.dart';
import 'package:dosecerta/models/conta_criada_dto.dart';
import 'package:dosecerta/models/nova_conta_dto.dart';
import 'package:dosecerta/models/sessao_dto.dart';
import 'package:dosecerta/models/utilizador_dto.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Relógio fixo, para testar a expiração da sessão (RNF-009).
class RelogioFixo implements Relogio {
  RelogioFixo(this.instante);

  DateTime instante;

  @override
  DateTime agora() => instante;
}

/// Armazenamento da sessão em memória.
class SessaoStoreEmMemoria implements SessaoStore {
  SessaoGuardada? guardada;
  int limpezas = 0;

  @override
  FlutterSecureStorage get armazenamento => throw UnimplementedError();

  @override
  Future<SessaoGuardada?> ler() async => guardada;

  @override
  Future<void> guardar(SessaoGuardada sessao) async => guardada = sessao;

  @override
  Future<void> limpar() async {
    guardada = null;
    limpezas++;
  }
}

/// Repositório de autenticação falso: cada operação devolve ou lança o que o teste indicar.
class AuthRepositorioFalso implements AuthRepositorio {
  Object? erroRegisto;
  Object? erroLogin;
  Object? erroLogout;
  NovaContaDto? ultimoRegisto;
  int logouts = 0;
  SessaoDto sessao = const SessaoDto(
    token: 'a.eyJzdWIiOiJ1dGlsaXphZG9yLTEifQ.c',
    tipo: 'Bearer',
    expiraEm: '2026-11-09T20:00:00+00:00',
  );

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<ContaCriadaDto> registar(NovaContaDto dados) async {
    ultimoRegisto = dados;
    if (erroRegisto != null) {
      throw erroRegisto!;
    }
    return ContaCriadaDto(
      id: 'id-1',
      nome: dados.nome,
      email: dados.email,
      convitesPendentes: 2,
    );
  }

  @override
  Future<SessaoDto> iniciarSessao(String email, String palavraPasse) async {
    if (erroLogin != null) {
      throw erroLogin!;
    }
    return sessao;
  }

  @override
  Future<void> terminarSessao() async {
    logouts++;
    if (erroLogout != null) {
      throw erroLogout!;
    }
  }
}

/// Repositório da conta falso.
class ContaRepositorioFalso implements ContaRepositorio {
  UtilizadorDto conta = const UtilizadorDto(
    id: 'id-1',
    nome: 'Ana Fictícia',
    email: 'ana@exemplo.pt',
  );
  Object? erroAtualizar;
  Object? erroPalavraPasse;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<UtilizadorDto> obter() async => conta;

  @override
  Future<UtilizadorDto> atualizar(String nome, String email) async {
    if (erroAtualizar != null) {
      throw erroAtualizar!;
    }
    return conta = UtilizadorDto(id: conta.id, nome: nome, email: email);
  }

  @override
  Future<void> alterarPalavraPasse(String atual, String nova) async {
    if (erroPalavraPasse != null) {
      throw erroPalavraPasse!;
    }
  }
}
