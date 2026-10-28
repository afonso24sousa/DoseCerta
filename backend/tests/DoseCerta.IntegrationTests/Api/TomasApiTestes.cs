// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-32 a OP-34 (F006, I038): a API completa com o SQL Server e o relógio real. As tomas são criadas
/// na base de dados com instantes relativos ao momento do teste; o tratamento é registado para daqui a 60 dias, para as tomas
/// que gera não interferirem.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class TomasApiTestes
{
    private static readonly string[] AsOito = { "08:00" };

    private readonly FabricaApi fabrica;
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TomasApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public TomasApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-32: o Cuidador e o Familiar listam as tomas do dia, com o JSON do contrato; o filtro pelo estado aplica-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarTomasDaFicha()
    {
        using var cenario = await this.CenarioAsync();
        var previsto = DadosTeste.Agora().AddMinutes(-10);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, previsto);
        var dia = this.Dia(previsto);

        using var doCuidador = await cenario.Cuidador.GetAsync($"api/v1/fichas/{cenario.FichaId}/tomas?de={dia}&ate={dia}");
        using var doFamiliar = await cenario.Familiar.GetAsync($"api/v1/fichas/{cenario.FichaId}/tomas?de={dia}&ate={dia}");
        using var confirmadas = await cenario.Cuidador.GetAsync($"api/v1/fichas/{cenario.FichaId}/tomas?de={dia}&ate={dia}&estado=Confirmada");

        Assert.Equal(HttpStatusCode.OK, doCuidador.StatusCode);
        var toma = Assert.Single((await ClienteApi.JsonAsync(doCuidador)).EnumerateArray());
        Assert.Equal(tomaId, toma.GetProperty("id").GetGuid());
        Assert.Equal(cenario.TratamentoId, toma.GetProperty("tratamentoId").GetGuid());
        Assert.Equal("Metformina", toma.GetProperty("medicamento").GetString());
        Assert.Equal("500 mg", toma.GetProperty("dose").GetString());
        Assert.Equal("Moderado", toma.GetProperty("grauImportancia").GetString());
        Assert.Equal(this.calendario.ParaLocal(previsto), toma.GetProperty("previstaEm").GetDateTimeOffset());
        Assert.Equal(this.calendario.ParaLocal(previsto).Offset, toma.GetProperty("previstaEm").GetDateTimeOffset().Offset);
        Assert.Equal("Pendente", toma.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, toma.GetProperty("confirmadaPor").ValueKind);
        Assert.Equal(JsonValueKind.Null, toma.GetProperty("confirmadaEm").ValueKind);
        Assert.Equal(1, (await ClienteApi.JsonAsync(doFamiliar)).GetArrayLength());
        Assert.Equal(0, (await ClienteApi.JsonAsync(confirmadas)).GetArrayLength());
    }

    /// <summary>
    /// OP-32: o intervalo é obrigatório e tem no máximo 31 dias; quem não está associado recebe 404 e sem token dá 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarValidaOIntervaloEOAcesso()
    {
        using var cenario = await this.CenarioAsync();
        var url = $"api/v1/fichas/{cenario.FichaId}/tomas";
        using var anonimo = this.fabrica.CreateClient();

        using var semAte = await cenario.Cuidador.GetAsync($"{url}?de=2026-10-01");
        using var longo = await cenario.Cuidador.GetAsync($"{url}?de=2026-10-01&ate=2026-11-01");
        using var mesInteiro = await cenario.Cuidador.GetAsync($"{url}?de=2026-10-01&ate=2026-10-31");
        using var doEstranho = await cenario.Estranho.GetAsync($"{url}?de=2026-10-01&ate=2026-10-31");
        using var semToken = await anonimo.GetAsync($"{url}?de=2026-10-01&ate=2026-10-31");

        Assert.Equal(HttpStatusCode.BadRequest, semAte.StatusCode);
        Assert.Contains("ate", await CamposAsync(semAte));
        Assert.Equal(HttpStatusCode.BadRequest, longo.StatusCode);
        Assert.Contains("ate", await CamposAsync(longo));
        Assert.Equal(HttpStatusCode.OK, mesInteiro.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
    }

    /// <summary>
    /// OP-33: o Cuidador confirma (200 com quem e quando); a segunda confirmação dá 409 <c>TOMA_JA_CONFIRMADA</c> com a
    /// confirmação gravada; o Familiar recebe 403 e a toma inexistente 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConfirmarToma()
    {
        using var cenario = await this.CenarioAsync();
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, DadosTeste.Agora().AddMinutes(-10));

        using var doFamiliar = await cenario.Familiar.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);
        using var confirmada = await cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);
        using var segunda = await cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);
        using var inexistente = await cenario.Cuidador.PostAsync($"api/v1/tomas/{Guid.NewGuid()}/confirmacao", null);
        using var doEstranho = await cenario.Estranho.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));

        Assert.Equal(HttpStatusCode.OK, confirmada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(confirmada);
        Assert.Equal(tomaId, corpo.GetProperty("id").GetGuid());
        Assert.Equal("Confirmada", corpo.GetProperty("estado").GetString());
        Assert.Equal(cenario.CuidadorId, corpo.GetProperty("confirmadaPor").GetProperty("id").GetGuid());
        Assert.Equal("Pessoa Fictícia", corpo.GetProperty("confirmadaPor").GetProperty("nome").GetString());
        var confirmadaEm = corpo.GetProperty("confirmadaEm").GetDateTimeOffset();

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        var erro = await ClienteApi.JsonAsync(segunda);
        Assert.Equal("TOMA_JA_CONFIRMADA", erro.GetProperty("codigo").GetString());
        Assert.StartsWith("A toma já foi confirmada por Pessoa Fictícia em ", erro.GetProperty("mensagem").GetString(), StringComparison.Ordinal);
        var confirmacao = erro.GetProperty("confirmacao");
        Assert.Equal(cenario.CuidadorId, confirmacao.GetProperty("confirmadaPor").GetProperty("id").GetGuid());
        Assert.Equal(confirmadaEm, confirmacao.GetProperty("confirmadaEm").GetDateTimeOffset());

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(inexistente));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(doEstranho));

        var gravada = await this.fabrica.ComContextoAsync(c => c.Tomas.AsNoTracking().SingleAsync(t => t.Id == tomaId));
        Assert.Equal(EstadoToma.Confirmada, gravada.Estado);
        Assert.Equal(cenario.CuidadorId, gravada.ConfirmadaPorId);
    }

    /// <summary>
    /// OP-33 e TR-13: a toma de amanhã ainda não pode ser confirmada (409 <c>TOMA_FORA_DA_JANELA</c>) e fica pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaDeAmanhaFicaForaDaJanela()
    {
        using var cenario = await this.CenarioAsync();
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, DadosTeste.Agora().AddHours(30));

        using var resposta = await cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("TOMA_FORA_DA_JANELA", await ClienteApi.CodigoErroAsync(resposta));
        var gravada = await this.fabrica.ComContextoAsync(c => c.Tomas.AsNoTracking().SingleAsync(t => t.Id == tomaId));
        Assert.Equal(EstadoToma.Pendente, gravada.Estado);
    }

    /// <summary>
    /// OP-33 e TR-14: passadas 24 horas, a toma pendente já não pode ser confirmada (409 <c>TOMA_NAO_CONFIRMAVEL</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaComVinteEQuatroHorasNaoEConfirmavel()
    {
        using var cenario = await this.CenarioAsync();
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, DadosTeste.Agora().AddHours(-25));

        using var resposta = await cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("TOMA_NAO_CONFIRMAVEL", await ClienteApi.CodigoErroAsync(resposta));
    }

    /// <summary>
    /// OP-33 e RP-13: numa ficha arquivada, a toma passada ainda é confirmada (não dá <c>FICHA_ARQUIVADA</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaAindaConfirma()
    {
        using var cenario = await this.CenarioAsync();
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, DadosTeste.Agora().AddMinutes(-10));
        using var arquivo = await cenario.Cuidador.PostAsync($"api/v1/fichas/{cenario.FichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        using var resposta = await cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Confirmada", (await ClienteApi.JsonAsync(resposta)).GetProperty("estado").GetString());
    }

    /// <summary>
    /// RNF-044: dois Cuidadores a confirmar ao mesmo tempo dão uma resposta 200 e uma 409 <c>TOMA_JA_CONFIRMADA</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConfirmacoesEmParaleloDaoUma200EUma409()
    {
        using var cenario = await this.CenarioAsync();
        var outro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var outroCuidador = outro.Cliente;
        await ClienteFichas.AssociarAsync(this.fabrica, cenario.FichaId, outro.Id, Papel.Cuidador);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, DadosTeste.Agora().AddMinutes(-10));

        var respostas = await Task.WhenAll(
            cenario.Cuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null),
            outroCuidador.PostAsync($"api/v1/tomas/{tomaId}/confirmacao", null));
        try
        {
            Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
            var conflito = Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
            var erro = await ClienteApi.JsonAsync(conflito);
            Assert.Equal("TOMA_JA_CONFIRMADA", erro.GetProperty("codigo").GetString());
            Assert.Equal(JsonValueKind.Object, erro.GetProperty("confirmacao").ValueKind);
        }
        finally
        {
            foreach (var resposta in respostas)
            {
                resposta.Dispose();
            }
        }
    }

    /// <summary>
    /// OP-34: o calendário tem só as tomas pendentes das próximas 48 horas das fichas ativas em que é Cuidador; sem tomas, a
    /// lista é vazia.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CalendarioDeTomas()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        var proxima = await this.NovaTomaAsync(cenario.TratamentoId, agora.AddHours(2));
        await this.NovaTomaAsync(cenario.TratamentoId, agora.AddHours(50));
        await this.NovaTomaAsync(cenario.TratamentoId, agora.AddHours(-2));

        // Ficha arquivada do mesmo Cuidador: a toma futura não entra (o arquivo apaga-a e a ficha não está ativa).
        var arquivadaId = await ClienteFichas.CriarFichaAsync(cenario.Cuidador, "Rosa Fictícia");
        var tratamentoArquivada = await RegistarAsync(cenario.Cuidador, arquivadaId);
        await this.NovaTomaAsync(tratamentoArquivada, agora.AddHours(3));
        using var arquivo = await cenario.Cuidador.PostAsync($"api/v1/fichas/{arquivadaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        using var doCuidador = await cenario.Cuidador.GetAsync("api/v1/calendario/tomas");
        using var doFamiliar = await cenario.Familiar.GetAsync("api/v1/calendario/tomas");
        using var doEstranho = await cenario.Estranho.GetAsync("api/v1/calendario/tomas");

        Assert.Equal(HttpStatusCode.OK, doCuidador.StatusCode);
        var toma = Assert.Single((await ClienteApi.JsonAsync(doCuidador)).EnumerateArray());
        Assert.Equal(proxima, toma.GetProperty("tomaId").GetGuid());
        Assert.Equal(cenario.FichaId, toma.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", toma.GetProperty("nomeFicha").GetString());
        Assert.Equal("Metformina", toma.GetProperty("medicamento").GetString());
        Assert.Equal("500 mg", toma.GetProperty("dose").GetString());
        Assert.Equal(this.calendario.ParaLocal(agora.AddHours(2)), toma.GetProperty("previstaEm").GetDateTimeOffset());
        Assert.False(toma.TryGetProperty("id", out _));
        Assert.Equal(0, (await ClienteApi.JsonAsync(doFamiliar)).GetArrayLength());
        Assert.Equal(0, (await ClienteApi.JsonAsync(doEstranho)).GetArrayLength());
    }

    private static async Task<Guid> RegistarAsync(HttpClient cliente, Guid fichaId)
    {
        var dia = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var pedido = new
        {
            medicamento = "Metformina",
            dose = "500 mg",
            grauImportancia = "Moderado",
            dataInicio = dia,
            dataFim = dia,
            horarios = AsOito,
        };
        using var resposta = await cliente.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", pedido);
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private static async Task<IEnumerable<string?>> CamposAsync(HttpResponseMessage resposta)
    {
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return corpo.GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
    }

    private string Dia(DateTime instanteUtc)
    {
        return this.calendario.DiaLocal(instanteUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private Task<Guid> NovaTomaAsync(Guid tratamentoId, DateTime previstaUtc)
    {
        return this.fabrica.ComContextoAsync(async contexto =>
        {
            var toma = new Toma(tratamentoId, previstaUtc, "Metformina", "500 mg", GrauImportancia.Moderado);
            contexto.Tomas.Add(toma);
            await contexto.SaveChangesAsync();
            return toma.Id;
        });
    }

    private async Task<Cenario> CenarioAsync()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var fichaId = await ClienteFichas.CriarFichaAsync(cuidador.Cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiar.Id, Papel.Familiar);
        var tratamentoId = await RegistarAsync(cuidador.Cliente, fichaId);
        return new Cenario(cuidador.Cliente, cuidador.Id, familiar.Cliente, estranho.Cliente, fichaId, tratamentoId);
    }

    /// <summary>
    /// Ficha de um Cuidador com um tratamento, um Familiar associado e um Utilizador sem associação, cada um com o seu cliente autenticado.
    /// </summary>
    /// <param name="Cuidador">Cliente do Cuidador principal.</param>
    /// <param name="CuidadorId">Identificador do Cuidador principal.</param>
    /// <param name="Familiar">Cliente do Familiar.</param>
    /// <param name="Estranho">Cliente de um Utilizador não associado.</param>
    /// <param name="FichaId">Identificador da ficha.</param>
    /// <param name="TratamentoId">Identificador do tratamento.</param>
    private sealed record Cenario(
        HttpClient Cuidador,
        Guid CuidadorId,
        HttpClient Familiar,
        HttpClient Estranho,
        Guid FichaId,
        Guid TratamentoId) : IDisposable
    {
        public void Dispose()
        {
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
