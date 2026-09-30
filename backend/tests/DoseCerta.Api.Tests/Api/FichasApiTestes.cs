// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DoseCerta.Api.Dominio;
using DoseCerta.Api.Tests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Api.Tests.Api;

/// <summary>
/// Testes de integração do endpoint <c>/api/fichas</c> para a ficha própria (I003 / F003).
/// </summary>
public sealed class FichasApiTestes : IDisposable
{
    private readonly FabricaApi fabrica = new();

    /// <summary>
    /// <c>POST /api/fichas</c> com <c>paraOUtilizadorAutenticado=true</c> cria a ficha própria (201).
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaPropriaDevolve201ComDonoComoCuidadorPrincipal()
    {
        using var cliente = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);
        using var formulario = FormularioValido(comDocumento: true);

        using var resposta = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), formulario);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        using var json = await LerJsonAsync(resposta);
        var raiz = json.RootElement;
        Assert.True(raiz.GetProperty("ehFichaPropria").GetBoolean());
        Assert.Equal("Ativa", raiz.GetProperty("estado").GetString());
        var associacao = Assert.Single(raiz.GetProperty("associacoes").EnumerateArray());
        Assert.Equal(BaseDadosTeste.Ana, associacao.GetProperty("utilizadorId").GetGuid());
        Assert.Equal("Cuidador", associacao.GetProperty("papel").GetString());
        Assert.True(associacao.GetProperty("ehCuidadorPrincipal").GetBoolean());
        var documento = Assert.Single(raiz.GetProperty("documentos").EnumerateArray());
        Assert.Equal("ReceitaMedica", documento.GetProperty("categoria").GetString());

        using var propria = await cliente.GetAsync(new Uri("/api/fichas/propria", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, propria.StatusCode);
    }

    /// <summary>
    /// Uma segunda ficha própria devolve 409 com um código de erro explícito, não um 500.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task SegundaFichaPropriaDevolve409()
    {
        using var cliente = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);
        using (var primeiro = FormularioValido())
        using (var criada = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), primeiro))
        {
            Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        }

        using var segundo = FormularioValido();
        using var resposta = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), segundo);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        using var json = await LerJsonAsync(resposta);
        Assert.Equal(nameof(CodigoErro.FichaPropriaJaExiste), json.RootElement.GetProperty("codigo").GetString());
    }

    /// <summary>
    /// Campos obrigatórios em falta devolvem 400 com os erros por campo.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task CamposObrigatoriosEmFaltaDevolvem400()
    {
        using var cliente = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);
        using var formulario = new MultipartFormDataContent
        {
            { new StringContent("true"), "paraOUtilizadorAutenticado" },
        };

        using var resposta = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var json = await LerJsonAsync(resposta);
        var erros = json.RootElement.GetProperty("errors");
        Assert.True(erros.TryGetProperty("NomeCompleto", out _));
        Assert.True(erros.TryGetProperty("DataNascimento", out _));
        Assert.True(erros.TryGetProperty("ContactoEmergenciaTelefone", out _));
        Assert.True(erros.TryGetProperty("GrauAutonomiaId", out _));
    }

    /// <summary>
    /// Se a gravação do documento falhar, a API devolve o erro e não fica nenhuma ficha criada.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNaGravacaoDoDocumentoNaoDeixaDadosParciais()
    {
        this.fabrica.Armazenamento.FalharAposGravacoes = 0;
        using var cliente = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);
        using var formulario = FormularioValido(comDocumento: true);

        using var resposta = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), formulario);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        using var json = await LerJsonAsync(resposta);
        Assert.Equal(nameof(CodigoErro.FalhaGravacaoDocumento), json.RootElement.GetProperty("codigo").GetString());
        using var propria = await cliente.GetAsync(new Uri("/api/fichas/propria", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, propria.StatusCode);
    }

    /// <summary>
    /// Um pedido direto à API para remover ou despromover o dono da ficha própria é bloqueado (409).
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task PedidoDiretoParaRemoverOuDespromoverODonoEhBloqueado()
    {
        var fichaId = await this.CriarFichaPropriaDaAnaComCuidadorBrunoAsync();
        using var bruno = this.fabrica.CriarClienteComo(BaseDadosTeste.Bruno);

        using var remover = await bruno.DeleteAsync(new Uri($"/api/fichas/{fichaId}/associacoes/{BaseDadosTeste.Ana}", UriKind.Relative));
        using var despromover = await bruno.PutAsJsonAsync(
            new Uri($"/api/fichas/{fichaId}/associacoes/{BaseDadosTeste.Ana}/papel", UriKind.Relative),
            new { papel = "Familiar" });

        foreach (var resposta in new[] { remover, despromover })
        {
            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            using var json = await LerJsonAsync(resposta);
            Assert.Equal(nameof(CodigoErro.CuidadorPrincipalFichaPropriaImutavel), json.RootElement.GetProperty("codigo").GetString());
        }
    }

    /// <summary>
    /// Sem autenticação, o endpoint devolve 401.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task PedidoSemAutenticacaoDevolve401()
    {
        using var cliente = this.fabrica.CreateClient();
        using var formulario = FormularioValido();

        using var resposta = await cliente.PostAsync(new Uri("/api/fichas", UriKind.Relative), formulario);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    /// <summary>
    /// Os catálogos das categorias fechadas estão disponíveis para os frontends.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task CatalogosDevolvemAsCategoriasFechadas()
    {
        using var cliente = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);

        using var resposta = await cliente.GetAsync(new Uri("/api/catalogos", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = await LerJsonAsync(resposta);
        Assert.Equal(4, json.RootElement.GetProperty("grausAutonomia").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("alergias").GetArrayLength() > 1);
        Assert.True(json.RootElement.GetProperty("condicoesSaude").GetArrayLength() > 1);
    }

    /// <inheritdoc/>
    public void Dispose() => this.fabrica.Dispose();

    private static MultipartFormDataContent FormularioValido(bool comDocumento = false)
    {
        var formulario = new MultipartFormDataContent
        {
            { new StringContent("true"), "paraOUtilizadorAutenticado" },
            { new StringContent("Ana Maria Teste"), "nomeCompleto" },
            { new StringContent("1980-05-17"), "dataNascimento" },
            { new StringContent("Rui Teste"), "contactoEmergenciaNome" },
            { new StringContent("+351 912 345 678"), "contactoEmergenciaTelefone" },
            { new StringContent(Pedidos.Autonomo.ToString(System.Globalization.CultureInfo.InvariantCulture)), "grauAutonomiaId" },
            { new StringContent(Pedidos.Hipertensao.ToString(System.Globalization.CultureInfo.InvariantCulture)), "condicoesSaudeIds" },
            { new StringContent(Pedidos.SemAlergias.ToString(System.Globalization.CultureInfo.InvariantCulture)), "alergiasIds" },
        };

        if (comDocumento)
        {
            var ficheiro = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 documento ficticio"));
            ficheiro.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            formulario.Add(new StringContent(nameof(CategoriaDocumento.ReceitaMedica)), "documentos[0].categoria");
            formulario.Add(ficheiro, "documentos[0].ficheiro", "receita.pdf");
        }

        return formulario;
    }

    private static async Task<JsonDocument> LerJsonAsync(HttpResponseMessage resposta) =>
        await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());

    private async Task<Guid> CriarFichaPropriaDaAnaComCuidadorBrunoAsync()
    {
        using var ana = this.fabrica.CriarClienteComo(BaseDadosTeste.Ana);
        using var formulario = FormularioValido();
        using var resposta = await ana.PostAsync(new Uri("/api/fichas", UriKind.Relative), formulario);
        using var json = await LerJsonAsync(resposta);
        var fichaId = json.RootElement.GetProperty("id").GetGuid();

        // Simula a associação que a F004 cria quando o convite é aceite.
        using var contexto = this.fabrica.BaseDados.CriarContexto();
        var ficha = await contexto.Fichas.Include(f => f.Associacoes).SingleAsync(f => f.Id == fichaId);
        ficha.AdicionarAssociacao(BaseDadosTeste.Bruno, PapelFicha.Cuidador, DateTime.UtcNow);
        await contexto.SaveChangesAsync();
        return fichaId;
    }
}
