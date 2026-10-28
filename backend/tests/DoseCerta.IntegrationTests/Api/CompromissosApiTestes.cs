// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-41 a OP-44 e da recusa da confirmação de compromissos (F008, I044): a API completa com o SQL
/// Server. As fichas e as associações são criadas diretamente na base de dados de testes.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class CompromissosApiTestes
{
    private static readonly string[] CamposObrigatorios = { "dataHora", "local", "motivo" };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissosApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public CompromissosApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-41 a OP-44: o Cuidador regista, o Familiar lista, o Cuidador edita e remove; o compromisso removido deixa de aparecer.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarListarEditarERemover()
    {
        using var cenario = await this.CenarioAsync();

        using var registado = await cenario.Cuidador.PostAsJsonAsync(
            $"api/v1/fichas/{cenario.FichaId}/compromissos",
            new { dataHora = "2027-03-12T10:30:00+00:00", local = "Centro de Saúde Fictício", motivo = "Consulta de rotina" });
        Assert.Equal(HttpStatusCode.Created, registado.StatusCode);
        var corpo = await ClienteApi.JsonAsync(registado);
        var id = corpo.GetProperty("id").GetGuid();
        Assert.Equal(cenario.FichaId, corpo.GetProperty("fichaId").GetGuid());
        Assert.Equal("2027-03-12T10:30:00+00:00", corpo.GetProperty("dataHora").GetString());
        Assert.Equal("Centro de Saúde Fictício", corpo.GetProperty("local").GetString());
        Assert.Equal("Consulta de rotina", corpo.GetProperty("motivo").GetString());
        Assert.Equal(1440, corpo.GetProperty("antecedenciaAvisoMinutos").GetInt32());

        using var lista = await cenario.Familiar.GetAsync($"api/v1/fichas/{cenario.FichaId}/compromissos?de=2027-03-01&ate=2027-03-31");
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(id, Assert.Single((await ClienteApi.JsonAsync(lista)).EnumerateArray()).GetProperty("id").GetGuid());

        using var editado = await cenario.Cuidador.PutAsJsonAsync(
            $"api/v1/compromissos/{id}",
            new { dataHora = "2027-04-12T11:00:00+01:00", local = "Hospital Fictício", motivo = "Análises", antecedenciaAvisoMinutos = 120 });
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        var corpoEditado = await ClienteApi.JsonAsync(editado);
        Assert.Equal("2027-04-12T11:00:00+01:00", corpoEditado.GetProperty("dataHora").GetString());
        Assert.Equal(120, corpoEditado.GetProperty("antecedenciaAvisoMinutos").GetInt32());
        var gravado = await this.fabrica.ComContextoAsync(c => c.Compromissos.AsNoTracking().SingleAsync(x => x.Id == id));
        Assert.Equal(new DateTime(2027, 4, 12, 10, 0, 0, DateTimeKind.Utc), gravado.DataHoraUtc);

        using var removido = await cenario.Cuidador.DeleteAsync($"api/v1/compromissos/{id}");
        using var depois = await cenario.Cuidador.GetAsync($"api/v1/fichas/{cenario.FichaId}/compromissos");
        using var agenda = await cenario.Cuidador.GetAsync("api/v1/agenda?vista=mes&data=2027-04-12");

        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
        Assert.Equal(0, (await ClienteApi.JsonAsync(depois)).GetArrayLength());
        Assert.Equal(0, (await ClienteApi.JsonAsync(agenda)).GetArrayLength());
    }

    /// <summary>
    /// RF-080, RF-116 e RNF-028: campos em falta, antecedência fora dos valores permitidos e data inválida dão 400 com o campo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ValidacaoIndicaOCampo()
    {
        using var cenario = await this.CenarioAsync();
        var url = $"api/v1/fichas/{cenario.FichaId}/compromissos";

        using var vazio = await cenario.Cuidador.PostAsJsonAsync(url, new { });
        using var antecedencia = await cenario.Cuidador.PostAsJsonAsync(
            url,
            new { dataHora = "2027-03-12T10:30:00+00:00", local = "Centro", motivo = "Consulta", antecedenciaAvisoMinutos = 45 });
        using var conteudo = new StringContent("{\"dataHora\":\"amanhã\",\"local\":\"Centro\",\"motivo\":\"Consulta\"}", Encoding.UTF8, "application/json");
        using var dataInvalida = await cenario.Cuidador.PostAsync(url, conteudo);
        using var intervalo = await cenario.Cuidador.GetAsync($"{url}?de=2027-03-10&ate=2027-03-01");

        Assert.Equal(HttpStatusCode.BadRequest, vazio.StatusCode);
        Assert.Equal(CamposObrigatorios, await CamposAsync(vazio));
        Assert.Equal(HttpStatusCode.BadRequest, antecedencia.StatusCode);
        Assert.Contains("antecedenciaAvisoMinutos", await CamposAsync(antecedencia));
        Assert.Equal(HttpStatusCode.BadRequest, dataInvalida.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, intervalo.StatusCode);
        Assert.Contains("ate", await CamposAsync(intervalo));
    }

    /// <summary>
    /// RP-11 e RNF-005: o Familiar recebe 403 nas escritas, quem não está associado 404 e sem token 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PermissoesPorPapel()
    {
        using var cenario = await this.CenarioAsync();
        var compromisso = await DadosAgenda.CriarCompromissoAsync(this.fabrica, cenario.FichaId, cenario.CuidadorId, new DateTime(2027, 3, 12, 10, 30, 0, DateTimeKind.Utc));
        var pedido = new { dataHora = "2027-03-12T10:30:00+00:00", local = "Centro", motivo = "Consulta" };
        using var anonimo = this.fabrica.CreateClient();

        using var registarFamiliar = await cenario.Familiar.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/compromissos", pedido);
        using var editarFamiliar = await cenario.Familiar.PutAsJsonAsync($"api/v1/compromissos/{compromisso.Id}", pedido);
        using var removerFamiliar = await cenario.Familiar.DeleteAsync($"api/v1/compromissos/{compromisso.Id}");
        using var registarEstranho = await cenario.Estranho.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/compromissos", pedido);
        using var listarEstranho = await cenario.Estranho.GetAsync($"api/v1/fichas/{cenario.FichaId}/compromissos");
        using var editarEstranho = await cenario.Estranho.PutAsJsonAsync($"api/v1/compromissos/{compromisso.Id}", pedido);
        using var removerInexistente = await cenario.Cuidador.DeleteAsync($"api/v1/compromissos/{Guid.NewGuid()}");
        using var semToken = await anonimo.GetAsync($"api/v1/fichas/{cenario.FichaId}/compromissos");

        Assert.Equal(HttpStatusCode.Forbidden, registarFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(registarFamiliar));
        Assert.Equal(HttpStatusCode.Forbidden, editarFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, removerFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, registarEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(registarEstranho));
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(listarEstranho));
        Assert.Equal(HttpStatusCode.NotFound, editarEstranho.StatusCode);
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(editarEstranho));
        Assert.Equal("RECURSO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(removerInexistente));
        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Compromissos.CountAsync(x => x.FichaId == cenario.FichaId)));
    }

    /// <summary>
    /// RP-13 e RF-120: numa ficha arquivada as escritas dão 409 <c>FICHA_ARQUIVADA</c> e a consulta continua disponível.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaSoParaConsulta()
    {
        using var cenario = await this.CenarioAsync();
        var compromisso = await DadosAgenda.CriarCompromissoAsync(this.fabrica, cenario.FichaId, cenario.CuidadorId, new DateTime(2027, 3, 12, 10, 30, 0, DateTimeKind.Utc));
        await DadosAgenda.ArquivarAsync(this.fabrica, cenario.FichaId);
        var pedido = new { dataHora = "2027-03-12T10:30:00+00:00", local = "Centro", motivo = "Consulta" };

        using var registar = await cenario.Cuidador.PostAsJsonAsync($"api/v1/fichas/{cenario.FichaId}/compromissos", pedido);
        using var editar = await cenario.Cuidador.PutAsJsonAsync($"api/v1/compromissos/{compromisso.Id}", pedido);
        using var remover = await cenario.Cuidador.DeleteAsync($"api/v1/compromissos/{compromisso.Id}");
        using var listar = await cenario.Familiar.GetAsync($"api/v1/fichas/{cenario.FichaId}/compromissos");

        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(registar));
        Assert.Equal(HttpStatusCode.Conflict, editar.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(editar));
        Assert.Equal(HttpStatusCode.Conflict, remover.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listar.StatusCode);
        Assert.Equal(1, (await ClienteApi.JsonAsync(listar)).GetArrayLength());
    }

    /// <summary>
    /// RF-087: o pedido de confirmação de um compromisso responde sempre 405 <c>COMPROMISSO_NAO_CONFIRMAVEL</c>, para qualquer papel.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConfirmarCompromissoDa405()
    {
        using var cenario = await this.CenarioAsync();
        var compromisso = await DadosAgenda.CriarCompromissoAsync(this.fabrica, cenario.FichaId, cenario.CuidadorId, new DateTime(2027, 3, 12, 10, 30, 0, DateTimeKind.Utc));
        using var anonimo = this.fabrica.CreateClient();

        using var doCuidador = await cenario.Cuidador.PostAsync($"api/v1/compromissos/{compromisso.Id}/confirmacao", null);
        using var doFamiliar = await cenario.Familiar.PostAsync($"api/v1/compromissos/{compromisso.Id}/confirmacao", null);
        using var inexistente = await cenario.Estranho.PostAsync($"api/v1/compromissos/{Guid.NewGuid()}/confirmacao", null);
        using var semToken = await anonimo.PostAsync($"api/v1/compromissos/{compromisso.Id}/confirmacao", null);

        foreach (var resposta in new[] { doCuidador, doFamiliar, inexistente })
        {
            Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
            Assert.Equal("COMPROMISSO_NAO_CONFIRMAVEL", await ClienteApi.CodigoErroAsync(resposta));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
    }

    private static async Task<IEnumerable<string?>> CamposAsync(HttpResponseMessage resposta)
    {
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return corpo.GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
    }

    private async Task<Cenario> CenarioAsync()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var fichaId = await DadosAgenda.CriarFichaAsync(this.fabrica, "Maria Fictícia Santos", cuidador.Id, (familiar.Id, Papel.Familiar));
        return new Cenario(cuidador.Cliente, cuidador.Id, familiar.Cliente, estranho.Cliente, fichaId);
    }

    /// <summary>
    /// Ficha de um Cuidador, com um Familiar associado e um Utilizador sem associação, cada um com o seu cliente autenticado.
    /// </summary>
    /// <param name="Cuidador">Cliente do Cuidador principal.</param>
    /// <param name="CuidadorId">Identificador do Cuidador principal.</param>
    /// <param name="Familiar">Cliente do Familiar.</param>
    /// <param name="Estranho">Cliente de um Utilizador não associado.</param>
    /// <param name="FichaId">Identificador da ficha.</param>
    private sealed record Cenario(HttpClient Cuidador, Guid CuidadorId, HttpClient Familiar, HttpClient Estranho, Guid FichaId) : IDisposable
    {
        public void Dispose()
        {
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
