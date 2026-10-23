// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-27 a OP-31 (F005, I037): a API completa com o SQL Server e a pasta de documentos da execução.
/// As datas são futuras, para o número de tomas geradas não depender do dia em que os testes correm.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class TratamentosApiTestes
{
    private static readonly string[] HorasDoDia = { "08:00", "20:00" };
    private static readonly string[] HoraInvalida = { "25:00" };
    private static readonly string[] HoraNova = { "09:00" };
    private static readonly string[] CamposObrigatorios = { "medicamento", "dose", "grauImportancia", "dataInicio", "horarios" };
    private static readonly string[] CamposInvalidos = { "grauImportancia", "dataFim", "horarios[0]" };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratamentosApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public TratamentosApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-27, OP-28 e OP-29: o Cuidador regista, as tomas ficam geradas e o Familiar consulta; quem não está associado
    /// recebe 404 e o filtro pelo estado é aplicado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarConsultarEListar()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;

        using var registado = await cuidador.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", Pedido());
        Assert.Equal(HttpStatusCode.Created, registado.StatusCode);
        var corpo = await ClienteApi.JsonAsync(registado);
        var id = corpo.GetProperty("id").GetGuid();
        Assert.Equal(fichaId, corpo.GetProperty("fichaId").GetGuid());
        Assert.Equal("Moderado", corpo.GetProperty("grauImportancia").GetString());
        Assert.Equal(Dia(10), corpo.GetProperty("dataInicio").GetString());
        Assert.Equal(HorasDoDia, corpo.GetProperty("horarios").EnumerateArray().Select(h => h.GetString()));
        Assert.Equal("EmCurso", corpo.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, corpo.GetProperty("receitaDocumentoId").ValueKind);
        Assert.Equal(6, await this.TomasAsync(id));

        using var lista = await familiar.GetAsync($"api/v1/fichas/{fichaId}/tratamentos");
        using var consulta = await familiar.GetAsync($"api/v1/tratamentos/{id}");
        using var terminados = await cuidador.GetAsync($"api/v1/fichas/{fichaId}/tratamentos?estado=Terminado");
        using var estadoInvalido = await cuidador.GetAsync($"api/v1/fichas/{fichaId}/tratamentos?estado=Outro");
        using var listaDoEstranho = await estranho.GetAsync($"api/v1/fichas/{fichaId}/tratamentos");
        using var consultaDoEstranho = await estranho.GetAsync($"api/v1/tratamentos/{id}");

        Assert.Equal(id, (await ClienteApi.JsonAsync(lista))[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(0, (await ClienteApi.JsonAsync(terminados)).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, estadoInvalido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, listaDoEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(listaDoEstranho));
        Assert.Equal(HttpStatusCode.NotFound, consultaDoEstranho.StatusCode);
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(consultaDoEstranho));
    }

    /// <summary>
    /// OP-27 com receita (RF-052): o ficheiro é guardado, fica associado ao tratamento e obtém-se com OP-17.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComReceita()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;
        using var pedido = ComReceita(Pedido(), ClienteFichas.Pdf);

        using var resposta = await cuidador.PostAsync($"api/v1/fichas/{fichaId}/tratamentos", pedido);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        var tratamentoId = corpo.GetProperty("id").GetGuid();
        var receitaId = corpo.GetProperty("receitaDocumentoId").GetGuid();
        var documento = await this.fabrica.ComContextoAsync(c => c.Documentos.AsNoTracking().SingleAsync(d => d.Id == receitaId));
        Assert.Equal(tratamentoId, documento.TratamentoId);
        Assert.Equal(CategoriaDocumento.ReceitaMedica, documento.Categoria);
        Assert.True(File.Exists(Path.Combine(this.fabrica.PastaDocumentos, documento.CaminhoRelativo)));
        using var ficheiro = await familiar.GetAsync($"api/v1/documentos/{receitaId}");
        Assert.Equal(HttpStatusCode.OK, ficheiro.StatusCode);
        using var consulta = await familiar.GetAsync($"api/v1/tratamentos/{tratamentoId}");
        Assert.Equal(receitaId, (await ClienteApi.JsonAsync(consulta)).GetProperty("receitaDocumentoId").GetGuid());
    }

    /// <summary>
    /// RF-050 CA3, RF-055 CA2 e RNF-005: só um Cuidador regista, altera e termina; o Familiar recebe 403 e quem não está
    /// associado recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoUmCuidadorRegistaAlteraETermina()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;
        var id = await RegistarAsync(cuidador, fichaId);

        using var registoDoFamiliar = await familiar.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", Pedido());
        using var registoDoEstranho = await estranho.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", Pedido());
        using var alteracaoDoFamiliar = await familiar.PutAsJsonAsync($"api/v1/tratamentos/{id}", new { dose = "1 g" });
        using var terminoDoFamiliar = await familiar.PostAsync($"api/v1/tratamentos/{id}/termino", null);
        using var terminoDoEstranho = await estranho.PostAsync($"api/v1/tratamentos/{id}/termino", null);

        Assert.Equal(HttpStatusCode.Forbidden, registoDoFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(registoDoFamiliar));
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(registoDoEstranho));
        Assert.Equal(HttpStatusCode.Forbidden, alteracaoDoFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, terminoDoFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, terminoDoEstranho.StatusCode);
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(terminoDoEstranho));
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Tratamentos.CountAsync(t => t.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-050 CA2, CA4 e RF-051: os campos em falta ou inválidos são indicados um a um (RNF-028).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DadosInvalidosSaoIndicadosPorCampo()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;

        using var vazio = await cuidador.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", new { });
        using var invalido = await cuidador.PostAsJsonAsync(
            $"api/v1/fichas/{fichaId}/tratamentos",
            Pedido(grau: "Urgente", fim: Dia(9), horarios: HoraInvalida));

        Assert.Equal(HttpStatusCode.BadRequest, vazio.StatusCode);
        Assert.Equal(CamposObrigatorios, await CamposAsync(vazio));
        Assert.Equal(CamposInvalidos, await CamposAsync(invalido));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Tratamentos.AnyAsync(t => t.FichaId == fichaId)));
    }

    /// <summary>
    /// OP-30 e OP-31 (RF-055, RF-056, RF-060): alterar regenera as tomas futuras com os dados novos; terminar apaga-as e um
    /// tratamento terminado já não muda.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarETerminar()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;
        var id = await RegistarAsync(cuidador, fichaId);

        using var alterado = await cuidador.PutAsJsonAsync($"api/v1/tratamentos/{id}", new { dose = "850 mg", horarios = HoraNova });
        Assert.Equal(HttpStatusCode.OK, alterado.StatusCode);
        var corpo = await ClienteApi.JsonAsync(alterado);
        Assert.Equal("Metformina", corpo.GetProperty("medicamento").GetString());
        Assert.Equal("850 mg", corpo.GetProperty("dose").GetString());
        Assert.Equal("09:00", corpo.GetProperty("horarios")[0].GetString());
        var doses = await this.fabrica.ComContextoAsync(c => c.Tomas.Where(t => t.TratamentoId == id).Select(t => t.Dose).ToListAsync());
        Assert.Equal(3, doses.Count);
        Assert.All(doses, d => Assert.Equal("850 mg", d));

        using var terminado = await cuidador.PostAsync($"api/v1/tratamentos/{id}/termino", null);
        using var outraVez = await cuidador.PostAsync($"api/v1/tratamentos/{id}/termino", null);
        using var alteracaoDepois = await cuidador.PutAsJsonAsync($"api/v1/tratamentos/{id}", new { dose = "1 g" });

        Assert.Equal(HttpStatusCode.OK, terminado.StatusCode);
        Assert.Equal("Terminado", (await ClienteApi.JsonAsync(terminado)).GetProperty("estado").GetString());
        Assert.Equal(0, await this.TomasAsync(id));
        Assert.Equal(HttpStatusCode.Conflict, outraVez.StatusCode);
        Assert.Equal("TRATAMENTO_JA_TERMINADO", await ClienteApi.CodigoErroAsync(outraVez));
        Assert.Equal(HttpStatusCode.Conflict, alteracaoDepois.StatusCode);
        Assert.Equal("TRATAMENTO_TERMINADO", await ClienteApi.CodigoErroAsync(alteracaoDepois));
    }

    /// <summary>
    /// RF-120 e RF-122: numa ficha arquivada não se regista nem se altera medicação (409), mas o tratamento pode ser terminado;
    /// ao arquivar, as tomas futuras são apagadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaSoPermiteTerminar()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;
        var id = await RegistarAsync(cuidador, fichaId);
        using var arquivo = await cuidador.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        Assert.Equal(HttpStatusCode.NoContent, arquivo.StatusCode);

        using var registo = await cuidador.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", Pedido());
        using var alteracao = await cuidador.PutAsJsonAsync($"api/v1/tratamentos/{id}", new { dose = "1 g" });
        using var termino = await cuidador.PostAsync($"api/v1/tratamentos/{id}/termino", null);

        Assert.Equal(0, await this.TomasAsync(id));
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(registo));
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(alteracao));
        Assert.Equal(HttpStatusCode.OK, termino.StatusCode);
    }

    /// <summary>
    /// RF-053 CA1: se a receita não puder ser guardada, a resposta é 503 e o tratamento não é registado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ReceitaQueNaoEGuardadaNaoRegistaOTratamento()
    {
        using var cenario = await this.CenarioAsync();
        var (cuidador, familiar, estranho, fichaId) = cenario;
        await using var fabricaComFalha = this.fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IArmazenamentoDocumentos, ArmazenamentoQueFalha>()));
        using var cliente = fabricaComFalha.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = cuidador.DefaultRequestHeaders.Authorization;
        using var pedido = ComReceita(Pedido(), ClienteFichas.Pdf);

        using var resposta = await cliente.PostAsync($"api/v1/fichas/{fichaId}/tratamentos", pedido);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("DOCUMENTO_NAO_GUARDADO", await ClienteApi.CodigoErroAsync(resposta));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Tratamentos.AnyAsync(t => t.FichaId == fichaId)));
    }

    private static string Dia(int diasDepoisDeHoje)
    {
        return DateOnly.FromDateTime(DateTime.UtcNow).AddDays(diasDepoisDeHoje).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static object Pedido(string grau = "Moderado", string? fim = null, string[]? horarios = null)
    {
        return new
        {
            medicamento = "Metformina",
            dose = "500 mg",
            grauImportancia = grau,
            dataInicio = Dia(10),
            dataFim = fim ?? Dia(12),
            horarios = horarios ?? HorasDoDia,
        };
    }

    private static MultipartFormDataContent ComReceita(object dados, byte[] receita)
    {
        var ficheiro = new ByteArrayContent(receita);
        ficheiro.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(dados), Encoding.UTF8, "application/json"), "dados" },
            { ficheiro, "receita", "receita.pdf" },
        };
    }

    private static async Task<Guid> RegistarAsync(HttpClient cliente, Guid fichaId)
    {
        using var resposta = await cliente.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", Pedido());
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private static async Task<IEnumerable<string?>> CamposAsync(HttpResponseMessage resposta)
    {
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return corpo.GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
    }

    private Task<int> TomasAsync(Guid tratamentoId)
    {
        return this.fabrica.ComContextoAsync(c => c.Tomas.CountAsync(t => t.TratamentoId == tratamentoId));
    }

    private async Task<Cenario> CenarioAsync()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var fichaId = await ClienteFichas.CriarFichaAsync(cuidador.Cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiar.Id, Papel.Familiar);
        return new Cenario(cuidador.Cliente, familiar.Cliente, estranho.Cliente, fichaId);
    }

    /// <summary>
    /// Ficha de um Cuidador, com um Familiar associado e um Utilizador sem associação, cada um com o seu cliente autenticado.
    /// </summary>
    /// <param name="Cuidador">Cliente do Cuidador principal.</param>
    /// <param name="Familiar">Cliente do Familiar.</param>
    /// <param name="Estranho">Cliente de um Utilizador não associado.</param>
    /// <param name="FichaId">Identificador da ficha.</param>
    private sealed record Cenario(HttpClient Cuidador, HttpClient Familiar, HttpClient Estranho, Guid FichaId) : IDisposable
    {
        public void Dispose()
        {
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
