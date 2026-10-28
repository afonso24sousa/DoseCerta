// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Text.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-45 (F008, I044): a agenda reúne as tomas e os compromissos das fichas ativas do Utilizador.
/// Usa datas de 2027, fora da janela das tomas geradas por outros testes; os dados são criados na base de dados de testes.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class AgendaApiTestes
{
    private static readonly DateOnly Dia = new(2027, 3, 10);

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgendaApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public AgendaApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-083 a RF-089: a vista diária devolve as tomas e os compromissos do dia, por ordem do início, com o formato do contrato;
    /// as fichas arquivadas e as de outras pessoas não aparecem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AgendaDoCuidador()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Maria Fictícia Santos", cuidador.Id);
        var compromisso = await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(Dia, 10, 30));
        var toma = await DadosAgenda.CriarTomaAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(Dia, 8));
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(Dia.AddDays(1), 9));

        var arquivadaId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Rosa Fictícia", cuidador.Id);
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, arquivadaId, cuidador.Id, DadosAgenda.Lisboa(Dia, 11));
        await DadosAgenda.ArquivarAsync(this.fabrica, arquivadaId);
        var outro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteOutro = outro.Cliente;
        var deOutroId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Carlos Fictício", outro.Id);
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, deOutroId, outro.Id, DadosAgenda.Lisboa(Dia, 12));

        using var resposta = await cliente.GetAsync("api/v1/agenda?vista=dia&data=2027-03-10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var itens = (await ClienteApi.JsonAsync(resposta)).EnumerateArray().ToList();
        Assert.Equal(new[] { toma.Id, compromisso.Id }, itens.Select(i => i.GetProperty("id").GetGuid()));

        var itemToma = itens[0];
        Assert.Equal("Toma", itemToma.GetProperty("tipo").GetString());
        Assert.Equal(fichaId, itemToma.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", itemToma.GetProperty("nomeFicha").GetString());
        Assert.Equal("Metformina 500 mg", itemToma.GetProperty("titulo").GetString());
        Assert.Equal("2027-03-10T08:00:00+00:00", itemToma.GetProperty("inicio").GetString());
        Assert.Equal("Pendente", itemToma.GetProperty("estado").GetString());
        Assert.True(itemToma.GetProperty("confirmavel").GetBoolean());

        var itemCompromisso = itens[1];
        Assert.Equal("Compromisso", itemCompromisso.GetProperty("tipo").GetString());
        Assert.Equal("Consulta de rotina", itemCompromisso.GetProperty("titulo").GetString());
        Assert.False(itemCompromisso.TryGetProperty("estado", out _));
        Assert.False(itemCompromisso.GetProperty("confirmavel").GetBoolean());
    }

    /// <summary>
    /// RF-048 e RF-087: o Familiar vê a agenda da ficha, mas nada é confirmável.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarVeAAgendaSemConfirmar()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCuidador = cuidador.Cliente;
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = familiar.Cliente;
        var fichaId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Maria Fictícia Santos", cuidador.Id, (familiar.Id, Papel.Familiar));
        await DadosAgenda.CriarTomaAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(Dia, 8));
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(Dia, 10));

        using var resposta = await cliente.GetAsync("api/v1/agenda?vista=semana&data=2027-03-10");

        var itens = (await ClienteApi.JsonAsync(resposta)).EnumerateArray().ToList();
        Assert.Equal(2, itens.Count);
        Assert.All(itens, i => Assert.False(i.GetProperty("confirmavel").GetBoolean()));
    }

    /// <summary>
    /// RF-085: a vista semanal vai de segunda-feira a domingo; RF-083: sem fichas, a agenda é vazia.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task VistaSemanalESemFichas()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var semFichas = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteSemFichas = semFichas.Cliente;
        var fichaId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Maria Fictícia Santos", cuidador.Id);

        // 10 de março de 2027 é quarta-feira: a semana vai de segunda, 8, a domingo, 14.
        var segunda = await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(new DateOnly(2027, 3, 8), 0));
        var domingo = await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(new DateOnly(2027, 3, 14), 23, 59));
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(new DateOnly(2027, 3, 7), 23, 59));
        await DadosAgenda.CriarCompromissoAsync(this.fabrica, fichaId, cuidador.Id, DadosAgenda.Lisboa(new DateOnly(2027, 3, 15), 0));

        using var semana = await cliente.GetAsync("api/v1/agenda?vista=semana&data=2027-03-10");
        using var mes = await cliente.GetAsync("api/v1/agenda?vista=mes&data=2027-03-10");
        using var vazia = await clienteSemFichas.GetAsync("api/v1/agenda?vista=mes&data=2027-03-10");

        Assert.Equal(new[] { segunda.Id, domingo.Id }, Ids(await ClienteApi.JsonAsync(semana)));
        Assert.Equal(4, (await ClienteApi.JsonAsync(mes)).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, vazia.StatusCode);
        Assert.Equal(0, (await ClienteApi.JsonAsync(vazia)).GetArrayLength());
    }

    /// <summary>
    /// RNF-028: a vista e a data são obrigatórias e validadas; sem token dá 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ValidacaoDosParametros()
    {
        var utilizador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = utilizador.Cliente;
        using var anonimo = this.fabrica.CreateClient();

        using var semVista = await cliente.GetAsync("api/v1/agenda?data=2027-03-10");
        using var vistaInvalida = await cliente.GetAsync("api/v1/agenda?vista=ano&data=2027-03-10");
        using var semData = await cliente.GetAsync("api/v1/agenda?vista=dia");
        using var dataInvalida = await cliente.GetAsync("api/v1/agenda?vista=dia&data=2027-02-30");
        using var semToken = await anonimo.GetAsync("api/v1/agenda?vista=dia&data=2027-03-10");

        Assert.Equal(HttpStatusCode.BadRequest, semVista.StatusCode);
        Assert.Equal("vista", await CampoAsync(semVista));
        Assert.Equal("vista", await CampoAsync(vistaInvalida));
        Assert.Equal("data", await CampoAsync(semData));
        Assert.Equal(HttpStatusCode.BadRequest, dataInvalida.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
    }

    private static List<Guid> Ids(JsonElement lista)
    {
        return lista.EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToList();
    }

    private static async Task<string?> CampoAsync(HttpResponseMessage resposta)
    {
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return Assert.Single(corpo.GetProperty("campos").EnumerateArray()).GetProperty("campo").GetString();
    }
}
