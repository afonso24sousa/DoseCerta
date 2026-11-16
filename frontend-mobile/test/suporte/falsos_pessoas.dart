import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/convites_repositorio.dart';
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
