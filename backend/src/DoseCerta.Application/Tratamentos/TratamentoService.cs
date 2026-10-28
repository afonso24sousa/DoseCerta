// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Tratamentos;

/// <summary>
/// Registo, consulta, atualização e término de tratamentos, com receita opcional (F005, RF-050 a RF-057). Delega na
/// <see cref="TomaService"/> a geração, a regeneração e a remoção das tomas futuras (RF-060). Cada escrita é atómica:
/// se a receita não for guardada, nada é gravado (RF-053, RNF-046).
/// </summary>
public sealed class TratamentoService
{
    /// <summary>
    /// Comprimento máximo do nome do medicamento (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoMedicamento = 120;

    /// <summary>
    /// Comprimento máximo da dose (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoDose = 60;

    private const string MensagemNaoEncontrado = "O tratamento não existe ou pertence a uma ficha a que não está associado.";

    private readonly ITratamentoRepositorio tratamentos;
    private readonly DocumentoService documentoService;
    private readonly TomaService tomaService;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratamentoService"/> class.
    /// </summary>
    /// <param name="tratamentos">Repositório dos tratamentos.</param>
    /// <param name="documentoService">Serviço dos documentos, para a receita.</param>
    /// <param name="tomaService">Serviço das tomas, para a geração.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    public TratamentoService(
        ITratamentoRepositorio tratamentos,
        DocumentoService documentoService,
        TomaService tomaService,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.tratamentos = tratamentos;
        this.documentoService = documentoService;
        this.tomaService = tomaService;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-27: regista um tratamento numa ficha ativa, com receita opcional, e gera as tomas previstas (RF-050 a RF-053, RF-060).
    /// Só um Cuidador regista.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha do tratamento.</param>
    /// <param name="dados">Dados do tratamento.</param>
    /// <param name="receita">Receita, ou <see langword="null"/> se não houver.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O tratamento registado.</returns>
    public async Task<TratamentoComReceita> RegistarAsync(
        Guid utilizadorId,
        Guid fichaId,
        DadosTratamento dados,
        DocumentoSubmetido? receita,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validados = Validar(dados, null);
                var receitaValidada = await ValidarReceitaAsync(receita, cancelamento).ConfigureAwait(false);

                var agora = this.relogio.AgoraUtc();
                var tratamento = new Tratamento(
                    fichaId,
                    validados.Medicamento!,
                    validados.Dose!,
                    validados.Grau!.Value,
                    validados.DataInicio!.Value,
                    validados.DataFim,
                    validados.Horas!,
                    utilizadorId,
                    agora);
                this.tratamentos.Adicionar(tratamento);

                var documento = receitaValidada is null
                    ? null
                    : await this.documentoService
                        .GuardarNaTransacaoAsync(fichaId, tratamento.Id, receitaValidada, utilizadorId, agora, cancelamento)
                        .ConfigureAwait(false);
                await this.tomaService.GerarParaTratamentoAsync(tratamento, agora, cancelamento).ConfigureAwait(false);
                return new TratamentoComReceita(tratamento, documento?.Id);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-28: lista os tratamentos em curso e os terminados de uma ficha a Cuidadores e Familiares associados (RF-054, RF-057).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="estado">Filtro opcional pelo estado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os tratamentos da ficha.</returns>
    public async Task<IReadOnlyList<TratamentoComReceita>> ListarAsync(
        Guid utilizadorId,
        Guid fichaId,
        EstadoTratamento? estado,
        CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        var lista = await this.tratamentos.DaFichaAsync(fichaId, estado, cancelamento).ConfigureAwait(false);
        var receitas = await this.documentoService.ReceitasPorTratamentoAsync(fichaId, cancelamento).ConfigureAwait(false);
        return lista.Select(t => new TratamentoComReceita(t, Receita(receitas, t.Id))).ToList();
    }

    /// <summary>
    /// OP-29: consulta um tratamento, em curso ou terminado (RF-054, RF-057).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="tratamentoId">Tratamento pedido.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O tratamento.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.RecursoNaoEncontrado"/>, a quem não está associado (RNF-005).</exception>
    public async Task<TratamentoComReceita> ObterAsync(Guid utilizadorId, Guid tratamentoId, CancellationToken cancelamento)
    {
        var tratamento = await this.ObterComAcessoAsync(utilizadorId, tratamentoId, this.autorizacao.ExigirConsultaAsync, cancelamento)
            .ConfigureAwait(false);
        return await this.ComReceitaAsync(tratamento, null, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-30: altera um tratamento em curso de uma ficha ativa; os campos em falta mantêm o valor. Alterar os horários só
    /// afeta as tomas futuras (RF-055, RF-060). Se a receita nova não for guardada, o tratamento fica como estava (RF-053).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="tratamentoId">Tratamento a alterar.</param>
    /// <param name="dados">Dados a alterar.</param>
    /// <param name="receita">Receita nova, ou <see langword="null"/> se não houver.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O tratamento alterado.</returns>
    public async Task<TratamentoComReceita> AtualizarAsync(
        Guid utilizadorId,
        Guid tratamentoId,
        DadosTratamento dados,
        DocumentoSubmetido? receita,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var tratamento = await this.ObterComAcessoAsync(
                    utilizadorId,
                    tratamentoId,
                    this.autorizacao.ExigirCuidadorDeFichaAtivaAsync,
                    cancelamento).ConfigureAwait(false);
                tratamento.ExigirEmCurso();
                var validados = Validar(dados, tratamento);
                var receitaValidada = await ValidarReceitaAsync(receita, cancelamento).ConfigureAwait(false);

                // A receita é guardada antes de alterar o tratamento: se falhar, nada muda (RF-053 CA2).
                var agora = this.relogio.AgoraUtc();
                var documento = receitaValidada is null
                    ? null
                    : await this.documentoService
                        .GuardarNaTransacaoAsync(tratamento.FichaId, tratamento.Id, receitaValidada, utilizadorId, agora, cancelamento)
                        .ConfigureAwait(false);

                tratamento.Atualizar(
                    validados.Medicamento ?? tratamento.Medicamento,
                    validados.Dose ?? tratamento.Dose,
                    validados.Grau ?? tratamento.GrauImportancia,
                    validados.DataFim ?? tratamento.DataFim,
                    validados.Horas ?? tratamento.Horarios.Select(h => h.HoraLocal).ToList());
                await this.tomaService.RegerarFuturasAsync(tratamento, agora, cancelamento).ConfigureAwait(false);
                return await this.ComReceitaAsync(tratamento, documento?.Id, cancelamento).ConfigureAwait(false);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Passo 4 do processo periódico: termina os tratamentos em curso cuja data de fim já passou, em hora de Lisboa
    /// (RF-050, RF-056). Não há tomas depois da data de fim, por isso nenhuma é removida.
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tratamentos terminados.</returns>
    public async Task<int> TerminarConcluidosAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var hojeLocal = this.relogio.Calendario().DiaLocal(agoraUtc);
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var concluidos = await this.tratamentos.ComDataFimUltrapassadaAsync(hojeLocal, cancelamento).ConfigureAwait(false);
                foreach (var tratamento in concluidos)
                {
                    tratamento.Terminar(agoraUtc);
                }

                return concluidos.Count;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-31: termina um tratamento em curso, também numa ficha arquivada; as tomas previstas posteriores deixam de existir e
    /// as anteriores mantêm a hora e o estado (RF-056, RF-057, RF-060).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="tratamentoId">Tratamento a terminar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O tratamento terminado.</returns>
    public async Task<TratamentoComReceita> TerminarAsync(Guid utilizadorId, Guid tratamentoId, CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var tratamento = await this.ObterComAcessoAsync(utilizadorId, tratamentoId, this.autorizacao.ExigirCuidadorAsync, cancelamento)
                    .ConfigureAwait(false);
                var agora = this.relogio.AgoraUtc();
                tratamento.Terminar(agora);
                await this.tomaService.RemoverFuturasAsync(tratamento, agora, cancelamento).ConfigureAwait(false);
                return await this.ComReceitaAsync(tratamento, null, cancelamento).ConfigureAwait(false);
            },
            cancelamento).ConfigureAwait(false);
    }

    private static Guid? Receita(IReadOnlyDictionary<Guid, Guid> receitas, Guid tratamentoId)
    {
        return receitas.TryGetValue(tratamentoId, out var id) ? id : null;
    }

    private static async Task<DocumentoValidado?> ValidarReceitaAsync(DocumentoSubmetido? receita, CancellationToken cancelamento)
    {
        return receita is null
            ? null
            : await DocumentoService
                .ValidarAsync(receita with { Categoria = nameof(CategoriaDocumento.ReceitaMedica) }, "receita", cancelamento)
                .ConfigureAwait(false);
    }

    // No registo (atual nulo) os campos são obrigatórios, exceto a data de fim; na atualização só se validam os campos
    // indicados (RF-050, RF-051, RF-055, RNF-028).
    private static DadosValidados Validar(DadosTratamento dados, Tratamento? atual)
    {
        var registo = atual is null;
        var validador = new ValidadorDados();

        var medicamento = ValidadorDados.Normalizar(dados.Medicamento);
        if (registo || dados.Medicamento is not null)
        {
            validador.Obrigatorio("medicamento", medicamento, ComprimentoMaximoMedicamento);
        }

        var dose = ValidadorDados.Normalizar(dados.Dose);
        if (registo || dados.Dose is not null)
        {
            validador.Obrigatorio("dose", dose, ComprimentoMaximoDose);
        }

        GrauImportancia? grau = null;
        var textoGrau = ValidadorDados.Normalizar(dados.GrauImportancia);
        if (textoGrau is not null && Enum.GetNames<GrauImportancia>().Contains(textoGrau, StringComparer.Ordinal))
        {
            grau = Enum.Parse<GrauImportancia>(textoGrau);
        }
        else if (registo || dados.GrauImportancia is not null)
        {
            validador.Erro("grauImportancia", "É obrigatório e tem de ser Leve, Moderado ou Grave.");
        }

        var dataInicio = atual?.DataInicio ?? dados.DataInicio;
        if (registo && dados.DataInicio is null)
        {
            validador.Erro("dataInicio", "É obrigatória, no formato AAAA-MM-DD.");
        }
        else if (!registo && dados.DataInicio is not null && dados.DataInicio != atual!.DataInicio)
        {
            validador.Erro("dataInicio", "Não pode ser alterada depois do registo do tratamento.");
        }

        if (dados.DataFim < dataInicio)
        {
            validador.Erro("dataFim", "Não pode ser anterior à data de início.");
        }

        var horas = registo || dados.Horarios is not null ? ValidarHorarios(dados.Horarios, validador) : null;

        validador.LancarSeInvalido();
        return new DadosValidados(medicamento, dose, grau, dados.DataInicio, dados.DataFim, horas);
    }

    private static List<TimeOnly> ValidarHorarios(IReadOnlyList<string>? horarios, ValidadorDados validador)
    {
        var horas = new List<TimeOnly>();
        if (horarios is null || horarios.Count == 0)
        {
            validador.Erro("horarios", "É obrigatório indicar pelo menos uma hora de toma, no formato HH:mm.");
            return horas;
        }

        for (var i = 0; i < horarios.Count; i++)
        {
            if (TimeOnly.TryParseExact(horarios[i]?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            {
                horas.Add(hora);
            }
            else
            {
                validador.Erro($"horarios[{i}]", "Tem de ser uma hora no formato HH:mm, por exemplo 08:00.");
            }
        }

        if (horas.Distinct().Count() != horas.Count)
        {
            validador.Erro("horarios", "Não pode repetir a mesma hora.");
        }

        return horas;
    }

    private async Task<Tratamento> ObterComAcessoAsync(
        Guid utilizadorId,
        Guid tratamentoId,
        Func<Guid, Guid, CancellationToken, Task<AcessoFicha>> exigir,
        CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentos.ObterPorIdAsync(tratamentoId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrado);
        try
        {
            await exigir(utilizadorId, tratamento.FichaId, cancelamento).ConfigureAwait(false);
        }
        catch (DominioException erro) when (erro.Codigo == CodigosErro.FichaNaoEncontrada)
        {
            // Nas operações sobre um tratamento, quem não está associado à ficha recebe RECURSO_NAO_ENCONTRADO (RNF-005).
            throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrado);
        }

        return tratamento;
    }

    private async Task<TratamentoComReceita> ComReceitaAsync(Tratamento tratamento, Guid? receitaNova, CancellationToken cancelamento)
    {
        if (receitaNova is not null)
        {
            return new TratamentoComReceita(tratamento, receitaNova);
        }

        var receitas = await this.documentoService.ReceitasPorTratamentoAsync(tratamento.FichaId, cancelamento).ConfigureAwait(false);
        return new TratamentoComReceita(tratamento, Receita(receitas, tratamento.Id));
    }

    private sealed record DadosValidados(
        string? Medicamento,
        string? Dose,
        GrauImportancia? Grau,
        DateOnly? DataInicio,
        DateOnly? DataFim,
        IReadOnlyList<TimeOnly>? Horas);
}
