// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoseCerta.Api.Arranque;
using DoseCerta.IntegrationTests.Suporte;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da configuração da solução (I029): arranque da API e ligação ao SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class SaudeApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaudeApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public SaudeApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// O endpoint de verificação do estado responde 200 sem sessão e confirma o acesso à base de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SaudeRespondeSaudavelComBaseDeDados()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await cliente.GetAsync(new Uri(ArranqueApi.CaminhoSaude, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Saudavel", corpo.GetProperty("estado").GetString());
        Assert.Equal("Saudavel", corpo.GetProperty("verificacoes").GetProperty("baseDados").GetString());
    }

    /// <summary>
    /// A base de dados de testes foi criada pelas migrações e aceita ligações.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task BaseDeDadosCriadaPelasMigracoesAceitaLigacoes()
    {
        var liga = await this.fabrica.ComContextoAsync(c => c.Database.CanConnectAsync());

        Assert.True(liga);
    }
}
