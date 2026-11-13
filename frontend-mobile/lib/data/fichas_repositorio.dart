import 'dart:convert';

import '../core/api_client.dart';
import '../models/dados_ficha_dto.dart';
import '../models/documento_para_envio.dart';
import '../models/estado_ficha.dart';
import '../models/ficha_dto.dart';
import '../models/ficha_resumo_dto.dart';
import 'partes_documento.dart';

/// Fichas (OP-07 e OP-09 a OP-14). Sem regras de negócio: forma o pedido, chama a API e devolve o DTO.
///
/// A criação e a edição usam `application/json` sem documentos e `multipart/form-data` com documentos: a parte
/// `dados` com o JSON e, por cada documento, uma parte `categoria` e uma parte `documento`, pela mesma ordem
/// (documentação da API, secção 3.6). Se um documento não for guardado, o servidor recusa toda a operação (RF-023).
class FichasRepositorio {
  /// Cria o repositório sobre o [api].
  const FichasRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-07: `GET /fichas`; sem [estado], devolve as fichas ativas e as arquivadas.
  Future<List<FichaResumoDto>> listar({EstadoFicha? estado}) async {
    final corpo = await api.get(
      '/fichas',
      parametros: estado == null ? null : {'estado': estado.valor},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(FichaResumoDto.fromJson)
        .toList(growable: false);
  }

  /// OP-09: `POST /fichas`, de terceiro ou própria, com zero ou mais [documentos].
  Future<FichaDto> criar(
    DadosFichaDto dados, {
    List<DocumentoParaEnvio> documentos = const [],
  }) {
    return _enviar('POST', '/fichas', dados.toJsonCriar(), documentos);
  }

  /// OP-10: `GET /fichas/{fichaId}`.
  Future<FichaDto> obter(String fichaId) async {
    final corpo = await api.get('/fichas/$fichaId');
    return FichaDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-11: `PUT /fichas/{fichaId}`, com zero ou mais [documentos] a acrescentar.
  Future<FichaDto> editar(
    String fichaId,
    DadosFichaDto dados, {
    List<DocumentoParaEnvio> documentos = const [],
  }) {
    return _enviar('PUT', '/fichas/$fichaId', dados.toJsonEditar(), documentos);
  }

  /// OP-12: `POST /fichas/{fichaId}/arquivo`.
  Future<void> arquivar(String fichaId) async {
    await api.post('/fichas/$fichaId/arquivo');
  }

  /// OP-13: `DELETE /fichas/{fichaId}/arquivo`.
  Future<void> reativar(String fichaId) async {
    await api.delete('/fichas/$fichaId/arquivo');
  }

  /// OP-14: `DELETE /fichas/{fichaId}`; só aceite sem medicação, tomas, ocorrências nem notas (RF-123).
  Future<void> eliminar(String fichaId) async {
    await api.delete('/fichas/$fichaId');
  }

  Future<FichaDto> _enviar(
    String metodo,
    String caminho,
    Map<String, dynamic> json,
    List<DocumentoParaEnvio> documentos,
  ) async {
    final Object? corpo;
    if (documentos.isEmpty) {
      corpo = metodo == 'POST'
          ? await api.post(caminho, corpo: json)
          : await api.put(caminho, corpo: json);
    } else {
      corpo = await api.enviarMultipart(
        metodo,
        caminho,
        campos: [
          MapEntry('dados', jsonEncode(json)),
          for (final documento in documentos)
            MapEntry('categoria', documento.categoria.valor),
        ],
        ficheiros: [
          for (final documento in documentos)
            parteDoDocumento(documento, 'documento'),
        ],
      );
    }
    return FichaDto.fromJson(corpo! as Map<String, dynamic>);
  }
}
