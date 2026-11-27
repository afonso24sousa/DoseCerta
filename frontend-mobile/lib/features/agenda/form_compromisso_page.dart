import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/compromisso_dto.dart';
import '../../models/dados_compromisso_dto.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_data.dart';
import '../../shared/campo_formulario.dart';
import '../../shared/dialogo_confirmacao.dart';
import 'form_compromisso_controller.dart';

/// Ecrã E22 Registar ou editar compromisso (RF-080 a RF-082, RF-116, RNF-028). Só o Cuidador de uma ficha ativa
/// regista, edita e remove; o Familiar e a ficha arquivada só consultam os dados (RF-121).
class FormCompromissoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId]; com [compromissoId], abre a edição.
  const FormCompromissoPage({
    super.key,
    required this.fichaId,
    this.compromissoId,
  });

  /// Identificador da ficha.
  final String fichaId;

  /// Compromisso a editar; `null` no registo.
  final String? compromissoId;

  /// Texto de cada antecedência do aviso (RF-116).
  static String rotuloAntecedencia(int minutos) => switch (minutos) {
    30 => '30 minutos',
    60 => '1 hora',
    _ => '${minutos ~/ 60} horas',
  };

  @override
  ConsumerState<FormCompromissoPage> createState() =>
      _FormCompromissoPageState();
}

class _FormCompromissoPageState extends ConsumerState<FormCompromissoPage> {
  final _data = TextEditingController();
  final _hora = TextEditingController();
  final _local = TextEditingController();
  final _motivo = TextEditingController();
  int _antecedencia = DadosCompromissoDto.antecedenciaPorOmissao;

  AlvoCompromisso get _alvo =>
      (fichaId: widget.fichaId, compromissoId: widget.compromissoId);

  bool get _edicao => widget.compromissoId != null;

  bool get _avisoJaPassou {
    final data = FormatadorDataHora.lerData(_data.text);
    final hora = RegExp(r'^([01]?\d|2[0-3]):([0-5]\d)$')
        .firstMatch(_hora.text.trim());
    if (data == null || hora == null) {
      return false;
    }

    final compromisso = DateTime.parse(
      FormatadorDataHora.instanteDeLisboa(
        data,
        int.parse(hora.group(1)!),
        int.parse(hora.group(2)!),
      ),
    );
    final agora = ref.read(relogioProvider).agora();
    return compromisso.isAfter(agora) &&
        compromisso.subtract(Duration(minutes: _antecedencia)).isBefore(agora);
  }

  @override
  void initState() {
    super.initState();
    if (_edicao) {
      Future.microtask(() async {
        await ref
            .read(formCompromissoControllerProvider(_alvo).notifier)
            .carregar();
        final original = ref
            .read(formCompromissoControllerProvider(_alvo))
            .original;
        if (original != null && mounted) {
          setState(() => _preencher(original));
        }
      });
    }
  }

  void _preencher(CompromissoDto c) {
    _data.text = FormatadorDataHora.apresentarData(c.dataHora);
    _hora.text = FormatadorDataHora.apresentarHora(c.dataHora);
    _local.text = c.local;
    _motivo.text = c.motivo;
    _antecedencia =
        DadosCompromissoDto.antecedencias.contains(c.antecedenciaAvisoMinutos)
        ? c.antecedenciaAvisoMinutos
        : DadosCompromissoDto.antecedenciaPorOmissao;
  }

  @override
  void dispose() {
    _data.dispose();
    _hora.dispose();
    _local.dispose();
    _motivo.dispose();
    super.dispose();
  }

  void _voltar() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRouter.agenda);
    }
  }

  Future<void> _guardar() async {
    final guardado = await ref
        .read(formCompromissoControllerProvider(_alvo).notifier)
        .guardar((
          data: _data.text,
          hora: _hora.text,
          local: _local.text,
          motivo: _motivo.text,
          antecedencia: _antecedencia,
        ));
    if (guardado && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            _edicao ? 'Compromisso guardado.' : 'Compromisso registado.',
          ),
        ),
      );
      _voltar();
    }
  }

  Future<void> _remover() async {
    final confirmado = await DialogoConfirmacao.confirmar(
      context,
      titulo: 'Remover compromisso',
      mensagem: 'O compromisso deixa de aparecer na agenda e o aviso deixa de ser enviado.',
      acao: 'Remover',
    );
    if (!confirmado || !mounted) {
      return;
    }
    final removido = await ref
        .read(formCompromissoControllerProvider(_alvo).notifier)
        .remover();
    if (removido && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Compromisso removido.')));
      _voltar();
    }
  }

  Future<void> _escolherHora() async {
    final atual = RegExp(r'^(\d{1,2}):(\d{2})$').firstMatch(_hora.text.trim());
    final escolhida = await showTimePicker(
      context: context,
      initialTime: atual == null
          ? const TimeOfDay(hour: 9, minute: 0)
          : TimeOfDay(
              hour: int.parse(atual.group(1)!) % 24,
              minute: int.parse(atual.group(2)!) % 60,
            ),
      builder: (context, filho) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: filho!,
      ),
    );
    if (escolhida != null) {
      _hora.text =
          '${escolhida.hour.toString().padLeft(2, '0')}:'
          '${escolhida.minute.toString().padLeft(2, '0')}';
    }
  }

  @override
  Widget build(BuildContext context) {
    final estado = ref.watch(formCompromissoControllerProvider(_alvo));
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId));
    final podeAlterar = acesso.value?.podeRegistar ?? false;
    final ativo = podeAlterar && !estado.ocupado;
    final titulo = !_edicao
        ? 'Registar compromisso'
        : podeAlterar
        ? 'Editar compromisso'
        : 'Compromisso';

    final List<Widget> corpo;
    if (estado.aCarregar || acesso.isLoading) {
      corpo = const [LinearProgressIndicator()];
    } else if (acesso.error case final erro?) {
      corpo = [BannerErro(MensagensErro.traduzir(erro))];
    } else if (estado.erroCarregar case final erro?) {
      corpo = [BannerErro(erro)];
    } else {
      corpo = [
        Text('Ficha: ${acesso.value!.ficha.nomeCompleto}'),
        const SizedBox(height: 16),
        if (!acesso.value!.ativa)
          BannerErro(
            MensagensErro.catalogo['FICHA_ARQUIVADA']!,
            informativo: true,
          )
        else if (!podeAlterar)
          const BannerErro(
            'O Familiar só consulta os compromissos.',
            informativo: true,
          ),
        if (estado.erro case final erro?) BannerErro(erro),
        CampoData(
          rotulo: 'Data',
          controlador: _data,
          primeiraData: DateTime(2000),
          ultimaData: DateTime(2100, 12, 31),
          erro: estado.erros['data'],
          ativo: ativo,
          aoMudar: (_) => setState(() {}),
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: TextField(
            controller: _hora,
            enabled: ativo,
            keyboardType: TextInputType.datetime,
            textInputAction: TextInputAction.next,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: 'Hora',
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
        CampoFormulario(
          rotulo: 'Local',
          controlador: _local,
          ajuda: 'Até ${FormCompromissoController.maxLocal} caracteres',
          erro: estado.erros['local'],
          ativo: ativo,
        ),
        CampoFormulario(
          rotulo: 'Motivo',
          controlador: _motivo,
          ajuda: 'Até ${FormCompromissoController.maxMotivo} caracteres',
          erro: estado.erros['motivo'],
          ativo: ativo,
          acaoTeclado: TextInputAction.done,
        ),
        DropdownButtonFormField<int>(
          key: ValueKey(_antecedencia),
          initialValue: _antecedencia,
          decoration: InputDecoration(
            labelText: 'Antecedência do aviso',
            errorText: estado.erros['antecedenciaAvisoMinutos'],
          ),
          items: [
            for (final minutos in DadosCompromissoDto.antecedencias)
              DropdownMenuItem(
                value: minutos,
                child: Text(FormCompromissoPage.rotuloAntecedencia(minutos)),
              ),
          ],
          onChanged: ativo
              ? (valor) => setState(() => _antecedencia = valor!)
              : null,
        ),
        if (_avisoJaPassou)
          const BannerErro(
            'O momento do aviso já passou: a notificação é criada logo ao guardar.',
            informativo: true,
          ),
        const SizedBox(height: 24),
        if (podeAlterar) ...[
          FilledButton(
            onPressed: ativo ? _guardar : null,
            child: Text(_edicao ? 'Guardar' : 'Registar compromisso'),
          ),
          if (_edicao) ...[
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: ativo ? _remover : null,
              icon: const Icon(Icons.delete_outline),
              label: const Text('Remover compromisso'),
            ),
          ],
        ],
      ];
    }

    return Scaffold(
      appBar: AppBar(title: Text(titulo)),
      body: SafeArea(
        child: ListView(padding: const EdgeInsets.all(16), children: corpo),
      ),
    );
  }
}
