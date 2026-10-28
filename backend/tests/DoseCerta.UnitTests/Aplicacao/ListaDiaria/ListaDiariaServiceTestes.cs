// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.ListaDiaria;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.ListaDiaria;

/// <summary>
/// Testes unitários da lista de tarefas diária consolidada (F010: RF-100, RF-101, RF-104, RF-087, RNF-047). Em novembro a hora
/// de Lisboa coincide com UTC.
/// </summary>
public class ListaDiariaServiceTestes
{
    private static readonly DateOnly Dia = new(2026, 11, 2);

    private static readonly string[] NomesPorOrdem = { "João Exemplo", "Maria Exemplo" };

    private static readonly bool[] ConfirmaveisDeHoje = { false, true, true };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly RelogioFixo relogio = new(Utc(11, 2, 9));
    private readonly ListaDiariaService servico;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Ficha fichaX;
    private readonly Ficha fichaY;
    private readonly Ficha fichaZ;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListaDiariaServiceTestes"/> class.
    /// </summary>
    public ListaDiariaServiceTestes()
    {
        this.servico = new ListaDiariaService(
            this.repositorios,
            this.repositorios,
            new AutorizacaoFichaService(this.repositorios),
            this.relogio);
        this.fichaX = this.CriarFicha("Maria Exemplo", this.cuidador);
        this.fichaY = this.CriarFicha("João Exemplo", this.cuidador);
        this.fichaZ = this.CriarFicha("Rui Exemplo", Guid.NewGuid());
        this.fichaZ.Associar(this.cuidador, Papel.Familiar, Utc(10, 1, 9));
    }

    /// <summary>
    /// RF-100, CA1, CA2 e CA5, e RF-101: reúne as tomas e os compromissos do dia das fichas em que o Utilizador é Cuidador, com o
    /// nome do utente; as fichas em que é Familiar não entram e os compromissos não são confirmáveis.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ReuneTomasECompromissosDasFichasEmQueECuidador()
    {
        var tomaX = this.Toma(this.fichaX, Utc(11, 2, 8));
        var compromissoX = this.Compromisso(this.fichaX, Utc(11, 2, 10, 30));
        var tomaY = this.Toma(this.fichaY, Utc(11, 2, 8));
        var compromissoY = this.Compromisso(this.fichaY, Utc(11, 2, 15));
        this.Toma(this.fichaZ, Utc(11, 2, 9));
        this.Compromisso(this.fichaZ, Utc(11, 2, 11));

        var itens = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        Assert.Equal(new[] { tomaY.Id, tomaX.Id, compromissoX.Id, compromissoY.Id }, itens.Select(i => i.Id));
        Assert.DoesNotContain(itens, i => i.FichaId == this.fichaZ.Id);
        Assert.Equal(NomesPorOrdem, itens.Take(2).Select(i => i.NomeFicha));
        var toma = itens[1];
        Assert.Equal(TipoItemListaDiaria.Toma, toma.Tipo);
        Assert.Equal("Metformina 500 mg", toma.Titulo);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.True(toma.Confirmavel);
        var compromisso = itens[2];
        Assert.Equal(TipoItemListaDiaria.Compromisso, compromisso.Tipo);
        Assert.Equal("Consulta de rotina", compromisso.Titulo);
        Assert.Null(compromisso.Estado);
        Assert.False(compromisso.Confirmavel);
    }

    /// <summary>
    /// RF-100, CA3: os itens de uma ficha arquivada não aparecem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaNaoAparece()
    {
        this.Toma(this.fichaX, Utc(11, 2, 8));
        this.Compromisso(this.fichaX, Utc(11, 2, 10));
        var tomaY = this.Toma(this.fichaY, Utc(11, 2, 8));
        this.fichaX.Arquivar(Utc(11, 1, 9));

        var itens = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        Assert.Equal(tomaY.Id, Assert.Single(itens).Id);
    }

    /// <summary>
    /// RF-100, CA4: a lista de hoje inclui a toma do dia anterior que continua pendente, mas não as já confirmadas nem os
    /// compromissos do dia anterior; sem data, o dia é o de hoje em Lisboa.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task HojeIncluiTomasPendentesDoDiaAnterior()
    {
        this.relogio.Agora = Utc(11, 2, 0, 30);
        var pendente = this.Toma(this.fichaX, Utc(11, 1, 23, 30));
        this.Toma(this.fichaX, Utc(11, 1, 8), EstadoToma.Confirmada, "Ana Fictícia", Utc(11, 1, 8, 5));
        this.Compromisso(this.fichaX, Utc(11, 1, 18));

        var semData = await this.servico.ConsultarAsync(this.cuidador, null, CancellationToken.None);
        var comData = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        var item = Assert.Single(semData);
        Assert.Equal(pendente.Id, item.Id);
        Assert.True(item.Confirmavel);
        Assert.Equal(semData, comData);
    }

    /// <summary>
    /// RF-100: num dia que não é hoje, a lista só tem os itens desse dia.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OutroDiaNaoIncluiTomasDoDiaAnterior()
    {
        this.relogio.Agora = Utc(11, 5, 10);
        this.Toma(this.fichaX, Utc(11, 1, 23, 30));
        var doDia = this.Toma(this.fichaX, Utc(11, 2, 8));

        var itens = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        Assert.Equal(doDia.Id, Assert.Single(itens).Id);
    }

    /// <summary>
    /// RF-104: uma toma confirmada, a tempo ou com atraso, indica quem a confirmou e quando; uma toma não confirmada não indica
    /// nenhum dos dois. Nenhuma delas é confirmável.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaConfirmadaIndicaQuemConfirmouEQuando()
    {
        this.relogio.Agora = Utc(11, 4, 9);
        this.Toma(this.fichaX, Utc(11, 2, 6), EstadoToma.NaoConfirmada);
        this.Toma(this.fichaX, Utc(11, 2, 10), EstadoToma.Confirmada, "Ana Fictícia", Utc(11, 2, 10, 5));
        this.Toma(this.fichaX, Utc(11, 2, 12), EstadoToma.ConfirmadaComAtraso, "Rui Fictício", Utc(11, 2, 14, 30));

        var itens = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        Assert.Equal(new EstadoToma?[] { EstadoToma.NaoConfirmada, EstadoToma.Confirmada, EstadoToma.ConfirmadaComAtraso }, itens.Select(i => i.Estado));
        Assert.Equal(new[] { null, "Ana Fictícia", "Rui Fictício" }, itens.Select(i => i.ConfirmadaPor));
        Assert.Equal(new DateTime?[] { null, Utc(11, 2, 10, 5), Utc(11, 2, 14, 30) }, itens.Select(i => i.ConfirmadaEmUtc));
        Assert.All(itens, i => Assert.False(i.Confirmavel));
    }

    /// <summary>
    /// RF-064 e RF-068: só é confirmável a toma pendente cujo dia já começou e que ainda não tem 24 horas; a pendente do dia
    /// anterior com mais de 24 horas aparece, mas não é confirmável, e a de amanhã ainda não é confirmável.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoEConfirmavelATomaPendenteDoDiaComMenosDe24Horas()
    {
        this.relogio.Agora = Utc(11, 2, 12);
        var antiga = this.Toma(this.fichaX, Utc(11, 1, 8));
        var recente = this.Toma(this.fichaX, Utc(11, 1, 20));
        var deHoje = this.Toma(this.fichaX, Utc(11, 2, 20));
        var deAmanha = this.Toma(this.fichaX, Utc(11, 3, 8));

        var hoje = await this.servico.ConsultarAsync(this.cuidador, null, CancellationToken.None);
        var amanha = await this.servico.ConsultarAsync(this.cuidador, Dia.AddDays(1), CancellationToken.None);

        Assert.Equal(new[] { antiga.Id, recente.Id, deHoje.Id }, hoje.Select(i => i.Id));
        Assert.Equal(ConfirmaveisDeHoje, hoje.Select(i => i.Confirmavel));
        var item = Assert.Single(amanha);
        Assert.Equal(deAmanha.Id, item.Id);
        Assert.False(item.Confirmavel);
    }

    /// <summary>
    /// Os itens estão ordenados por hora; à mesma hora, a toma vem antes do compromisso e depois ordena-se pelo nome do utente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OrdenaPorHoraEDesempataPorTipoENomeDoUtente()
    {
        var compromissoX = this.Compromisso(this.fichaX, Utc(11, 2, 8));
        var tomaX = this.Toma(this.fichaX, Utc(11, 2, 8));
        var tomaY = this.Toma(this.fichaY, Utc(11, 2, 8));
        var cedo = this.Toma(this.fichaX, Utc(11, 2, 7, 30));

        var itens = await this.servico.ConsultarAsync(this.cuidador, Dia, CancellationToken.None);

        Assert.Equal(new[] { cedo.Id, tomaY.Id, tomaX.Id, compromissoX.Id }, itens.Select(i => i.Id));
    }

    /// <summary>
    /// RNF-047: no dia em que a hora muda para a de inverno (25 horas), o dia vai das 00:00 às 24:00 de Lisboa.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DiaDaMudancaParaHoraDeInvernoTem25Horas()
    {
        this.relogio.Agora = Utc(10, 20, 9);
        var vespera = this.Toma(this.fichaX, Utc(10, 24, 22, 30));
        var inicio = this.Toma(this.fichaX, Utc(10, 24, 23, 30));
        var fim = this.Toma(this.fichaX, Utc(10, 25, 23, 30));

        var dia25 = await this.servico.ConsultarAsync(this.cuidador, new DateOnly(2026, 10, 25), CancellationToken.None);
        var dia24 = await this.servico.ConsultarAsync(this.cuidador, new DateOnly(2026, 10, 24), CancellationToken.None);

        Assert.Equal(new[] { inicio.Id, fim.Id }, dia25.Select(i => i.Id));
        Assert.Equal(vespera.Id, Assert.Single(dia24).Id);
    }

    /// <summary>
    /// RNF-047: no dia em que a hora muda para a de verão (23 horas), o dia vai das 00:00 às 24:00 de Lisboa.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DiaDaMudancaParaHoraDeVeraoTem23Horas()
    {
        var ultima = this.Toma(this.fichaX, new DateTime(2027, 3, 28, 22, 30, 0, DateTimeKind.Utc));
        this.Toma(this.fichaX, new DateTime(2027, 3, 28, 23, 30, 0, DateTimeKind.Utc));
        var primeira = this.Toma(this.fichaX, new DateTime(2027, 3, 28, 0, 30, 0, DateTimeKind.Utc));

        var itens = await this.servico.ConsultarAsync(this.cuidador, new DateOnly(2027, 3, 28), CancellationToken.None);

        Assert.Equal(new[] { primeira.Id, ultima.Id }, itens.Select(i => i.Id));
    }

    /// <summary>
    /// RP-15: quem não é Cuidador de nenhuma ficha ativa recebe uma lista vazia, mesmo sendo Familiar de uma ficha com itens.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemFichasDeCuidadorDevolveListaVazia()
    {
        var familiar = Guid.NewGuid();
        this.fichaX.Associar(familiar, Papel.Familiar, Utc(10, 1, 9));
        this.Toma(this.fichaX, Utc(11, 2, 8));

        var doFamiliar = await this.servico.ConsultarAsync(familiar, Dia, CancellationToken.None);
        var semAssociacao = await this.servico.ConsultarAsync(Guid.NewGuid(), Dia, CancellationToken.None);

        Assert.Empty(doFamiliar);
        Assert.Empty(semAssociacao);
    }

    private static DateTime Utc(int mes, int dia, int hora, int minuto = 0)
    {
        return new DateTime(2026, mes, dia, hora, minuto, 0, DateTimeKind.Utc);
    }

    private Ficha CriarFicha(string nome, Guid principalId)
    {
        var ficha = new Ficha(nome, new DateOnly(1945, 3, 10), "+351 910 000 001", null, principalId, Utc(10, 1, 9));
        ficha.AssociarCuidadorPrincipal(principalId, provisorio: false, Utc(10, 1, 9));
        this.repositorios.Fichas.Add(ficha);
        return ficha;
    }

    private TomaDoDia Toma(
        Ficha ficha,
        DateTime previstaEmUtc,
        EstadoToma estado = EstadoToma.Pendente,
        string? confirmadaPor = null,
        DateTime? confirmadaEmUtc = null)
    {
        var toma = new TomaDoDia(Guid.NewGuid(), ficha.Id, ficha.NomeCompleto, "Metformina", "500 mg", previstaEmUtc, estado, confirmadaPor, confirmadaEmUtc);
        this.repositorios.TomasDoDia.Add(toma);
        return toma;
    }

    private CompromissoDoPeriodo Compromisso(Ficha ficha, DateTime dataHoraUtc)
    {
        var compromisso = new CompromissoDoPeriodo(Guid.NewGuid(), ficha.Id, ficha.NomeCompleto, "Consulta de rotina", dataHoraUtc);
        this.repositorios.CompromissosDoPeriodo.Add(compromisso);
        return compromisso;
    }
}
