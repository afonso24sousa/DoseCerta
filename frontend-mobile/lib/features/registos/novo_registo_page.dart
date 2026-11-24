import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/tipo_registo.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_data.dart';
import '../../shared/validador_tratamento.dart';
import 'novo_registo_controller.dart';

/// Ecrã E24 Nova ocorrência ou nota (protótipo de baixa fidelidade v02): o Cuidador, numa ficha ativa, regista uma
/// ocorrência, com a descrição e a data e hora em que aconteceu (por omissão, o momento atual em Lisboa; RF-090),
/// ou uma nota dirigida aos Cuidadores, só com o texto (RF-091). Os erros aparecem em cada campo sem apagar os
/// restantes dados (RNF-028) e as recusas do servidor mostram a mensagem respetiva. Guardado o registo, volta a E23
/// com o registo como resultado.
class NovoRegistoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId], com o [tipo] escolhido à partida.
  const NovoRegistoPage({
    super.key,
    required this.fichaId,
    this.tipo = TipoRegisto.ocorrencia,
  });

  /// Identificador da ficha.
  final String fichaId;

  /// Tipo escolhido ao abrir o ecrã (o do separador aberto em E23).
  final TipoRegisto tipo;

  /// Aviso do protótipo (E24): os registos não se alteram nem se eliminam (RF-090, RF-091).
  static const String avisoDefinitivo =
      'Depois de guardado, o registo não pode ser alterado nem eliminado.';

  @override
  ConsumerState<NovoRegistoPage> createState() => _NovoRegistoPageState();
}

class _NovoRegistoPageState extends ConsumerState<NovoRegistoPage> {
  final TextEditingController _descricao = TextEditingController();
  final TextEditingController _data = TextEditingController();
  final TextEditingController _hora = TextEditingController();
  late TipoRegisto _tipo = widget.tipo;

  @override
  void initState() {
    super.initState();
    final agora = FormatadorDataHora.emLisboa(
      ref.read(relogioProvider).agora(),
    );
    _data.text = FormatadorDataHora.deData(agora);
    _hora.text = _horaMinuto(agora.hour, agora.minute);
  }

  @override
  void dispose() {
    _descricao.dispose();
    _data.dispose();
    _hora.dispose();
    super.dispose();
  }

  static String _horaMinuto(int hora, int minuto) =>
      '${hora.toString().padLeft(2, '0')}:${minuto.toString().padLeft(2, '0')}';

  Future<void> _escolherHora() async {
    final atual = ValidadorTratamento.normalizarHora(_hora.text);
    final hora = await showTimePicker(
      context: context,
      initialTime: atual == null
          ? TimeOfDay.now()
          : TimeOfDay(
              hour: int.parse(atual.substring(0, 2)),
              minute: int.parse(atual.substring(3)),
            ),
    );
    if (hora != null) {
      _hora.text = _horaMinuto(hora.hour, hora.minute);
    }
  }

  Future<void> _guardar() async {
    FocusScope.of(context).unfocus();
    final controlador = ref.read(
      novoRegistoControllerProvider(widget.fichaId).notifier,
    );
    final registo = _tipo == TipoRegisto.ocorrencia
        ? await controlador.registarOcorrencia(
            descricao: _descricao.text,
            data: _data.text,
            hora: _hora.text,
          )
        : await controlador.registarNota(descricao: _descricao.text);
    if (registo == null || !mounted) {
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          registo.tipo == TipoRegisto.ocorrencia
              ? 'Ocorrência registada.'
              : 'Nota registada.',
        ),
      ),
    );
    context.pop(registo);
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId)).value;
    final estado = ref.watch(novoRegistoControllerProvider(widget.fichaId));
    final ativo = !estado.aGuardar;
    final ocorrencia = _tipo == TipoRegisto.ocorrencia;
    final hoje = FormatadorDataHora.diaDeLisboa(
      ref.read(relogioProvider).agora(),
    );
    return Scaffold(
      appBar: AppBar(title: const Text('Novo registo')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (acesso != null && !acesso.podeRegistar)
              BannerErro(
                MensagensErro.catalogo[acesso.ativa
                    ? 'ACESSO_PROIBIDO'
                    : 'FICHA_ARQUIVADA']!,
                informativo: true,
              )
            else ...[
              RadioGroup<TipoRegisto>(
                groupValue: _tipo,
                onChanged: (tipo) {
                  if (ativo && tipo != null) {
                    setState(() => _tipo = tipo);
                  }
                },
                child: const Column(
                  children: [
                    RadioListTile<TipoRegisto>(
                      value: TipoRegisto.ocorrencia,
                      title: Text('Ocorrência'),
                      subtitle: Text(
                        'Os outros Cuidadores e os Familiares da ficha veem-na '
                        'e são notificados.',
                      ),
                      contentPadding: EdgeInsets.zero,
                    ),
                    RadioListTile<TipoRegisto>(
                      value: TipoRegisto.nota,
                      title: Text('Nota para os Cuidadores'),
                      subtitle: Text('Só os Cuidadores da ficha a veem.'),
                      contentPadding: EdgeInsets.zero,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: TextField(
                  controller: _descricao,
                  enabled: ativo,
                  minLines: 3,
                  maxLines: null,
                  maxLength: NovoRegistoController.maximoDescricao,
                  keyboardType: TextInputType.multiline,
                  textCapitalization: TextCapitalization.sentences,
                  decoration: InputDecoration(
                    labelText: NovoRegistoController.rotulos['descricao'],
                    alignLabelWithHint: true,
                    errorText: estado.erros['descricao'],
                  ),
                ),
              ),
              if (ocorrencia) ...[
                Semantics(
                  header: true,
                  child: Text(
                    'Data e hora em que aconteceu',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                ),
                const SizedBox(height: 8),
                CampoData(
                  rotulo: NovoRegistoController.rotulos['data']!,
                  controlador: _data,
                  ultimaData: DateTime(hoje.year + 1, hoje.month, hoje.day),
                  erro: estado.erros['data'],
                  ativo: ativo,
                ),
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: TextField(
                    controller: _hora,
                    enabled: ativo,
                    keyboardType: TextInputType.datetime,
                    decoration: InputDecoration(
                      labelText: NovoRegistoController.rotulos['hora'],
                      hintText: 'hh:mm',
                      helperText: 'Formato hh:mm',
                      errorText: estado.erros['hora'],
                      suffixIcon: IconButton(
                        tooltip: 'Escolher a hora',
                        icon: const Icon(Icons.schedule),
                        onPressed: ativo ? _escolherHora : null,
                      ),
                    ),
                  ),
                ),
              ],
              if (estado.erroGeral != null) BannerErro(estado.erroGeral!),
              const Text(NovoRegistoPage.avisoDefinitivo),
              const SizedBox(height: 16),
              if (estado.aGuardar)
                const Padding(
                  padding: EdgeInsets.only(bottom: 8),
                  child: LinearProgressIndicator(),
                ),
              FilledButton(
                onPressed: ativo ? _guardar : null,
                child: const Text('Guardar'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
