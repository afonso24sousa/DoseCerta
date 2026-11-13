import 'package:flutter/material.dart';

/// Mostra um erro da API, de validação ou de falta de ligação junto da ação que falhou, sem apagar os dados do
/// formulário. Com [informativo], mostra uma informação (por exemplo, a conta criada) em vez de um erro. O tipo é
/// identificado pelo ícone e pelo texto, não só pela cor.
class BannerErro extends StatelessWidget {
  /// Cria o banner com a [mensagem].
  const BannerErro(this.mensagem, {super.key, this.informativo = false});

  /// Mensagem a mostrar.
  final String mensagem;

  /// Indica se é uma informação e não um erro.
  final bool informativo;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    final fundo = informativo
        ? esquema.secondaryContainer
        : const Color(0xFFFFF3D6);
    final contorno = informativo ? esquema.secondary : const Color(0xFFC9A227);
    return Semantics(
      liveRegion: true,
      child: Container(
        width: double.infinity,
        margin: const EdgeInsets.only(bottom: 16),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: fundo,
          border: Border.all(color: contorno),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              informativo ? Icons.info_outline : Icons.error_outline,
              color: Colors.black87,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                mensagem,
                style: const TextStyle(color: Colors.black87),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
