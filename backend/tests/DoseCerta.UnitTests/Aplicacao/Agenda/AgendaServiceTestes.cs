// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Agenda;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Agenda;

/// <summary>
/// Testes unitários de <see cref="AgendaService"/> (F008, OP-45; RF-083 a RF-089, RNF-047). A F007 foi retirada: a agenda
/// só tem tomas e compromissos.
/// </summary>
public class AgendaServiceTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;
    private static readonly TimeOnly[] AsOito = { new(8, 0) };
    private static readonly string[] CamposObrigatorios = { "vista", "data" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly CalendarioLisboa calendario = new();
    private readonly AgendaService servico;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Guid familiar;
    private readonly Ficha ficha;
    private readonly Tratamento tratamento;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgendaServiceTestes"/> class.
    /// </summary>
    public AgendaServiceTestes()
    {
        this.servico = new AgendaService(new AutorizacaoFichaService(this.repositorios), this.repositorios, this.repositorios, this.relogio);
        this.ficha = FichasDeTeste.DeTerceiro(this.cuidador);
        this.familiar = FichasDeTeste.Associar(this.ficha, Papel.Familiar);
        this.repositorios.Fichas.Add(this.ficha);
        this.tratamento = this.NovoTratamento(this.ficha);
    }

    /// <summary>
    /// A vista e a data são obrigatórias; a vista só aceita dia, semana ou mes.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task VistaEDataSaoObrigatorias()
    {
        var semNada = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ConsultarAsync(this.cuidador, null, null, CancellationToken.None));
        var vistaInvalida = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ConsultarAsync(this.cuidador, "ano", new DateOnly(2026, 10, 22), CancellationToken.None));

        Assert.Equal(CamposObrigatorios, semNada.Campos.Select(c => c.Campo));
        Assert.Equal("vista", Assert.Single(vistaInvalida.Campos).Campo);
    }

    /// <summary>
    /// A vista lê dia, semana e mes, sem distinguir maiúsculas.
    /// </summary>
    /// <param name="texto">Texto recebido.</param>
    /// <param name="esperada">Vista esperada.</param>
    [Theory]
    [InlineData("dia", VistaAgenda.Dia)]
    [InlineData("Semana", VistaAgenda.Semana)]
    [InlineData(" mes ", VistaAgenda.Mes)]
    public void LerVista(string texto, VistaAgenda esperada)
    {
        Assert.Equal(esperada, AgendaService.LerVista(texto));
    }

    /// <summary>
    /// RF-083: sem fichas associadas, a agenda é vazia.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemFichasAAgendaEVazia()
    {
        var agenda = await this.servico.ConsultarAsync(Guid.NewGuid(), "mes", new DateOnly(2026, 10, 22), CancellationToken.None);

        Assert.Empty(agenda);
    }

    /// <summary>
    /// RF-083, RF-084 e RF-089: a vista diária junta tomas e compromissos do dia de Lisboa, por ordem, com o nome da ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task VistaDiariaJuntaTomasECompromissosPorOrdem()
    {
        var toma = this.NovaToma(Lisboa(22, 8));
        var compromisso = this.NovoCompromisso(this.ficha, Lisboa(22, 10, 30));
        this.NovaToma(Lisboa(23, 8));
        this.NovaToma(Lisboa(21, 23, 30));

        var agenda = await this.servico.ConsultarAsync(this.cuidador, "dia", new DateOnly(2026, 10, 22), CancellationToken.None);

        Assert.Equal(new[] { toma.Id, compromisso.Id }, agenda.Select(i => i.Id));
        var itemToma = agenda[0];
        Assert.Equal(TipoItemAgenda.Toma, itemToma.Tipo);
        Assert.Equal("Metformina 500 mg", itemToma.Titulo);
        Assert.Equal("Maria Fictícia Santos", itemToma.NomeFicha);
        Assert.Equal(EstadoToma.Pendente, itemToma.Estado);
        Assert.True(itemToma.Confirmavel);
        var itemCompromisso = agenda[1];
        Assert.Equal(TipoItemAgenda.Compromisso, itemCompromisso.Tipo);
        Assert.Equal("Consulta de rotina", itemCompromisso.Titulo);
        Assert.Null(itemCompromisso.Estado);
        Assert.False(itemCompromisso.Confirmavel);
    }

    /// <summary>
    /// RF-087 e RF-048: o Familiar vê os eventos, mas nada é confirmável; uma toma já confirmada também não.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConfirmavelSoEmTomasPendentesDoCuidador()
    {
        var pendente = this.NovaToma(Lisboa(22, 8));
        var confirmada = this.NovaToma(Lisboa(22, 20));
        confirmada.Confirmar(this.cuidador, Lisboa(22, 20), this.calendario);

        var doCuidador = await this.servico.ConsultarAsync(this.cuidador, "dia", new DateOnly(2026, 10, 22), CancellationToken.None);
        var doFamiliar = await this.servico.ConsultarAsync(this.familiar, "dia", new DateOnly(2026, 10, 22), CancellationToken.None);

        Assert.True(doCuidador.Single(i => i.Id == pendente.Id).Confirmavel);
        Assert.False(doCuidador.Single(i => i.Id == confirmada.Id).Confirmavel);
        Assert.Equal(EstadoToma.Confirmada, doCuidador.Single(i => i.Id == confirmada.Id).Estado);
        Assert.Equal(2, doFamiliar.Count);
        Assert.All(doFamiliar, i => Assert.False(i.Confirmavel));
    }

    /// <summary>
    /// RF-083 e RNF-005: só as fichas ativas do Utilizador; as arquivadas e as de outras pessoas não aparecem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoFichasAtivasDoUtilizador()
    {
        var minha = this.NovoCompromisso(this.ficha, Lisboa(22, 10));
        var arquivada = FichasDeTeste.DeTerceiro(this.cuidador);
        this.repositorios.Fichas.Add(arquivada);
        this.NovoCompromisso(arquivada, Lisboa(22, 11));
        arquivada.Arquivar(Agora);
        var deOutro = FichasDeTeste.DeTerceiro(Guid.NewGuid());
        this.repositorios.Fichas.Add(deOutro);
        this.NovoCompromisso(deOutro, Lisboa(22, 12));

        var agenda = await this.servico.ConsultarAsync(this.cuidador, "semana", new DateOnly(2026, 10, 22), CancellationToken.None);

        Assert.Equal(minha.Id, Assert.Single(agenda).Id);
    }

    /// <summary>
    /// RF-085 e RNF-047: a semana do último domingo de outubro vai de segunda, 19, às 00:00 de domingo, 26, em hora de Lisboa.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task VistaSemanalRespeitaOsLimitesDeLisboa()
    {
        var segunda = this.NovoCompromisso(this.ficha, Lisboa(19, 0));
        var domingo = this.NovoCompromisso(this.ficha, Lisboa(25, 23, 59));
        this.NovoCompromisso(this.ficha, Lisboa(18, 23, 59));
        this.NovoCompromisso(this.ficha, Lisboa(26, 0));

        var agenda = await this.servico.ConsultarAsync(this.cuidador, "semana", new DateOnly(2026, 10, 22), CancellationToken.None);

        Assert.Equal(new[] { segunda.Id, domingo.Id }, agenda.Select(i => i.Id));
    }

    /// <summary>
    /// RF-086 e RF-083: a vista mensal mostra os compromissos de todo o mês, mesmo para lá da janela das tomas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task VistaMensalTemOMesInteiro()
    {
        var primeiro = this.NovoCompromisso(this.ficha, new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var ultimo = this.NovoCompromisso(this.ficha, new DateTime(2027, 3, 31, 22, 59, 0, DateTimeKind.Utc));
        this.NovoCompromisso(this.ficha, new DateTime(2027, 3, 31, 23, 0, 0, DateTimeKind.Utc));

        var agenda = await this.servico.ConsultarAsync(this.cuidador, "mes", new DateOnly(2027, 3, 15), CancellationToken.None);

        Assert.Equal(new[] { primeiro.Id, ultimo.Id }, agenda.Select(i => i.Id));
    }

    // Instante UTC de um dia de outubro de 2026 a uma hora de Lisboa.
    private static DateTime Lisboa(int dia, int hora, int minuto = 0)
    {
        return new CalendarioLisboa().ParaUtc(new DateOnly(2026, 10, dia), new TimeOnly(hora, minuto));
    }

    private Tratamento NovoTratamento(Ficha daFicha)
    {
        var novo = new Tratamento(daFicha.Id, "Metformina", "500 mg", GrauImportancia.Moderado, new DateOnly(2026, 10, 21), null, AsOito, this.cuidador, Agora);
        this.repositorios.Tratamentos.Add(novo);
        return novo;
    }

    private Toma NovaToma(DateTime previstaUtc)
    {
        var toma = new Toma(this.tratamento.Id, previstaUtc, "Metformina", "500 mg", GrauImportancia.Moderado);
        this.repositorios.Tomas.Add(toma);
        return toma;
    }

    private Compromisso NovoCompromisso(Ficha daFicha, DateTime dataHoraUtc)
    {
        var compromisso = new Compromisso(daFicha.Id, dataHoraUtc, "Centro de Saúde Fictício", "Consulta de rotina", 60, this.cuidador, Agora);
        this.repositorios.Compromissos.Add(compromisso);
        return compromisso;
    }
}
