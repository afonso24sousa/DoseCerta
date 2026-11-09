import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/dados_ficha_dto.dart';
import '../models/documento_para_envio.dart';

/// Dados da nova ficha guardados entre E06 e E07.
class RascunhoNovaFicha {
  /// Cria o rascunho.
  const RascunhoNovaFicha({
    this.dados,
    this.documentos = const [],
    this.principalEu = true,
    this.emailPrincipal = '',
    this.errosDados = const {},
  });

  /// Dados de E06, já validados no dispositivo (sem o Cuidador principal).
  final DadosFichaDto? dados;

  /// Documentos escolhidos em E07, pela ordem em que foram escolhidos.
  final List<DocumentoParaEnvio> documentos;

  /// Indica se o criador é o Cuidador principal (RF-024); falso para convidar outra pessoa (RF-025).
  final bool principalEu;

  /// E-mail da pessoa a convidar para Cuidador principal.
  final String emailPrincipal;

  /// Erros que o servidor devolveu para campos de E06 ao criar a ficha em E07 (decisão D4).
  final Map<String, String> errosDados;

  /// Copia o rascunho, alterando os valores indicados.
  RascunhoNovaFicha copiar({
    DadosFichaDto? dados,
    List<DocumentoParaEnvio>? documentos,
    bool? principalEu,
    String? emailPrincipal,
    Map<String, String>? errosDados,
  }) {
    return RascunhoNovaFicha(
      dados: dados ?? this.dados,
      documentos: documentos ?? this.documentos,
      principalEu: principalEu ?? this.principalEu,
      emailPrincipal: emailPrincipal ?? this.emailPrincipal,
      errosDados: errosDados ?? this.errosDados,
    );
  }
}

/// Guarda os dados de E06 e de E07 enquanto o Utilizador avança, recua ou repete a criação depois de uma falha
/// (protótipo, E06: «O formulário em dois passos mantém os dados ao avançar e ao recuar»; RF-023, RNF-028).
class NovaFichaRascunho extends Notifier<RascunhoNovaFicha> {
  @override
  RascunhoNovaFicha build() => const RascunhoNovaFicha();

  /// Guarda os dados de E06 e limpa os erros do servidor desses campos.
  void guardar(DadosFichaDto dados) {
    state = state.copiar(dados: dados, errosDados: const {});
  }

  /// Junta um documento escolhido em E07.
  void juntarDocumento(DocumentoParaEnvio documento) {
    state = state.copiar(documentos: [...state.documentos, documento]);
  }

  /// Retira o documento na posição [indice].
  void retirarDocumento(int indice) {
    state = state.copiar(documentos: [...state.documentos]..removeAt(indice));
  }

  /// Escolhe quem é o Cuidador principal.
  void escolherPrincipal({required bool eu}) {
    state = state.copiar(principalEu: eu);
  }

  /// Guarda o e-mail da pessoa a convidar.
  void mudarEmail(String email) {
    state = state.copiar(emailPrincipal: email);
  }

  /// Guarda os erros do servidor nos campos de E06, para E06 os mostrar por baixo dos campos.
  void indicarErrosDados(Map<String, String> erros) {
    state = state.copiar(errosDados: erros);
  }

  /// Começa uma nova ficha.
  void limpar() {
    state = const RascunhoNovaFicha();
  }
}

/// Fornecedor do rascunho da nova ficha.
final novaFichaRascunhoProvider =
    NotifierProvider<NovaFichaRascunho, RascunhoNovaFicha>(
      NovaFichaRascunho.new,
    );
