import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/convites_repositorio.dart';
import 'package:dosecerta/data/pessoas_repositorio.dart';
import 'package:dosecerta/models/convite_dto.dart';
import 'package:dosecerta/models/estado_convite.dart';
import 'package:dosecerta/models/novo_convite_dto.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/models/pessoa_ficha_dto.dart';

/// Convite pendente de exemplo (dados fictícios da documentação da API).
ConviteDto conviteExemplo({
  String id = 'c-1',
  String nomeFicha = 'Maria Fictícia Santos',
  Papel papel = Papel.cuidador,
  bool paraPrincipal = false,
  String convidadoPor = 'Ana Fictícia',
}) {
  return ConviteDto(
    id: id,
    fichaId: 'f-$id',
    nomeFicha: nomeFicha,
    papelOferecido: papel,
    paraPrincipal: paraPrincipal,
    convidadoPor: convidadoPor,
    estado: EstadoConvite.pendente,
    criadoEm: '2026-11-16T09:00:00+00:00',
  );
}

/// Repositório dos convites falso: guarda os pedidos e devolve ou lança o que o teste indicar.
class ConvitesRepositorioFalso implements ConvitesRepositorio {
  /// Convites pendentes recebidos pelo Utilizador (OP-20).
  final List<ConviteDto> recebidos = [];

  Object? erroListar;
  Object? erroResponder;
  Object? erroConvidar;

  int listagens = 0;
  final List<String> respostas = [];
  final List<(String fichaId, NovoConviteDto dados)> enviados = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<ConviteDto> convidar(String fichaId, NovoConviteDto dados) async {
    enviados.add((fichaId, dados));
    if (erroConvidar != null) {
      throw erroConvidar!;
    }
    return conviteExemplo(
      id: 'c-novo',
      papel: dados.papel,
      paraPrincipal: dados.paraPrincipal,
    );
  }

  @override
  Future<List<ConviteDto>> listarRecebidos() async {
    listagens++;
    if (erroListar != null) {
      throw erroListar!;
    }
    return List.of(recebidos);
  }

  @override
  Future<PessoaFichaDto> aceitar(String conviteId) async {
    respostas.add('aceitar $conviteId');
    if (erroResponder != null) {
      throw erroResponder!;
    }
    final convite = recebidos.firstWhere((c) => c.id == conviteId);
    recebidos.remove(convite);
    return PessoaFichaDto(
      utilizadorId: 'u-eu',
      nome: 'Eu Fictício',
      email: 'eu@exemplo.pt',
      papel: convite.papelOferecido,
      principal: convite.paraPrincipal,
      provisorio: false,
    );
  }

  @override
  Future<void> recusar(String conviteId) async {
    respostas.add('recusar $conviteId');
    if (erroResponder != null) {
      throw erroResponder!;
    }
    recebidos.removeWhere((c) => c.id == conviteId);
  }
}

/// Pessoa da ficha de exemplo (dados fictícios da documentação da API).
PessoaFichaDto pessoaExemplo({
  String id = 'u-1',
  String nome = 'Ana Fictícia',
  Papel papel = Papel.cuidador,
  bool principal = false,
  bool provisorio = false,
}) {
  return PessoaFichaDto(
    utilizadorId: id,
    nome: nome,
    email: '$id@exemplo.pt',
    papel: papel,
    principal: principal,
    provisorio: provisorio,
  );
}

/// Repositório das pessoas da ficha falso: aplica as ações à lista [pessoas] ou lança [erroAcao].
class PessoasRepositorioFalso implements PessoasRepositorio {
  /// Pessoas da ficha (OP-18).
  final List<PessoaFichaDto> pessoas = [];

  Object? erroListar;
  Object? erroAcao;

  int listagens = 0;
  final List<String> operacoes = [];

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<PessoaFichaDto>> listar(String fichaId) async {
    listagens++;
    if (erroListar != null) {
      throw erroListar!;
    }
    return List.of(pessoas);
  }

  Future<void> _registar(String operacao) async {
    operacoes.add(operacao);
    if (erroAcao != null) {
      throw erroAcao!;
    }
  }

  PessoaFichaDto _substituir(
    String id,
    PessoaFichaDto Function(PessoaFichaDto) mudar,
  ) {
    final i = pessoas.indexWhere((p) => p.utilizadorId == id);
    final nova = mudar(pessoas[i]);
    pessoas[i] = nova;
    return nova;
  }

  @override
  Future<PessoaFichaDto> promover(String fichaId, String utilizadorId) async {
    await _registar('promover $utilizadorId');
    return _substituir(
      utilizadorId,
      (p) => pessoaExemplo(id: p.utilizadorId, nome: p.nome),
    );
  }

  @override
  Future<PessoaFichaDto> despromover(
    String fichaId,
    String utilizadorId,
  ) async {
    await _registar('despromover $utilizadorId');
    return _substituir(
      utilizadorId,
      (p) => pessoaExemplo(
        id: p.utilizadorId,
        nome: p.nome,
        papel: Papel.familiar,
      ),
    );
  }

  @override
  Future<void> remover(String fichaId, String utilizadorId) async {
    await _registar('remover $utilizadorId');
    pessoas.removeWhere((p) => p.utilizadorId == utilizadorId);
  }

  @override
  Future<PessoaFichaDto> transferirPrincipal(
    String fichaId,
    String utilizadorId,
  ) async {
    await _registar('transferir $utilizadorId');
    for (final p in List.of(pessoas)) {
      if (p.principal) {
        _substituir(
          p.utilizadorId,
          (a) => pessoaExemplo(id: a.utilizadorId, nome: a.nome),
        );
      }
    }
    return _substituir(
      utilizadorId,
      (p) => pessoaExemplo(id: p.utilizadorId, nome: p.nome, principal: true),
    );
  }
}
