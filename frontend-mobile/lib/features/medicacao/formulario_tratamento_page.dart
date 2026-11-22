import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/app_router.dart';
import '../../core/ficha_acesso_provider.dart';
import '../../core/formatador_data_hora.dart';
import '../../core/mensagens_erro.dart';
import '../../core/provedores.dart';
import '../../models/grau_importancia.dart';
import '../../models/tratamento_dto.dart';
import '../../shared/banner_erro.dart';
import '../../shared/campo_data.dart';
import '../../shared/campo_formulario.dart';
import '../../shared/folha_origem_ficheiro.dart';
import '../../shared/selecionador_ficheiros.dart';
import '../../shared/validador_tratamento.dart';
import 'apresentacao_tratamento.dart';
import 'formulario_tratamento_controller.dart';

/// Ecrã E16 Registar ou editar tratamento (OP-27, OP-29, OP-30; UC-17, UC-18). Registo: medicamento, dose, grau de
/// importância (um só, RF-051), data de início, data de fim opcional (vazia = contínuo, RF-050), horários (pelo
/// menos um) e receita opcional pela câmara, pela galeria ou pelo seletor de ficheiros (PDF, JPG ou PNG até 10 MB,
/// RF-052, RNF-008). Edição: só de tratamentos em curso, sem alterar a data de início (RF-055). Só o Cuidador, numa
/// ficha ativa (RF-050, RF-120). Os erros aparecem junto de cada campo e os dados mantêm-se (RNF-028); se a receita
/// não for guardada, nada é gravado e o formulário mantém os dados e a receita escolhida (RF-053).
class FormularioTratamentoPage extends ConsumerStatefulWidget {
  /// Cria o ecrã da ficha [fichaId]; com [tratamentoId], edita esse tratamento.
  const FormularioTratamentoPage({
    super.key,
    required this.fichaId,
    this.tratamentoId,
  });

  /// Identificador da ficha.
  final String fichaId;

  /// Tratamento a editar; `null` no registo.
  final String? tratamentoId;

  /// Mensagem depois de registar (igual à do frontend web, I066).
  static const String registado =
      'Tratamento registado. As tomas previstas foram geradas.';

  @override
  ConsumerState<FormularioTratamentoPage> createState() =>
      _FormularioTratamentoPageState();
}

class _FormularioTratamentoPageState
    extends ConsumerState<FormularioTratamentoPage> {
  final _medicamento = TextEditingController();
  final _dose = TextEditingController();
  final _dataInicio = TextEditingController();
  final _dataFim = TextEditingController();
  final _novaHora = TextEditingController();
  GrauImportancia? _grau;
  List<String> _horarios = const [];
  String? _erroHora;
  bool _preenchido = false;

  AlvoFormulario get _alvo =>
      (fichaId: widget.fichaId, tratamentoId: widget.tratamentoId);

  bool get _edicao => widget.tratamentoId != null;

  @override
  void initState() {
    super.initState();
    if (_edicao) {
      Future.microtask(
        () => ref
            .read(formularioTratamentoControllerProvider(_alvo).notifier)
            .carregar(),
      );
    } else {
      _dataInicio.text = FormatadorDataHora.deData(
        ref.read(relogioProvider).agora(),
      );
      _preenchido = true;
    }
  }

  @override
  void dispose() {
    _medicamento.dispose();
    _dose.dispose();
    _dataInicio.dispose();
    _dataFim.dispose();
    _novaHora.dispose();
    super.dispose();
  }

  void _preencher(TratamentoDto tratamento) {
    _medicamento.text = tratamento.medicamento;
    _dose.text = tratamento.dose;
    _grau = tratamento.grauImportancia;
    _dataInicio.text = FormatadorDataHora.apresentarData(tratamento.dataInicio);
    final fim = tratamento.dataFim;
    _dataFim.text = fim == null ? '' : FormatadorDataHora.apresentarData(fim);
    _horarios = [...tratamento.horarios];
    _preenchido = true;
  }

  void _acrescentarHora() {
    final erro = ValidadorTratamento.validarHora(_novaHora.text, _horarios);
    setState(() {
      _erroHora = erro;
      if (erro == null) {
        _horarios = [
          ..._horarios,
          ValidadorTratamento.normalizarHora(_novaHora.text)!,
        ]..sort();
        _novaHora.clear();
      }
    });
  }

  Future<void> _escolherHora() async {
    final atual = ValidadorTratamento.normalizarHora(_novaHora.text);
    final hora = await showTimePicker(
      context: context,
      initialTime: atual == null
          ? const TimeOfDay(hour: 8, minute: 0)
          : TimeOfDay(
              hour: int.parse(atual.substring(0, 2)),
              minute: int.parse(atual.substring(3)),
            ),
    );
    if (hora != null) {
      final horas = hora.hour.toString().padLeft(2, '0');
      final minutos = hora.minute.toString().padLeft(2, '0');
      _novaHora.text = '$horas:$minutos';
      _acrescentarHora();
    }
  }

  Future<void> _escolherReceita() async {
    final origem = await FolhaOrigemFicheiro.mostrar(
      context,
      titulo: 'Carregar: Receita médica',
    );
    if (origem != null) {
      await ref
          .read(formularioTratamentoControllerProvider(_alvo).notifier)
          .escolherReceita(origem);
    }
  }

  Future<void> _guardar() async {
    FocusScope.of(context).unfocus();
    final tratamento = await ref
        .read(formularioTratamentoControllerProvider(_alvo).notifier)
        .guardar((
          medicamento: _medicamento.text,
          dose: _dose.text,
          grau: _grau,
          dataInicio: _dataInicio.text,
          dataFim: _dataFim.text,
          horarios: _horarios,
        ));
    if (tratamento == null || !mounted) {
      return;
    }
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            _edicao
                ? 'Alterações guardadas.'
                : FormularioTratamentoPage.registado,
          ),
        ),
      );
    context.go(
      _edicao
          ? AppRouter.tratamento(widget.fichaId, tratamento.id)
          : AppRouter.medicacaoFicha(widget.fichaId),
    );
  }

  void _voltar() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRouter.medicacaoFicha(widget.fichaId));
    }
  }

  @override
  Widget build(BuildContext context) {
    final acesso = ref.watch(fichaAcessoProvider(widget.fichaId));
    final estado = ref.watch(formularioTratamentoControllerProvider(_alvo));
    final controlador = ref.read(
      formularioTratamentoControllerProvider(_alvo).notifier,
    );
    final original = estado.original;
    if (original != null && !_preenchido) {
      _preencher(original);
    }
    final ficha = acesso.value?.ficha;

    final List<Widget> conteudo;
    if (acesso.hasError) {
      conteudo = [BannerErro(MensagensErro.traduzir(acesso.error!))];
    } else if (ficha == null) {
      conteudo = const [LinearProgressIndicator()];
    } else if (!ficha.souCuidador) {
      conteudo = [BannerErro(MensagensErro.catalogo['ACESSO_PROIBIDO']!)];
    } else if (!ficha.ativa) {
      conteudo = [BannerErro(MensagensErro.catalogo['FICHA_ARQUIVADA']!)];
    } else if (estado.erroCarregar != null) {
      conteudo = [
        BannerErro(estado.erroCarregar!),
        OutlinedButton(
          onPressed: estado.aCarregar ? null : controlador.carregar,
          child: const Text('Tentar novamente'),
        ),
      ];
    } else if (_edicao && original == null) {
      conteudo = const [LinearProgressIndicator()];
    } else if (original != null && !original.emCurso) {
      conteudo = [const BannerErro(ApresentacaoTratamento.naoEditavel)];
    } else {
      conteudo = _formulario(context, estado, controlador);
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(_edicao ? 'Editar tratamento' : 'Registar tratamento'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: conteudo,
          ),
        ),
      ),
    );
  }

  List<Widget> _formulario(
    BuildContext context,
    FormularioTratamentoEstado estado,
    FormularioTratamentoController controlador,
  ) {
    final erros = estado.erros;
    final hoje = ref.read(relogioProvider).agora();
    final ultimaData = DateTime(hoje.year + 10, 12, 31);
    final receita = estado.receita;
    return [
      CampoFormulario(
        rotulo: 'Medicamento',
        controlador: _medicamento,
        erro: erros['medicamento'],
      ),
      CampoFormulario(rotulo: 'Dose', controlador: _dose, erro: erros['dose']),
      Padding(
        padding: const EdgeInsets.only(bottom: 16),
        child: InputDecorator(
          decoration: InputDecoration(
            labelText: 'Grau de importância',
            errorText: erros['grauImportancia'],
            border: InputBorder.none,
          ),
          child: SegmentedButton<GrauImportancia>(
            emptySelectionAllowed: true,
            showSelectedIcon: false,
            segments: [
              for (final grau in GrauImportancia.values)
                ButtonSegment(value: grau, label: Text(grau.valor)),
            ],
            selected: {?_grau},
            onSelectionChanged: (escolhidos) => setState(
              () => _grau = escolhidos.isEmpty ? null : escolhidos.first,
            ),
          ),
        ),
      ),
      CampoData(
        rotulo: 'Data de início',
        controlador: _dataInicio,
        ultimaData: ultimaData,
        primeiraData: DateTime(2000),
        ativo: !_edicao,
        ajuda: _edicao
            ? 'A data de início não pode ser alterada depois do registo.'
            : 'Formato dd/mm/aaaa',
        erro: erros['dataInicio'],
      ),
      CampoData(
        rotulo: 'Data de fim',
        controlador: _dataFim,
        ultimaData: ultimaData,
        primeiraData: DateTime(2000),
        ajuda: estado.original?.dataFim != null
            ? ValidadorTratamento.regraDataFimNaoRetirada
            : 'Opcional; vazia = tratamento contínuo. Formato dd/mm/aaaa',
        erro: erros['dataFim'],
      ),
      InputDecorator(
        decoration: InputDecoration(
          labelText: 'Horários',
          helperText: _edicao
              ? 'Alterar os horários só afeta as tomas futuras; as tomas anteriores mantêm a hora e o estado.'
              : 'Pelo menos uma hora de toma por dia.',
          helperMaxLines: 3,
          errorText: erros['horarios'],
          errorMaxLines: 3,
          border: InputBorder.none,
        ),
        child: Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            if (_horarios.isEmpty) const Text('Ainda não há horas de toma.'),
            for (final hora in _horarios)
              InputChip(
                label: Text(hora),
                deleteButtonTooltipMessage: 'Retirar $hora',
                onDeleted: estado.ocupado
                    ? null
                    : () => setState(
                        () => _horarios = [..._horarios]..remove(hora),
                      ),
              ),
          ],
        ),
      ),
      const SizedBox(height: 8),
      TextField(
        controller: _novaHora,
        keyboardType: TextInputType.datetime,
        textInputAction: TextInputAction.done,
        onSubmitted: (_) => _acrescentarHora(),
        decoration: InputDecoration(
          labelText: 'Hora a acrescentar',
          hintText: 'hh:mm',
          helperText: 'Formato hh:mm, por exemplo 08:00',
          errorText: _erroHora,
          suffixIcon: IconButton(
            tooltip: 'Escolher a hora',
            icon: const Icon(Icons.schedule),
            onPressed: estado.ocupado ? null : _escolherHora,
          ),
        ),
      ),
      const SizedBox(height: 8),
      OutlinedButton.icon(
        onPressed: estado.ocupado ? null : _acrescentarHora,
        icon: const Icon(Icons.add),
        label: const Text('Adicionar hora'),
      ),
      const SizedBox(height: 16),
      OutlinedButton.icon(
        onPressed: estado.ocupado ? null : _escolherReceita,
        icon: const Icon(Icons.upload_file_outlined),
        label: const Text('Carregar: Receita médica (opcional)'),
      ),
      const Padding(
        padding: EdgeInsets.symmetric(vertical: 8),
        child: Text(SelecionadorFicheiros.ficheirosAceites),
      ),
      if (receita != null)
        Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: ListTile(
            leading: const Icon(Icons.description_outlined),
            title: Text(receita.nome),
            subtitle: const Text('Receita médica'),
            trailing: IconButton(
              tooltip: 'Retirar ${receita.nome}',
              icon: const Icon(Icons.close),
              onPressed: estado.ocupado ? null : controlador.retirarReceita,
            ),
          ),
        ),
      if (erros['receita.ficheiro'] != null)
        BannerErro(erros['receita.ficheiro']!),
      const SizedBox(height: 8),
      if (estado.erro != null) BannerErro(estado.erro!),
      if (estado.aGuardar)
        const Padding(
          padding: EdgeInsets.only(bottom: 8),
          child: LinearProgressIndicator(),
        ),
      FilledButton(
        onPressed: estado.ocupado ? null : _guardar,
        child: Text(_edicao ? 'Guardar' : 'Registar tratamento'),
      ),
      const SizedBox(height: 8),
      OutlinedButton(
        onPressed: estado.aGuardar ? null : _voltar,
        child: const Text('Cancelar'),
      ),
    ];
  }
}
