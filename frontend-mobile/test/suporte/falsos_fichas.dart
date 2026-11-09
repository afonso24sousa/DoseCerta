import 'dart:typed_data';

import 'package:dosecerta/core/api_client.dart';
import 'package:dosecerta/data/documentos_repositorio.dart';
import 'package:dosecerta/data/dominios_repositorio.dart';
import 'package:dosecerta/data/fichas_repositorio.dart';
import 'package:dosecerta/models/dados_ficha_dto.dart';
import 'package:dosecerta/models/documento_dto.dart';
import 'package:dosecerta/models/documento_para_envio.dart';
import 'package:dosecerta/models/dominios_necessidades_dto.dart';
import 'package:dosecerta/models/estado_ficha.dart';
import 'package:dosecerta/models/ficha_dto.dart';
import 'package:dosecerta/models/ficha_resumo_dto.dart';
import 'package:dosecerta/models/item_dominio_dto.dart';
import 'package:dosecerta/models/papel.dart';
import 'package:dosecerta/shared/origem_ficheiro.dart';
import 'package:dosecerta/shared/selecionador_ficheiros.dart';
import 'package:image_picker/image_picker.dart';

/// Câmara, galeria e seletor de ficheiros falsos: devolvem [ficheiro] ou lançam [erro].
class FonteFicheirosFalsa implements FonteFicheiros {
  XFile? ficheiro = XFile.fromData(
    Uint8List.fromList([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31]),
  );
  String nome = 'receita.pdf';
  Object? erro;
  final List<OrigemFicheiro> pedidas = [];

  @override
  Future<FicheiroDaOrigem?> escolher(OrigemFicheiro origem) async {
    pedidas.add(origem);
    if (erro != null) {
      throw erro!;
    }
    final escolhido = ficheiro;
    return escolhido == null ? null : (ficheiro: escolhido, nome: nome);
  }
}

/// Ficha de exemplo (dados fictícios da documentação da API).
FichaDto fichaExemplo({
  String id = 'f-1',
  String nome = 'Maria Fictícia Santos',
  Papel papel = Papel.cuidador,
  bool principal = true,
  EstadoFicha estado = EstadoFicha.ativa,
  bool propria = false,
}) {
  return FichaDto(
    id: id,
    nomeCompleto: nome,
    dataNascimento: '1941-05-17',
    contactoEmergencia: '+351 910 000 001',
    eProprio: propria,
    estado: estado,
    grauAutonomia: const ItemDominioDto(id: 2, designacao: 'Autonomia parcial'),
    condicoesSaude: const [ItemDominioDto(id: 1, designacao: 'Diabetes')],
    alergias: const [ItemDominioDto(id: 1, designacao: 'Medicamentos')],
    meuPapel: papel,
    souPrincipal: principal,
  );
}

/// Linha de E05 correspondente a uma ficha.
FichaResumoDto resumoDe(FichaDto ficha) {
  return FichaResumoDto(
    id: ficha.id,
    nomeCompleto: ficha.nomeCompleto,
    eProprio: ficha.eProprio,
    estado: ficha.estado,
    meuPapel: ficha.meuPapel,
    souPrincipal: ficha.souPrincipal,
  );
}

/// Listas de necessidades de exemplo (OP-08).
const DominiosNecessidadesDto dominiosExemplo = DominiosNecessidadesDto(
  condicoesSaude: [
    ItemDominioDto(id: 1, designacao: 'Diabetes'),
    ItemDominioDto(id: 2, designacao: 'Hipertensão arterial'),
  ],
  alergias: [
    ItemDominioDto(id: 1, designacao: 'Medicamentos'),
    ItemDominioDto(id: 2, designacao: 'Alimentos'),
  ],
  grausAutonomia: [
    ItemDominioDto(id: 1, designacao: 'Autónomo'),
    ItemDominioDto(id: 2, designacao: 'Autonomia parcial'),
  ],
);

/// Repositório de fichas falso: guarda os pedidos e devolve ou lança o que o teste indicar.
class FichasRepositorioFalso implements FichasRepositorio {
  /// Fichas por identificador; a lista de E05 é feita a partir delas.
  final Map<String, FichaDto> fichas = {};

  Object? erroListar;
  Object? erroCriar;
  Object? erroObter;
  Object? erroEditar;
  Object? erroEstado;

  int listagens = 0;
  DadosFichaDto? ultimaCriacao;
  DadosFichaDto? ultimaEdicao;
  List<DocumentoParaEnvio> ultimosDocumentos = const [];
  final List<String> operacoes = [];

  /// Junta [ficha] às fichas do Utilizador.
  void juntar(FichaDto ficha) => fichas[ficha.id] = ficha;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<List<FichaResumoDto>> listar({EstadoFicha? estado}) async {
    listagens++;
    if (erroListar != null) {
      throw erroListar!;
    }
    return fichas.values
        .where((f) => estado == null || f.estado == estado)
        .map(resumoDe)
        .toList();
  }

  @override
  Future<FichaDto> criar(
    DadosFichaDto dados, {
    List<DocumentoParaEnvio> documentos = const [],
  }) async {
    ultimaCriacao = dados;
    ultimosDocumentos = documentos;
    if (erroCriar != null) {
      throw erroCriar!;
    }
    final ficha = fichaExemplo(
      id: 'f-nova',
      nome: dados.nomeCompleto,
      propria: dados.propria,
    );
    juntar(ficha);
    return ficha;
  }

  @override
  Future<FichaDto> obter(String fichaId) async {
    if (erroObter != null) {
      throw erroObter!;
    }
    return fichas[fichaId] ?? fichaExemplo(id: fichaId);
  }

  @override
  Future<FichaDto> editar(
    String fichaId,
    DadosFichaDto dados, {
    List<DocumentoParaEnvio> documentos = const [],
  }) async {
    ultimaEdicao = dados;
    ultimosDocumentos = documentos;
    if (erroEditar != null) {
      throw erroEditar!;
    }
    final atual = fichas[fichaId] ?? fichaExemplo(id: fichaId);
    final editada = FichaDto(
      id: fichaId,
      nomeCompleto: dados.nomeCompleto,
      dataNascimento: dados.dataNascimento,
      contactoEmergencia: dados.contactoEmergencia,
      eProprio: atual.eProprio,
      estado: atual.estado,
      grauAutonomia: atual.grauAutonomia,
      condicoesSaude: atual.condicoesSaude,
      alergias: atual.alergias,
      meuPapel: atual.meuPapel,
      souPrincipal: atual.souPrincipal,
    );
    juntar(editada);
    return editada;
  }

  Future<void> _mudarEstado(String operacao, String fichaId) async {
    operacoes.add('$operacao $fichaId');
    if (erroEstado != null) {
      throw erroEstado!;
    }
  }

  @override
  Future<void> arquivar(String fichaId) async {
    await _mudarEstado('arquivar', fichaId);
    final atual = fichas[fichaId];
    if (atual != null) {
      juntar(fichaExemplo(id: fichaId, estado: EstadoFicha.arquivada));
    }
  }

  @override
  Future<void> reativar(String fichaId) async {
    await _mudarEstado('reativar', fichaId);
    final atual = fichas[fichaId];
    if (atual != null) {
      juntar(fichaExemplo(id: fichaId));
    }
  }

  @override
  Future<void> eliminar(String fichaId) async {
    await _mudarEstado('eliminar', fichaId);
    fichas.remove(fichaId);
  }
}

/// Repositório das listas de necessidades falso.
class DominiosRepositorioFalso implements DominiosRepositorio {
  Object? erro;
  int pedidos = 0;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<DominiosNecessidadesDto> obterNecessidades() async {
    pedidos++;
    if (erro != null) {
      throw erro!;
    }
    return dominiosExemplo;
  }
}

/// Repositório dos documentos falso.
class DocumentosRepositorioFalso implements DocumentosRepositorio {
  final List<DocumentoDto> documentos = [];
  Object? erroListar;
  Object? erroCarregar;
  Object? erroDescarregar;
  DocumentoParaEnvio? ultimoCarregado;
  int descargas = 0;

  @override
  ApiClient get api => throw UnimplementedError();

  @override
  Future<DocumentoDto> carregar(
    String fichaId,
    DocumentoParaEnvio documento,
  ) async {
    ultimoCarregado = documento;
    if (erroCarregar != null) {
      throw erroCarregar!;
    }
    final dto = DocumentoDto(
      id: 'd-${documentos.length + 1}',
      categoria: documento.categoria,
      nomeOriginal: documento.ficheiro.nome,
      formato: 'PDF',
      tamanhoBytes: documento.ficheiro.tamanhoBytes,
      carregadoEm: '2026-11-09T20:00:00+00:00',
    );
    documentos.add(dto);
    return dto;
  }

  @override
  Future<List<DocumentoDto>> listar(String fichaId) async {
    if (erroListar != null) {
      throw erroListar!;
    }
    return List.of(documentos);
  }

  @override
  Future<Uint8List> descarregar(String documentoId) async {
    descargas++;
    if (erroDescarregar != null) {
      throw erroDescarregar!;
    }
    return Uint8List.fromList([0x25, 0x50, 0x44, 0x46]);
  }
}
