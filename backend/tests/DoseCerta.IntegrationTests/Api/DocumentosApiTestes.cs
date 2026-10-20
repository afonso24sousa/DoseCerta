// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração dos documentos de F002 (OP-09 e OP-11 com documentos, OP-15 a OP-17), com a API completa,
/// o SQL Server e a pasta de documentos da execução.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class DocumentosApiTestes
{
    private static readonly string[] CategoriasCriadas = { "DocumentoIdentificacao", "ReceitaMedica" };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentosApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public DocumentosApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-09 em multipart: a ficha é criada com os documentos, guardados na pasta do servidor (RF-022, RNF-006).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaComDocumentos()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        using var pedido = ClienteFichas.Multipart(
            ClienteFichas.PedidoTerceiro(),
            (ClienteFichas.Pdf, "receita.pdf", "ReceitaMedica"),
            (ClienteFichas.Png, "cc.png", "DocumentoIdentificacao"));

        using var resposta = await cliente.PostAsync("api/v1/fichas", pedido);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var fichaId = (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
        var lista = await ClienteApi.JsonAsync(await cliente.GetAsync($"api/v1/fichas/{fichaId}/documentos"));
        Assert.Equal(2, lista.GetArrayLength());
        var categorias = lista.EnumerateArray().Select(d => d.GetProperty("categoria").GetString()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(CategoriasCriadas, categorias);
        var caminhos = await this.fabrica.ComContextoAsync(c => c.Documentos.Where(d => d.FichaId == fichaId).Select(d => d.CaminhoRelativo).ToListAsync());
        Assert.All(caminhos, c => Assert.True(File.Exists(Path.Combine(this.fabrica.PastaDocumentos, c))));
    }

    /// <summary>
    /// OP-09 em multipart: uma parte dados em falta ou inválida dá 400 com o campo dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MultipartSemDadosValidosERecusado()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        using var semDados = new MultipartFormDataContent { { new StringContent("x"), "outro" } };
        using var jsonInvalido = new MultipartFormDataContent { { new StringContent("{ nao json"), "dados" } };

        using var resposta1 = await cliente.PostAsync("api/v1/fichas", semDados);
        using var resposta2 = await cliente.PostAsync("api/v1/fichas", jsonInvalido);

        foreach (var resposta in new[] { resposta1, resposta2 })
        {
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var corpo = await ClienteApi.JsonAsync(resposta);
            Assert.Equal("dados", corpo.GetProperty("campos")[0].GetProperty("campo").GetString());
        }
    }

    /// <summary>
    /// OP-15, OP-16 e OP-17: o Cuidador carrega; o Familiar lista e descarrega; quem não está associado recebe 404 (RF-028, RNF-007).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CarregarListarEDescarregar()
    {
        var contaCuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCuidador.Cliente;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var contaEstranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var estranho = contaEstranho.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, contaFamiliar.Id, Papel.Familiar);

        using var pedido = ClienteFichas.Carregamento(ClienteFichas.Pdf);
        using var carregado = await cliente.PostAsync($"api/v1/fichas/{fichaId}/documentos", pedido);
        Assert.Equal(HttpStatusCode.Created, carregado.StatusCode);
        var documento = await ClienteApi.JsonAsync(carregado);
        var documentoId = documento.GetProperty("id").GetGuid();

        using var lista = await familiar.GetAsync($"api/v1/fichas/{fichaId}/documentos");
        using var ficheiro = await familiar.GetAsync($"api/v1/documentos/{documentoId}");
        using var doEstranho = await estranho.GetAsync($"api/v1/documentos/{documentoId}");
        using var listaDoEstranho = await estranho.GetAsync($"api/v1/fichas/{fichaId}/documentos");

        Assert.Equal("RelatorioClinico", documento.GetProperty("categoria").GetString());
        Assert.Equal("relatorio.pdf", documento.GetProperty("nomeOriginal").GetString());
        Assert.Equal("PDF", documento.GetProperty("formato").GetString());
        Assert.Equal(ClienteFichas.Pdf.Length, documento.GetProperty("tamanhoBytes").GetInt32());
        Assert.Matches(@"[+-]\d{2}:\d{2}$", documento.GetProperty("carregadoEm").GetString());
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(1, (await ClienteApi.JsonAsync(lista)).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, ficheiro.StatusCode);
        Assert.Equal("application/pdf", ficheiro.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ClienteFichas.Pdf, await ficheiro.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("DOCUMENTO_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(listaDoEstranho));
    }

    /// <summary>
    /// OP-15: categoria inválida (400), formato não suportado (415), mais de 10 MB (413), Familiar (403) e ficha arquivada (409).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CarregamentoInvalidoERecusado()
    {
        var contaCuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCuidador.Cliente;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, contaFamiliar.Id, Papel.Familiar);
        var grande = new byte[ParametrosDeNegocio.TamanhoMaximoDocumento + 1];
        ClienteFichas.Pdf.CopyTo(grande, 0);
        var caminho = $"api/v1/fichas/{fichaId}/documentos";

        using var p1 = ClienteFichas.Carregamento(ClienteFichas.Pdf, categoria: "Receita");
        using var categoria = await cliente.PostAsync(caminho, p1);
        using var p2 = ClienteFichas.Carregamento("texto"u8.ToArray(), "falso.pdf");
        using var formato = await cliente.PostAsync(caminho, p2);
        using var p3 = ClienteFichas.Carregamento(grande);
        using var tamanho = await cliente.PostAsync(caminho, p3);
        using var p4 = ClienteFichas.Carregamento(ClienteFichas.Pdf);
        using var doFamiliar = await familiar.PostAsync(caminho, p4);
        (await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null)).Dispose();
        using var p5 = ClienteFichas.Carregamento(ClienteFichas.Pdf);
        using var arquivada = await cliente.PostAsync(caminho, p5);

        Assert.Equal(HttpStatusCode.BadRequest, categoria.StatusCode);
        Assert.Equal("categoria", (await ClienteApi.JsonAsync(categoria)).GetProperty("campos")[0].GetProperty("campo").GetString());
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, formato.StatusCode);
        Assert.Equal("FORMATO_NAO_SUPORTADO", await ClienteApi.CodigoErroAsync(formato));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, tamanho.StatusCode);
        Assert.Equal("FICHEIRO_DEMASIADO_GRANDE", await ClienteApi.CodigoErroAsync(tamanho));
        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Documentos.AnyAsync(d => d.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-023: se o ficheiro não puder ser guardado, a ficha não é criada, nada é gravado e a resposta é 503.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaAoGuardarNaoCriaAFicha()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var autenticado = conta.Cliente;
        await using var fabricaComFalha = this.fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IArmazenamentoDocumentos, ArmazenamentoQueFalha>()));
        using var cliente = fabricaComFalha.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = autenticado.DefaultRequestHeaders.Authorization;
        var nome = $"Sem Documento {Guid.NewGuid():N}";
        using var pedido = ClienteFichas.Multipart(ClienteFichas.PedidoTerceiro(nome), (ClienteFichas.Pdf, "receita.pdf", "ReceitaMedica"));

        using var resposta = await cliente.PostAsync("api/v1/fichas", pedido);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("DOCUMENTO_NAO_GUARDADO", await ClienteApi.CodigoErroAsync(resposta));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Fichas.AnyAsync(f => f.NomeCompleto == nome)));
    }

    /// <summary>
    /// RF-124: se o ficheiro guardado não puder ser obtido, a resposta é 503 e a ficha continua consultável.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FicheiroEmFaltaDa503()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        using var pedido = ClienteFichas.Carregamento(ClienteFichas.Png, "foto.png", "DocumentoIdentificacao");
        using var carregado = await cliente.PostAsync($"api/v1/fichas/{fichaId}/documentos", pedido);
        var documentoId = (await ClienteApi.JsonAsync(carregado)).GetProperty("id").GetGuid();
        var caminho = await this.fabrica.ComContextoAsync(c => c.Documentos.Where(d => d.Id == documentoId).Select(d => d.CaminhoRelativo).SingleAsync());
        File.Delete(Path.Combine(this.fabrica.PastaDocumentos, caminho));

        using var resposta = await cliente.GetAsync($"api/v1/documentos/{documentoId}");
        using var ficha = await cliente.GetAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("DOCUMENTO_INDISPONIVEL", await ClienteApi.CodigoErroAsync(resposta));
        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
    }

    /// <summary>
    /// OP-11 em multipart: a edição acrescenta documentos à ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarComDocumentos()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var dados = new { nomeCompleto = "Maria Editada", dataNascimento = "1941-05-17", contactoEmergencia = "+351 910 000 009" };
        using var pedido = ClienteFichas.Multipart(dados, (ClienteFichas.Pdf, "relatorio.pdf", "RelatorioClinico"));

        using var resposta = await cliente.PutAsync($"api/v1/fichas/{fichaId}", pedido);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Maria Editada", (await ClienteApi.JsonAsync(resposta)).GetProperty("nomeCompleto").GetString());
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Documentos.CountAsync(d => d.FichaId == fichaId)));
    }

    /// <summary>
    /// OP-14: ao eliminar a ficha, os ficheiros dos documentos são removidos da pasta do servidor (RF-123, RNF-046).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaRemoveOsFicheiros()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        using var pedido = ClienteFichas.Carregamento(ClienteFichas.Pdf);
        (await cliente.PostAsync($"api/v1/fichas/{fichaId}/documentos", pedido)).Dispose();
        var caminho = await this.fabrica.ComContextoAsync(c => c.Documentos.Where(d => d.FichaId == fichaId).Select(d => d.CaminhoRelativo).SingleAsync());

        using var resposta = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.False(File.Exists(Path.Combine(this.fabrica.PastaDocumentos, caminho)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Documentos.AnyAsync(d => d.FichaId == fichaId)));
    }
}
