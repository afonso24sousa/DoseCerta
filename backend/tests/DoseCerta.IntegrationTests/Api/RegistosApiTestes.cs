// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-46 a OP-49 (F009, I045): a API completa com o SQL Server. A ficha tem um Cuidador principal, outro
/// Cuidador e um Familiar; há ainda um Utilizador sem associação.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class RegistosApiTestes
{
    private static readonly string[] CamposDaOcorrencia = { "descricao", "ocorridaEm" };
    private static readonly string[] DescricoesDoMaisRecente = { "Julho, dia 2.", "Julho, dia 1.", "Janeiro." };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistosApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public RegistosApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-46 e OP-48 (RF-090, RF-092 a RF-094): o Cuidador principal regista; o outro Cuidador e o Familiar veem a ocorrência na
    /// consulta seguinte, com o autor e os dois instantes; quem não está associado recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarOcorrenciaEConsultar()
    {
        using var cenario = await this.CenarioAsync();

        using var registada = await cenario.Principal.PostAsJsonAsync(
            $"api/v1/fichas/{cenario.FichaId}/ocorrencias",
            new { descricao = "Queda ligeira sem lesões.", ocorridaEm = "2026-10-07T15:05:00+01:00" });

        Assert.Equal(HttpStatusCode.Created, registada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(registada);
        var id = corpo.GetProperty("id").GetGuid();
        Assert.Equal(cenario.FichaId, corpo.GetProperty("fichaId").GetGuid());
        Assert.Equal("Ocorrencia", corpo.GetProperty("tipo").GetString());
        Assert.Equal("Queda ligeira sem lesões.", corpo.GetProperty("descricao").GetString());
        Assert.Equal(cenario.PrincipalId, corpo.GetProperty("autor").GetProperty("id").GetGuid());
        Assert.Equal("Ana Principal", corpo.GetProperty("autor").GetProperty("nome").GetString());
        Assert.Equal("2026-10-07T15:05:00+01:00", corpo.GetProperty("ocorridaEm").GetString());
        var criadaEm = corpo.GetProperty("criadaEm").GetDateTimeOffset();
        Assert.InRange(DateTime.UtcNow - criadaEm.UtcDateTime, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        var guardada = await this.fabrica.ComContextoAsync(c => c.Ocorrencias.AsNoTracking().SingleAsync(o => o.Id == id));
        Assert.Equal(new DateTime(2026, 10, 7, 14, 5, 0), guardada.OcorreuEmUtc);
        Assert.Equal(cenario.PrincipalId, guardada.AutorId);

        using var doCuidador = await cenario.Cuidador.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Ocorrencia");
        using var doFamiliar = await cenario.Familiar.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Ocorrencia");
        using var doEstranho = await cenario.Estranho.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Ocorrencia");

        Assert.Equal(corpo.GetRawText(), (await ClienteApi.JsonAsync(doCuidador))[0].GetRawText());
        Assert.Equal(id, Assert.Single((await ClienteApi.JsonAsync(doFamiliar)).EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
    }

    /// <summary>
    /// OP-47 e OP-49 (RF-091, RF-093, RF-095, RP-15): um Cuidador que não é o principal regista a nota e o principal vê-a; o
    /// Familiar recebe 403 e quem não está associado recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NotasSaoSoDosCuidadores()
    {
        using var cenario = await this.CenarioAsync();

        using var registada = await cenario.Cuidador.PostAsJsonAsync(
            $"api/v1/fichas/{cenario.FichaId}/notas",
            new { descricao = "Reforçar a hidratação durante a tarde." });

        Assert.Equal(HttpStatusCode.Created, registada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(registada);
        Assert.Equal("Nota", corpo.GetProperty("tipo").GetString());
        Assert.Equal("Bruno Cuidador", corpo.GetProperty("autor").GetProperty("nome").GetString());
        Assert.Equal(JsonValueKind.Null, corpo.GetProperty("ocorridaEm").ValueKind);

        using var doPrincipal = await cenario.Principal.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Nota");
        using var doFamiliar = await cenario.Familiar.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Nota");
        using var doEstranho = await cenario.Estranho.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Nota");
        using var ocorrencias = await cenario.Principal.GetAsync($"api/v1/fichas/{cenario.FichaId}/registos?tipo=Ocorrencia");

        Assert.Equal(corpo.GetRawText(), Assert.Single((await ClienteApi.JsonAsync(doPrincipal)).EnumerateArray()).GetRawText());
        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal(0, (await ClienteApi.JsonAsync(ocorrencias)).GetArrayLength());
    }

    /// <summary>
    /// RF-090 CA3, RF-091 CA3 e RNF-005: o Familiar não regista (403) e quem não está associado recebe 404; nada é gravado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoUmCuidadorDaFichaRegista()
    {
        using var cenario = await this.CenarioAsync();
        var ocorrencia = new { descricao = "Queda.", ocorridaEm = "2026-10-07T15:05:00+01:00" };
        var nota = new { descricao = "Nota." };

        using var ocorrenciaDoFamiliar = await cenario.Familiar.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/ocorrencias", ocorrencia);
        using var notaDoFamiliar = await cenario.Familiar.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/notas", nota);
        using var ocorrenciaDoEstranho = await cenario.Estranho.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/ocorrencias", ocorrencia);
        using var notaDoEstranho = await cenario.Estranho.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/notas", nota);

        Assert.Equal(HttpStatusCode.Forbidden, ocorrenciaDoFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(ocorrenciaDoFamiliar));
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(notaDoFamiliar));
        Assert.Equal(HttpStatusCode.NotFound, ocorrenciaDoEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(ocorrenciaDoEstranho));
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(notaDoEstranho));
        Assert.Equal(0, await this.RegistosAsync(cenario.FichaId));
    }

    /// <summary>
    /// RF-090 CA2, RF-091 CA2 e RNF-028: os campos em falta ou inválidos, incluindo os da consulta, são indicados um a um.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DadosInvalidosSaoIndicadosPorCampo()
    {
        using var cenario = await this.CenarioAsync();
        var caminho = $"api/v1/fichas/{cenario.FichaId}";

        using var vazia = await cenario.Principal.PostAsJsonAsync($"{caminho}/ocorrencias", new { });
        using var instanteInvalido = await cenario.Principal.PostAsJsonAsync($"{caminho}/ocorrencias", new { descricao = "Queda.", ocorridaEm = "ontem" });
        using var notaSemTexto = await cenario.Principal.PostAsJsonAsync($"{caminho}/notas", new { descricao = "  " });
        using var semTipo = await cenario.Principal.GetAsync($"{caminho}/registos");
        using var tipoInvalido = await cenario.Principal.GetAsync($"{caminho}/registos?tipo=Outro");
        using var intervaloInvertido = await cenario.Principal.GetAsync($"{caminho}/registos?tipo=Nota&de=2026-11-02&ate=2026-11-01");

        Assert.Equal(HttpStatusCode.BadRequest, vazia.StatusCode);
        Assert.Equal(CamposDaOcorrencia, await CamposAsync(vazia));
        Assert.Contains("ocorridaEm", await CamposAsync(instanteInvalido));
        Assert.Equal("descricao", Assert.Single(await CamposAsync(notaSemTexto)));
        Assert.Equal("tipo", Assert.Single(await CamposAsync(semTipo)));
        Assert.Equal("tipo", Assert.Single(await CamposAsync(tipoInvalido)));
        Assert.Equal("ate", Assert.Single(await CamposAsync(intervaloInvertido)));
        Assert.Equal(0, await this.RegistosAsync(cenario.FichaId));
    }

    /// <summary>
    /// RF-120 e RP-13: numa ficha arquivada não se registam ocorrências nem notas (409), mas as já registadas continuam visíveis.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaSoPermiteConsultar()
    {
        using var cenario = await this.CenarioAsync();
        var caminho = $"api/v1/fichas/{cenario.FichaId}";
        await RegistarAsync(cenario.Principal, $"{caminho}/ocorrencias", new { descricao = "Queda.", ocorridaEm = "2026-10-07T15:05:00+01:00" });
        await RegistarAsync(cenario.Principal, $"{caminho}/notas", new { descricao = "Nota." });
        using var arquivo = await cenario.Principal.PostAsync($"{caminho}/arquivo", null);
        Assert.Equal(HttpStatusCode.NoContent, arquivo.StatusCode);

        using var ocorrencia = await cenario.Cuidador.PostAsJsonAsync($"{caminho}/ocorrencias", new { descricao = "Outra.", ocorridaEm = "2026-10-07T16:00:00+01:00" });
        using var nota = await cenario.Cuidador.PostAsJsonAsync($"{caminho}/notas", new { descricao = "Outra." });
        using var ocorrencias = await cenario.Familiar.GetAsync($"{caminho}/registos?tipo=Ocorrencia");
        using var notas = await cenario.Cuidador.GetAsync($"{caminho}/registos?tipo=Nota");

        Assert.Equal(HttpStatusCode.Conflict, ocorrencia.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(ocorrencia));
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(nota));
        Assert.Equal(1, (await ClienteApi.JsonAsync(ocorrencias)).GetArrayLength());
        Assert.Equal(1, (await ClienteApi.JsonAsync(notas)).GetArrayLength());
    }

    /// <summary>
    /// R-07, RF-090 CA4 e secção 4 da Documentação da API: alterar ou eliminar um registo responde 405 <c>REGISTO_IMUTAVEL</c>, e
    /// os gatilhos <c>TR_Ocorrencia_Imutavel</c> e <c>TR_Nota_Imutavel</c> recusam a eliminação na própria base de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistosSaoImutaveis()
    {
        using var cenario = await this.CenarioAsync();
        var caminho = $"api/v1/fichas/{cenario.FichaId}";
        var ocorrenciaId = await RegistarAsync(cenario.Principal, $"{caminho}/ocorrencias", new { descricao = "Queda.", ocorridaEm = "2026-10-07T15:05:00+01:00" });
        var notaId = await RegistarAsync(cenario.Principal, $"{caminho}/notas", new { descricao = "Nota." });

        using var alteracao = await cenario.Principal.PutAsJsonAsync($"{caminho}/registos/{ocorrenciaId}", new { descricao = "Alterada." });
        using var alteracaoParcial = await cenario.Cuidador.PatchAsJsonAsync($"{caminho}/registos/{notaId}", new { descricao = "Alterada." });
        using var eliminacao = await cenario.Principal.DeleteAsync($"{caminho}/registos/{notaId}");
        using var eliminacaoDoFamiliar = await cenario.Familiar.DeleteAsync($"{caminho}/registos/{ocorrenciaId}");

        foreach (var resposta in new[] { alteracao, alteracaoParcial, eliminacao, eliminacaoDoFamiliar })
        {
            Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
            Assert.Equal("REGISTO_IMUTAVEL", await ClienteApi.CodigoErroAsync(resposta));
        }

        await Assert.ThrowsAsync<SqlException>(() => this.fabrica.ComContextoAsync(c =>
            c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Nota WHERE Id = {notaId}")));
        await Assert.ThrowsAsync<SqlException>(() => this.fabrica.ComContextoAsync(c =>
            c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Ocorrencia WHERE Id = {ocorrenciaId}")));
        Assert.Equal(2, await this.RegistosAsync(cenario.FichaId));
        Assert.Equal("Queda.", await this.fabrica.ComContextoAsync(c => c.Ocorrencias.Where(o => o.Id == ocorrenciaId).Select(o => o.Descricao).SingleAsync()));
    }

    /// <summary>
    /// OP-48 (RF-094 CA1): da mais recente para a mais antiga pelo instante do registo; <c>de</c> e <c>ate</c> são dias de Lisboa.
    /// Às 23:30 UTC de 1 de julho já é dia 2 em Lisboa (UTC+1).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OrdenaEFiltraPeloDiaDoRegistoEmHoraDeLisboa()
    {
        using var cenario = await this.CenarioAsync();
        await this.fabrica.ComContextoAsync(async c =>
        {
            c.Ocorrencias.Add(Ocorrencia(cenario, "Janeiro.", new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
            c.Ocorrencias.Add(Ocorrencia(cenario, "Julho, dia 2.", new DateTime(2026, 7, 1, 23, 30, 0, DateTimeKind.Utc)));
            c.Ocorrencias.Add(Ocorrencia(cenario, "Julho, dia 1.", new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)));
            return await c.SaveChangesAsync();
        });
        var caminho = $"api/v1/fichas/{cenario.FichaId}/registos?tipo=Ocorrencia";

        using var todas = await cenario.Familiar.GetAsync(caminho);
        using var doDia2 = await cenario.Familiar.GetAsync($"{caminho}&de=2026-07-02&ate=2026-07-02");
        using var ateJunho = await cenario.Familiar.GetAsync($"{caminho}&ate=2026-06-30");

        var lista = (await ClienteApi.JsonAsync(todas)).EnumerateArray().ToList();
        Assert.Equal(DescricoesDoMaisRecente, lista.Select(r => r.GetProperty("descricao").GetString()));
        Assert.Equal("2026-07-02T00:30:00+01:00", lista[0].GetProperty("criadaEm").GetString());
        Assert.Equal("2026-01-15T12:00:00+00:00", lista[2].GetProperty("criadaEm").GetString());
        var dia2 = Assert.Single((await ClienteApi.JsonAsync(doDia2)).EnumerateArray());
        Assert.Equal("Julho, dia 2.", dia2.GetProperty("descricao").GetString());
        Assert.Equal("Janeiro.", Assert.Single((await ClienteApi.JsonAsync(ateJunho)).EnumerateArray()).GetProperty("descricao").GetString());
    }

    /// <summary>
    /// RF-115 CA1 e CA2 (parte na aplicação): a ocorrência do Cuidador principal cria uma notificação <c>NovaOcorrencia</c> para o
    /// outro Cuidador e para o Familiar, e nenhuma para o autor; a nota não cria notificações e não há e-mails registados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OcorrenciaNotificaOsOutrosEANotaNao()
    {
        using var cenario = await this.CenarioAsync();
        var caminho = $"api/v1/fichas/{cenario.FichaId}";

        var ocorrenciaId = await RegistarAsync(cenario.Principal, $"{caminho}/ocorrencias", new { descricao = "Queda.", ocorridaEm = "2026-10-07T15:05:00+01:00" });
        await RegistarAsync(cenario.Cuidador, $"{caminho}/notas", new { descricao = "Nota." });

        var notificacoes = await this.fabrica.ComContextoAsync(c => c.Notificacoes.AsNoTracking().Where(n => n.FichaId == cenario.FichaId).ToListAsync());
        var criadaEm = await this.fabrica.ComContextoAsync(c => c.Ocorrencias.Where(o => o.Id == ocorrenciaId).Select(o => o.RegistadaEmUtc).SingleAsync());
        Assert.Equal(2, notificacoes.Count);
        Assert.Equal(new[] { cenario.CuidadorId, cenario.FamiliarId }.Order(), notificacoes.Select(n => n.DestinatarioId).Order());
        Assert.All(notificacoes, n =>
        {
            Assert.Equal(TipoNotificacao.NovaOcorrencia, n.Tipo);
            Assert.Equal(TipoReferencia.Ocorrencia, n.ReferenciaTipo);
            Assert.Equal(ocorrenciaId, n.ReferenciaId);
            Assert.Equal(criadaEm, n.InstanteLimiteUtc);
            Assert.Contains("Ana Principal registou uma ocorrência", n.Mensagem, StringComparison.Ordinal);
            Assert.Contains("07/10/2026 15:05", n.Mensagem, StringComparison.Ordinal);
        });
        var ids = notificacoes.Select(n => n.Id).ToList();
        Assert.False(await this.fabrica.ComContextoAsync(c => c.EnviosEmail.AnyAsync(e => e.NotificacaoId != null && ids.Contains(e.NotificacaoId.Value))));
    }

    /// <summary>
    /// RNF-046: se a base de dados recusar a gravação da ocorrência, a resposta é 500 e não fica a ocorrência nem nenhuma
    /// notificação.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNaGravacaoNaoDeixaOcorrenciaNemNotificacoes()
    {
        using var cenario = await this.CenarioAsync();
        await using var fabricaComFalha = this.fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddScoped<IOcorrenciaRepositorio, OcorrenciaRepositorioQueFalha>()));
        using var cliente = fabricaComFalha.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = cenario.Principal.DefaultRequestHeaders.Authorization;

        using var resposta = await cliente.PostAsJsonAsync(
            $"api/v1/fichas/{cenario.FichaId}/ocorrencias",
            new { descricao = "Queda.", ocorridaEm = "2026-10-07T15:05:00+01:00" });

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("ERRO_INTERNO", await ClienteApi.CodigoErroAsync(resposta));
        Assert.Equal(0, await this.RegistosAsync(cenario.FichaId));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Notificacoes.AnyAsync(n => n.FichaId == cenario.FichaId)));
    }

    private static Ocorrencia Ocorrencia(Cenario cenario, string descricao, DateTime registadaEmUtc)
    {
        return new Ocorrencia(cenario.FichaId, descricao, registadaEmUtc.AddMinutes(-10), cenario.PrincipalId, registadaEmUtc);
    }

    private static async Task<Guid> RegistarAsync(HttpClient cliente, string caminho, object corpo)
    {
        using var resposta = await cliente.PostAsJsonAsync(caminho, corpo);
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private static async Task<IEnumerable<string?>> CamposAsync(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return corpo.GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
    }

    private Task<int> RegistosAsync(Guid fichaId)
    {
        return this.fabrica.ComContextoAsync(async c =>
            await c.Ocorrencias.CountAsync(o => o.FichaId == fichaId) + await c.Notas.CountAsync(n => n.FichaId == fichaId));
    }

    private async Task<Cenario> CenarioAsync()
    {
        var principal = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Ana Principal");
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Bruno Cuidador");
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Carla Familiar");
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Duarte Estranho");
        var fichaId = await ClienteFichas.CriarFichaAsync(principal.Cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, cuidador.Id, Papel.Cuidador);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiar.Id, Papel.Familiar);
        return new Cenario(principal.Cliente, cuidador.Cliente, familiar.Cliente, estranho.Cliente, principal.Id, cuidador.Id, familiar.Id, fichaId);
    }

    /// <summary>
    /// Ficha com um Cuidador principal, outro Cuidador e um Familiar, e um Utilizador sem associação, cada um com o seu cliente.
    /// </summary>
    /// <param name="Principal">Cliente do Cuidador principal.</param>
    /// <param name="Cuidador">Cliente do outro Cuidador.</param>
    /// <param name="Familiar">Cliente do Familiar.</param>
    /// <param name="Estranho">Cliente de um Utilizador não associado.</param>
    /// <param name="PrincipalId">Identificador do Cuidador principal.</param>
    /// <param name="CuidadorId">Identificador do outro Cuidador.</param>
    /// <param name="FamiliarId">Identificador do Familiar.</param>
    /// <param name="FichaId">Identificador da ficha.</param>
    private sealed record Cenario(
        HttpClient Principal,
        HttpClient Cuidador,
        HttpClient Familiar,
        HttpClient Estranho,
        Guid PrincipalId,
        Guid CuidadorId,
        Guid FamiliarId,
        Guid FichaId) : IDisposable
    {
        public void Dispose()
        {
            this.Principal.Dispose();
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
