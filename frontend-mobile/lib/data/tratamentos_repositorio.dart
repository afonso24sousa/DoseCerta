import 'dart:convert';

import '../core/api_client.dart';
import '../models/dados_tratamento_dto.dart';
import '../models/estado_tratamento.dart';
import '../models/ficheiro_escolhido.dart';
import '../models/tratamento_dto.dart';
import 'partes_documento.dart';

/// Tratamentos da ficha (OP-27 a OP-31). Sem regras de negócio: forma o pedido, chama a API e devolve o DTO.
///
/// O registo e a atualização usam `application/json` sem receita e `multipart/form-data` com receita: a parte
/// `dados` com o JSON e a parte `receita` com o ficheiro (documentação da API, secção 3.6). Se a receita não for
/// guardada, o servidor recusa toda a operação (RF-053).
class TratamentosRepositorio {
  /// Cria o repositório sobre o [api].
  const TratamentosRepositorio(this.api);

  /// Cliente HTTP.
  final ApiClient api;

  /// OP-27: `POST /fichas/{fichaId}/tratamentos`, com a [receita] opcional.
  Future<TratamentoDto> registar(
    String fichaId,
    DadosTratamentoDto dados, {
    FicheiroEscolhido? receita,
  }) {
    return _enviar('POST', '/fichas/$fichaId/tratamentos', dados, receita);
  }

  /// OP-28: `GET /fichas/{fichaId}/tratamentos`; sem [estado], devolve os em curso e os terminados.
  Future<List<TratamentoDto>> listar(
    String fichaId, {
    EstadoTratamento? estado,
  }) async {
    final corpo = await api.get(
      '/fichas/$fichaId/tratamentos',
      parametros: estado == null ? null : {'estado': estado.valor},
    );
    return (corpo! as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .map(TratamentoDto.fromJson)
        .toList(growable: false);
  }

  /// OP-29: `GET /tratamentos/{tratamentoId}`.
  Future<TratamentoDto> consultar(String tratamentoId) async {
    final corpo = await api.get('/tratamentos/$tratamentoId');
    return TratamentoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  /// OP-30: `PUT /tratamentos/{tratamentoId}`, só com os campos alterados e a [receita] nova opcional.
  Future<TratamentoDto> atualizar(
    String tratamentoId,
    DadosTratamentoDto dados, {
    FicheiroEscolhido? receita,
  }) {
    return _enviar('PUT', '/tratamentos/$tratamentoId', dados, receita);
  }

  /// OP-31: `POST /tratamentos/{tratamentoId}/termino`.
  Future<TratamentoDto> terminar(String tratamentoId) async {
    final corpo = await api.post('/tratamentos/$tratamentoId/termino');
    return TratamentoDto.fromJson(corpo! as Map<String, dynamic>);
  }

  Future<TratamentoDto> _enviar(
    String metodo,
    String caminho,
    DadosTratamentoDto dados,
    FicheiroEscolhido? receita,
  ) async {
    final json = dados.toJson();
    final Object? corpo;
    if (receita == null) {
      corpo = metodo == 'POST'
          ? await api.post(caminho, corpo: json)
          : await api.put(caminho, corpo: json);
    } else {
      corpo = await api.enviarMultipart(
        metodo,
        caminho,
        campos: [MapEntry('dados', jsonEncode(json))],
        ficheiros: [parteDoFicheiro(receita, 'receita')],
      );
    }
    return TratamentoDto.fromJson(corpo! as Map<String, dynamic>);
  }
}
