// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Tomas;
using DoseCerta.Application.Tratamentos;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Tratamentos;

/// <summary>
/// Testes unitários de <see cref="TratamentoService"/> e da geração das tomas pelo <see cref="TomaService"/>
/// (F005, RF-050 a RF-057, RF-060, RNF-046).
/// </summary>
public class TratamentoServiceTestes
{
    private static readonly DateTime Agora = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string[] CamposObrigatorios = { "medicamento", "dose", "grauImportancia", "dataInicio", "horarios" };
    private static readonly string[] CamposInvalidos = { "grauImportancia", "dataFim", "horarios[0]", "horarios" };
    private static readonly string[] HorasDoDia = { "08:00", "20:00" };
    private static readonly string[] AsDez = { "10:00" };
    private static readonly string[] AsOito = { "08:00" };
    private static readonly string[] AsNove = { "09:00" };
    private static readonly string[] HorasInvalidas = { "8h", "08:00", "08:00" };
    private static readonly string[] HorasAlteradas = { "08:00", "21:00" };
    private static readonly int[] HorasAnteriores = { 8, 20, 8 };
    private static readonly int[] HorasFuturas = { 21, 8, 21 };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly ArmazenamentoEmMemoria armazenamento = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly TratamentoService servico;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Guid familiar = Guid.NewGuid();
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratamentoServiceTestes"/> class.
    /// </summary>
    public TratamentoServiceTestes()
    {
        var unidadeDeTrabalho = new UnidadeDeTrabalhoComReversao(this.repositorios);
        var autorizacao = new AutorizacaoFichaService(this.repositorios);
        var documentos = new DocumentoService(this.repositorios, this.armazenamento, autorizacao, unidadeDeTrabalho, this.relogio);
        var tomas = new TomaService(this.repositorios, this.repositorios, this.repositorios, this.repositorios, autorizacao, unidadeDeTrabalho, this.relogio);
        this.servico = new TratamentoService(this.repositorios, documentos, tomas, autorizacao, unidadeDeTrabalho, this.relogio);

        // Ficha ativa desde 2026-10-02 09:00 UTC.
        this.ficha = new Ficha("Maria Fictícia Santos", new DateOnly(1941, 5, 17), "+351 910 000 001", null, this.cuidador, Agora.AddDays(-30));
        this.ficha.AssociarCuidadorPrincipal(this.cuidador, provisorio: false, Agora.AddDays(-30));
        this.ficha.Associar(this.familiar, Papel.Familiar, Agora.AddDays(-30));
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-050 CA1 e RF-060 CA1: o tratamento fica em curso e geram-se 6 tomas pendentes (08:00 e 20:00 de 02 a 04 de novembro).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarGeraUmaTomaPorHorarioEPorDia()
    {
        var registado = await this.RegistarAsync(Dados());

        var tratamento = Assert.Single(this.repositorios.Tratamentos);
        Assert.Same(tratamento, registado.Tratamento);
        Assert.Equal(EstadoTratamento.EmCurso, tratamento.Estado);
        Assert.Equal(this.ficha.Id, tratamento.FichaId);
        Assert.Null(registado.ReceitaDocumentoId);
        Assert.Equal(6, this.repositorios.Tomas.Count);
        Assert.All(this.repositorios.Tomas, t => Assert.Equal(EstadoToma.Pendente, t.Estado));
        Assert.Equal(new DateTime(2026, 11, 4, 20, 0, 0, DateTimeKind.Utc), this.repositorios.Tomas.Max(t => t.PrevistaEmUtc));
    }

    /// <summary>
    /// RF-060: sem data de fim, geram-se só as tomas da janela móvel de 30 dias.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarSemDataFimGeraAJanelaDeTrintaDias()
    {
        await this.RegistarAsync(Dados(inicio: new DateOnly(2026, 11, 1), fim: null, horarios: AsDez));

        Assert.Equal(30, this.repositorios.Tomas.Count);
        Assert.Equal(new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc), this.repositorios.Tomas.Min(t => t.PrevistaEmUtc));
        Assert.Equal(new DateTime(2026, 11, 30, 10, 0, 0, DateTimeKind.Utc), this.repositorios.Tomas.Max(t => t.PrevistaEmUtc));
    }

    /// <summary>
    /// DM-19: com a data de início no passado, não se geram tomas anteriores ao instante em que a ficha ficou ativa.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarNoPassadoNaoGeraAntesDaFichaAtiva()
    {
        await this.RegistarAsync(Dados(inicio: new DateOnly(2026, 9, 1), fim: new DateOnly(2026, 10, 3), horarios: AsOito));

        // A ficha está ativa desde 2026-10-02 09:00 UTC; as 08:00 de Lisboa de 3 de outubro são 07:00 UTC (hora de verão).
        Assert.Equal(new DateTime(2026, 10, 3, 7, 0, 0, DateTimeKind.Utc), Assert.Single(this.repositorios.Tomas).PrevistaEmUtc);
    }

    /// <summary>
    /// RF-052: a receita fica associada ao tratamento, na categoria receita médica.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComReceitaAssociaOFicheiroAoTratamento()
    {
        var registado = await this.RegistarAsync(Dados(), FicheirosTeste.Documento(FicheirosTeste.Pdf, categoria: null, nome: "receita.pdf"));

        var documento = Assert.Single(this.repositorios.Documentos);
        Assert.Equal(registado.Tratamento.Id, documento.TratamentoId);
        Assert.Equal(CategoriaDocumento.ReceitaMedica, documento.Categoria);
        Assert.Equal(documento.Id, registado.ReceitaDocumentoId);
        Assert.Single(this.armazenamento.Ficheiros);
    }

    /// <summary>
    /// RF-050 CA2, CA4 e RF-051: os campos obrigatórios em falta e os valores inválidos são indicados e nada é registado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComDadosInvalidosIndicaOsCampos()
    {
        var vazio = await Assert.ThrowsAsync<ValidacaoException>(() => this.RegistarAsync(new DadosTratamento(null, " ", null, null, null, null)));
        var invalido = await Assert.ThrowsAsync<ValidacaoException>(() => this.RegistarAsync(
            Dados(grau: "Urgente", inicio: new DateOnly(2026, 11, 2), fim: new DateOnly(2026, 11, 1), horarios: HorasInvalidas)));

        Assert.Equal(CamposObrigatorios, vazio.Campos.Select(c => c.Campo));
        Assert.Equal(CamposInvalidos, invalido.Campos.Select(c => c.Campo));
        Assert.Empty(this.repositorios.Tratamentos);
        Assert.Empty(this.repositorios.Tomas);
    }

    /// <summary>
    /// RF-050 CA3, RNF-005 e RF-120: o Familiar recebe ACESSO_PROIBIDO, quem não está associado recebe FICHA_NAO_ENCONTRADA
    /// e numa ficha arquivada a operação é recusada com FICHA_ARQUIVADA.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarSoPorUmCuidadorNumaFichaAtiva()
    {
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(() => this.servico.RegistarAsync(this.familiar, this.ficha.Id, Dados(), null, CancellationToken.None));
        var doEstranho = await Assert.ThrowsAsync<DominioException>(() => this.servico.RegistarAsync(Guid.NewGuid(), this.ficha.Id, Dados(), null, CancellationToken.None));
        this.ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(() => this.RegistarAsync(Dados()));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, doEstranho.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Empty(this.repositorios.Tratamentos);
    }

    /// <summary>
    /// RF-053 CA1: se a receita não puder ser guardada, o tratamento não é registado e nada é gravado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ReceitaQueNaoEGuardadaNaoRegistaNada()
    {
        this.armazenamento.GravacoesAntesDeFalhar = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.RegistarAsync(Dados(), FicheirosTeste.Documento(FicheirosTeste.Pdf)));

        Assert.Equal(CodigosErro.DocumentoNaoGuardado, erro.Codigo);
        Assert.Empty(this.repositorios.Tratamentos);
        Assert.Empty(this.repositorios.Tomas);
        Assert.Empty(this.repositorios.Documentos);
    }

    /// <summary>
    /// RF-055 CA1 e RF-060 CA3: alterar a dose e o horário das 20:00 para as 21:00 a 2026-11-03 às 12:00 só muda as tomas
    /// futuras; as anteriores mantêm a hora e a dose.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarSoAfetaAsTomasFuturas()
    {
        var registado = await this.RegistarAsync(Dados());
        this.relogio.Agora = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        var alterado = await this.servico.AtualizarAsync(
            this.cuidador,
            registado.Tratamento.Id,
            new DadosTratamento(null, "850 mg", null, null, null, HorasAlteradas),
            null,
            CancellationToken.None);

        Assert.Equal("Metformina", alterado.Tratamento.Medicamento);
        Assert.Equal("850 mg", alterado.Tratamento.Dose);
        var tomas = this.repositorios.Tomas.OrderBy(t => t.PrevistaEmUtc).ToList();
        var anteriores = tomas.Where(t => t.PrevistaEmUtc < this.relogio.Agora).ToList();
        var futuras = tomas.Where(t => t.PrevistaEmUtc > this.relogio.Agora).ToList();
        Assert.Equal(HorasAnteriores, anteriores.Select(t => t.PrevistaEmUtc.Hour));
        Assert.All(anteriores, t => Assert.Equal("500 mg", t.Dose));
        Assert.Equal(HorasFuturas, futuras.Select(t => t.PrevistaEmUtc.Hour));
        Assert.All(futuras, t => Assert.Equal("850 mg", t.Dose));
    }

    /// <summary>
    /// RF-055: só se altera um tratamento em curso, por um Cuidador associado; a data de início não muda.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarRespeitaPapelEstadoEDataDeInicio()
    {
        var id = (await this.RegistarAsync(Dados())).Tratamento.Id;
        var semAlteracao = new DadosTratamento(null, "1 g", null, null, null, null);

        var doEstranho = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(Guid.NewGuid(), id, semAlteracao, null, CancellationToken.None));
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(this.familiar, id, semAlteracao, null, CancellationToken.None));
        var inexistente = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(this.cuidador, Guid.NewGuid(), semAlteracao, null, CancellationToken.None));
        var inicio = await Assert.ThrowsAsync<ValidacaoException>(() => this.servico.AtualizarAsync(
            this.cuidador, id, new DadosTratamento(null, null, null, new DateOnly(2026, 12, 1), null, null), null, CancellationToken.None));
        await this.servico.TerminarAsync(this.cuidador, id, CancellationToken.None);
        var terminado = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(this.cuidador, id, semAlteracao, null, CancellationToken.None));

        Assert.Equal(CodigosErro.RecursoNaoEncontrado, doEstranho.Codigo);
        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.RecursoNaoEncontrado, inexistente.Codigo);
        Assert.Equal("dataInicio", Assert.Single(inicio.Campos).Campo);
        Assert.Equal(CodigosErro.TratamentoTerminado, terminado.Codigo);
        Assert.Equal("500 mg", Assert.Single(this.repositorios.Tratamentos).Dose);
    }

    /// <summary>
    /// RF-053 CA2: se a receita nova não for guardada, o tratamento e as tomas ficam exatamente como estavam.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ReceitaNovaQueNaoEGuardadaDeixaOTratamentoComoEstava()
    {
        var id = (await this.RegistarAsync(Dados())).Tratamento.Id;
        var tomasAntes = this.repositorios.Tomas.Select(t => t.Id).ToList();
        this.armazenamento.GravacoesAntesDeFalhar = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(
            this.cuidador, id, new DadosTratamento(null, "850 mg", null, null, null, AsNove), FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoNaoGuardado, erro.Codigo);
        var tratamento = Assert.Single(this.repositorios.Tratamentos);
        Assert.Equal("500 mg", tratamento.Dose);
        Assert.Equal(2, tratamento.Horarios.Count);
        Assert.Equal(tomasAntes, this.repositorios.Tomas.Select(t => t.Id));
    }

    /// <summary>
    /// RF-056 CA1 e RF-060 CA2: terminar a 2026-11-03 às 12:00 apaga as tomas posteriores e mantém as anteriores; não se termina
    /// duas vezes.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarRemoveAsTomasFuturas()
    {
        var id = (await this.RegistarAsync(Dados())).Tratamento.Id;
        this.relogio.Agora = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        var terminado = await this.servico.TerminarAsync(this.cuidador, id, CancellationToken.None);
        var outraVez = await Assert.ThrowsAsync<DominioException>(() => this.servico.TerminarAsync(this.cuidador, id, CancellationToken.None));

        Assert.Equal(EstadoTratamento.Terminado, terminado.Tratamento.Estado);
        Assert.Equal(this.relogio.Agora, terminado.Tratamento.TerminadoEmUtc);
        Assert.Equal(3, this.repositorios.Tomas.Count);
        Assert.All(this.repositorios.Tomas, t => Assert.True(t.PrevistaEmUtc < this.relogio.Agora));
        Assert.Equal(CodigosErro.TratamentoJaTerminado, outraVez.Codigo);
    }

    /// <summary>
    /// RF-056 (passo 4 do processo periódico): o tratamento com data de fim a 2026-11-04 continua em curso nesse dia e é
    /// terminado no dia seguinte, em hora de Lisboa; o tratamento sem data de fim nunca é terminado e um segundo ciclo não faz nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarConcluidosTerminaSoDepoisDaDataDeFim()
    {
        var comFim = (await this.RegistarAsync(Dados())).Tratamento;
        var continuo = (await this.RegistarAsync(Dados(medicamento: "Paracetamol", inicio: new DateOnly(2026, 11, 2)))).Tratamento;
        var ultimoInstanteDoDia = new DateTime(2026, 11, 4, 23, 59, 59, DateTimeKind.Utc);
        var diaSeguinte = new DateTime(2026, 11, 5, 0, 0, 30, DateTimeKind.Utc);

        var noDiaDoFim = await this.servico.TerminarConcluidosAsync(ultimoInstanteDoDia, CancellationToken.None);
        var depois = await this.servico.TerminarConcluidosAsync(diaSeguinte, CancellationToken.None);
        var outraVez = await this.servico.TerminarConcluidosAsync(diaSeguinte.AddSeconds(30), CancellationToken.None);

        Assert.Equal(0, noDiaDoFim);
        Assert.Equal(1, depois);
        Assert.Equal(0, outraVez);
        Assert.Equal(EstadoTratamento.Terminado, comFim.Estado);
        Assert.Equal(diaSeguinte, comFim.TerminadoEmUtc);
        Assert.Equal(EstadoTratamento.EmCurso, continuo.Estado);
    }

    /// <summary>
    /// RF-120: numa ficha arquivada o tratamento pode ser terminado, mas não pode ser alterado; o Familiar não termina.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarNumaFichaArquivadaEPermitido()
    {
        var id = (await this.RegistarAsync(Dados())).Tratamento.Id;
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(() => this.servico.TerminarAsync(this.familiar, id, CancellationToken.None));
        this.ficha.Arquivar(Agora);

        var alteracao = await Assert.ThrowsAsync<DominioException>(() => this.servico.AtualizarAsync(
            this.cuidador, id, new DadosTratamento(null, "1 g", null, null, null, null), null, CancellationToken.None));
        var terminado = await this.servico.TerminarAsync(this.cuidador, id, CancellationToken.None);

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, alteracao.Codigo);
        Assert.Equal(EstadoTratamento.Terminado, terminado.Tratamento.Estado);
    }

    /// <summary>
    /// RF-054 e RF-057: Cuidadores e Familiares consultam os tratamentos em curso e os terminados, com a receita; o filtro
    /// pelo estado é aplicado e quem não está associado não vê nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarEConsultarIncluemOsTerminados()
    {
        var comReceita = await this.RegistarAsync(Dados(), FicheirosTeste.Documento(FicheirosTeste.Png, nome: "receita.png"));
        var paraTerminar = await this.RegistarAsync(Dados(medicamento: "Paracetamol"));
        await this.servico.TerminarAsync(this.cuidador, paraTerminar.Tratamento.Id, CancellationToken.None);

        var todos = await this.servico.ListarAsync(this.familiar, this.ficha.Id, null, CancellationToken.None);
        var terminados = await this.servico.ListarAsync(this.cuidador, this.ficha.Id, EstadoTratamento.Terminado, CancellationToken.None);
        var consultado = await this.servico.ObterAsync(this.familiar, comReceita.Tratamento.Id, CancellationToken.None);
        var listaDoEstranho = await Assert.ThrowsAsync<DominioException>(() => this.servico.ListarAsync(Guid.NewGuid(), this.ficha.Id, null, CancellationToken.None));
        var consultaDoEstranho = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(Guid.NewGuid(), comReceita.Tratamento.Id, CancellationToken.None));

        Assert.Equal(2, todos.Count);
        Assert.Equal("Paracetamol", Assert.Single(terminados).Tratamento.Medicamento);
        Assert.Equal(comReceita.ReceitaDocumentoId, consultado.ReceitaDocumentoId);
        Assert.Equal(comReceita.ReceitaDocumentoId, todos.Single(t => t.Tratamento.Id == comReceita.Tratamento.Id).ReceitaDocumentoId);
        Assert.Null(todos.Single(t => t.Tratamento.Id == paraTerminar.Tratamento.Id).ReceitaDocumentoId);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, listaDoEstranho.Codigo);
        Assert.Equal(CodigosErro.RecursoNaoEncontrado, consultaDoEstranho.Codigo);
    }

    private static DadosTratamento Dados(
        string medicamento = "Metformina",
        string grau = "Moderado",
        DateOnly? inicio = null,
        DateOnly? fim = null,
        IReadOnlyList<string>? horarios = null)
    {
        return new DadosTratamento(
            medicamento,
            "500 mg",
            grau,
            inicio ?? new DateOnly(2026, 11, 2),
            fim ?? (inicio is null ? new DateOnly(2026, 11, 4) : null),
            horarios ?? HorasDoDia);
    }

    private Task<TratamentoComReceita> RegistarAsync(DadosTratamento dados, DocumentoSubmetido? receita = null)
    {
        return this.servico.RegistarAsync(this.cuidador, this.ficha.Id, dados, receita, CancellationToken.None);
    }
}
