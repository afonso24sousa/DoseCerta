import 'package:flutter/material.dart';

/// Campo de formulário com rótulo e, por baixo, a mensagem de erro do campo com o valor ou formato esperado
/// (RNF-028). O texto vive no [controlador] da página, por isso um erro nunca apaga o que foi preenchido.
class CampoFormulario extends StatelessWidget {
  /// Cria o campo.
  const CampoFormulario({
    super.key,
    required this.rotulo,
    required this.controlador,
    this.erro,
    this.ajuda,
    this.palavraPasse = false,
    this.tipoTeclado,
    this.sugestoesPreenchimento,
    this.acaoTeclado = TextInputAction.next,
    this.aoSubmeter,
    this.aoMudar,
    this.ativo = true,
  });

  /// Rótulo do campo.
  final String rotulo;

  /// Controlador do texto.
  final TextEditingController controlador;

  /// Mensagem de erro do campo, ou `null`.
  final String? erro;

  /// Texto de ajuda (por exemplo, a regra da palavra-passe).
  final String? ajuda;

  /// Esconde o texto escrito.
  final bool palavraPasse;

  /// Tipo de teclado.
  final TextInputType? tipoTeclado;

  /// Sugestões de preenchimento automático do sistema.
  final Iterable<String>? sugestoesPreenchimento;

  /// Ação do teclado.
  final TextInputAction acaoTeclado;

  /// Chamado quando o Utilizador submete o campo no teclado.
  final ValueChanged<String>? aoSubmeter;

  /// Chamado a cada alteração do texto.
  final ValueChanged<String>? aoMudar;

  /// Indica se o campo aceita edição.
  final bool ativo;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextField(
        controller: controlador,
        enabled: ativo,
        obscureText: palavraPasse,
        enableSuggestions: !palavraPasse,
        autocorrect: false,
        keyboardType: tipoTeclado,
        autofillHints: sugestoesPreenchimento,
        textInputAction: acaoTeclado,
        onSubmitted: aoSubmeter,
        onChanged: aoMudar,
        decoration: InputDecoration(
          labelText: rotulo,
          helperText: ajuda,
          errorText: erro,
        ),
      ),
    );
  }
}
