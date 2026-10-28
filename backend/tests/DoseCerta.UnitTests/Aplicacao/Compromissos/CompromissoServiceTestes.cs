// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Compromissos;

/// <summary>
/// Testes unitários de <see cref="CompromissoService"/> (F008, OP-41 a OP-44; RF-080 a RF-082, RF-116, RF-117).
/// </summary>
public class CompromissoServiceTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;
    private static readonly DateTimeOffset Consulta = new(2026, 11, 12, 10, 30, 15, TimeSpan.Zero);
    private static readonly string[] CamposObrigatorios = { "dataHora", "local", "motivo" };
    private static readonly string[] CamposDeTexto = { "local", "motivo" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly CompromissoService servico;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Guid familiar;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissoServiceTestes"/> class.
    /// </summary>
    public CompromissoServiceTestes()
    {
        this.servico = new CompromissoService(this.repositorios, new AutorizacaoFichaService(this.repositorios), this.unidadeDeTrabalho, this.relogio);
        this.ficha = FichasDeTeste.DeTerceiro(this.cuidador);
        this.familiar = FichasDeTeste.Associar(this.ficha, Papel.Familiar);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// OP-41 e RF-080: o Cuidador regista; sem antecedência fica 24 horas e o instante fica guardado em UTC, ao segundo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComAntecedenciaPorOmissao()
    {
        var registado = await this.servico.RegistarAsync(
            this.cuidador,
            this.ficha.Id,
            new DadosCompromisso(new DateTimeOffset(2026, 11, 12, 10, 30, 15, TimeSpan.FromHours(1)), "  Centro de Saúde Fictício ", "Consulta de rotina", null),
            CancellationToken.None);

        var guardado = Assert.Single(this.repositorios.Compromissos);
        Assert.Same(guardado, registado);
        Assert.Equal(this.ficha.Id, registado.FichaId);
        Assert.Equal(new DateTime(2026, 11, 12, 9, 30, 15, DateTimeKind.Utc), registado.DataHoraUtc);
        Assert.Equal("Centro de Saúde Fictício", registado.Local);
        Assert.Equal(ParametrosDeNegocio.AntecedenciaPorOmissaoMin, registado.AntecedenciaAvisoMin);
        Assert.Equal(this.cuidador, registado.CriadoPorId);
        Assert.Equal(Agora, registado.CriadoEmUtc);
        Assert.Null(registado.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RF-080 e RNF-028: data e hora, local e motivo são obrigatórios; a resposta indica cada campo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarSemCamposObrigatoriosIndicaOsCampos()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.RegistarAsync(this.cuidador, this.ficha.Id, new DadosCompromisso(null, " ", null, null), CancellationToken.None));

        Assert.Equal(CamposObrigatorios, erro.Campos.Select(c => c.Campo));
        Assert.Empty(this.repositorios.Compromissos);
    }

    /// <summary>
    /// RF-116: uma antecedência fora dos valores permitidos é recusada, com o campo em erro.
    /// </summary>
    /// <param name="minutos">Antecedência pedida.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(2881)]
    public async Task RegistarComAntecedenciaInvalidaERecusado(int minutos)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.RegistarAsync(this.cuidador, this.ficha.Id, Dados(minutos), CancellationToken.None));

        Assert.Equal("antecedenciaAvisoMinutos", Assert.Single(erro.Campos).Campo);
    }

    /// <summary>
    /// O local e o motivo respeitam o comprimento máximo do modelo de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComTextoDemasiadoLongoERecusado()
    {
        var dados = new DadosCompromisso(Consulta, new string('a', 151), new string('b', 251), 60);

        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.RegistarAsync(this.cuidador, this.ficha.Id, dados, CancellationToken.None));

        Assert.Equal(CamposDeTexto, erro.Campos.Select(c => c.Campo));
    }

    /// <summary>
    /// RP-11, RP-13 e RNF-005: o Familiar recebe 403, quem não está associado 404 e a ficha arquivada 409.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarSoPorCuidadorDeFichaAtiva()
    {
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RegistarAsync(this.familiar, this.ficha.Id, Dados(60), CancellationToken.None));
        var doEstranho = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RegistarAsync(Guid.NewGuid(), this.ficha.Id, Dados(60), CancellationToken.None));
        this.ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RegistarAsync(this.cuidador, this.ficha.Id, Dados(60), CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, doEstranho.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Empty(this.repositorios.Compromissos);
    }

    /// <summary>
    /// OP-42: o Familiar lista, também numa ficha arquivada; o intervalo usa os dias de Lisboa, inclusive.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarComIntervaloEmDiasDeLisboa()
    {
        // 12 de outubro, 23:30 UTC, é já 13 de outubro em Lisboa (UTC+1).
        var dia13 = this.NovoCompromisso(new DateTime(2026, 10, 12, 23, 30, 0, DateTimeKind.Utc));
        var dia12 = this.NovoCompromisso(new DateTime(2026, 10, 12, 22, 30, 0, DateTimeKind.Utc));
        var dia20 = this.NovoCompromisso(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc));
        this.ficha.Arquivar(Agora);

        var todos = await this.servico.ListarAsync(this.familiar, this.ficha.Id, null, null, CancellationToken.None);
        var filtrados = await this.servico.ListarAsync(this.familiar, this.ficha.Id, new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 19), CancellationToken.None);

        Assert.Equal(new[] { dia12.Id, dia13.Id, dia20.Id }, todos.Select(c => c.Id));
        Assert.Equal(dia13.Id, Assert.Single(filtrados).Id);
    }

    /// <summary>
    /// OP-42: <c>ate</c> anterior a <c>de</c> é recusado; quem não está associado recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarValidaOIntervaloEAAssociacao()
    {
        var invertido = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ListarAsync(this.cuidador, this.ficha.Id, new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 12), CancellationToken.None));
        var doEstranho = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.ListarAsync(Guid.NewGuid(), this.ficha.Id, null, null, CancellationToken.None));

        Assert.Equal("ate", Assert.Single(invertido.Campos).Campo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, doEstranho.Codigo);
    }

    /// <summary>
    /// OP-43 e RF-117: a edição altera os dados e repõe o aviso para ser calculado com a data nova.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarAlteraOsDadosERepoeOAviso()
    {
        var compromisso = this.NovoCompromisso(new DateTime(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc));
        compromisso.MarcarAvisoEnviado(Agora);

        var editado = await this.servico.EditarAsync(
            this.cuidador,
            compromisso.Id,
            new DadosCompromisso(Consulta.AddDays(1), "Hospital Fictício", "Análises", 120),
            CancellationToken.None);

        Assert.Same(compromisso, editado);
        Assert.Equal(new DateTime(2026, 11, 13, 10, 30, 15, DateTimeKind.Utc), editado.DataHoraUtc);
        Assert.Equal("Hospital Fictício", editado.Local);
        Assert.Equal("Análises", editado.Motivo);
        Assert.Equal(120, editado.AntecedenciaAvisoMin);
        Assert.Null(editado.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// OP-43 e OP-44: compromisso inexistente ou de ficha não associada dá 404 <c>RECURSO_NAO_ENCONTRADO</c>, o Familiar 403
    /// e a ficha arquivada 409, sem alterar nem remover o compromisso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarERemoverRecusamSemAcesso()
    {
        var compromisso = this.NovoCompromisso(new DateTime(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc));

        var inexistente = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.EditarAsync(this.cuidador, Guid.NewGuid(), Dados(60), CancellationToken.None));
        var doEstranho = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RemoverAsync(Guid.NewGuid(), compromisso.Id, CancellationToken.None));
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.EditarAsync(this.familiar, compromisso.Id, Dados(60), CancellationToken.None));
        var removerFamiliar = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RemoverAsync(this.familiar, compromisso.Id, CancellationToken.None));
        this.ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RemoverAsync(this.cuidador, compromisso.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.RecursoNaoEncontrado, inexistente.Codigo);
        Assert.Equal(CodigosErro.RecursoNaoEncontrado, doEstranho.Codigo);
        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.AcessoProibido, removerFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Single(this.repositorios.Compromissos);
        Assert.Equal("Consulta de rotina", compromisso.Motivo);
    }

    /// <summary>
    /// OP-44 e RF-082: o Cuidador remove o compromisso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RemoverApagaOCompromisso()
    {
        var compromisso = this.NovoCompromisso(new DateTime(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc));

        await this.servico.RemoverAsync(this.cuidador, compromisso.Id, CancellationToken.None);

        Assert.Empty(this.repositorios.Compromissos);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    private static DadosCompromisso Dados(int? antecedencia)
    {
        return new DadosCompromisso(Consulta, "Centro de Saúde Fictício", "Consulta de rotina", antecedencia);
    }

    private Compromisso NovoCompromisso(DateTime dataHoraUtc)
    {
        var compromisso = new Compromisso(this.ficha.Id, dataHoraUtc, "Centro de Saúde Fictício", "Consulta de rotina", 60, this.cuidador, Agora);
        this.repositorios.Compromissos.Add(compromisso);
        return compromisso;
    }
}
