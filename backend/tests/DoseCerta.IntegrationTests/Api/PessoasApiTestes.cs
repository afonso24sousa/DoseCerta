// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração das pessoas associadas a uma ficha (F004, OP-18), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class PessoasApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public PessoasApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-18, DP-03: o Cuidador e o Familiar recebem a mesma lista, com o nome, o e-mail e o papel de cada pessoa
    /// e o Cuidador principal primeiro.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorEFamiliarVeemAsPessoasDaFicha()
    {
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Principal");
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Ana Familiar");
        using var clienteCriador = criador.Cliente;
        using var clienteFamiliar = familiar.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(clienteCriador);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiar.Id, Papel.Familiar);

        var doCriador = await ClienteAssociacao.PessoasAsync(clienteCriador, fichaId);
        var doFamiliar = await ClienteAssociacao.PessoasAsync(clienteFamiliar, fichaId);

        Assert.Equal(2, doCriador.Count);
        AssertPessoa(doCriador[0], criador.Id, "Zé Principal", criador.Email, "Cuidador", principal: true, provisorio: false);
        AssertPessoa(doCriador[1], familiar.Id, "Ana Familiar", familiar.Email, "Familiar", principal: false, provisorio: false);
        Assert.Equal(doCriador.Select(p => p.GetRawText()), doFamiliar.Select(p => p.GetRawText()));
    }

    /// <summary>
    /// OP-18, RF-025: com um convite para Cuidador principal pendente, o criador aparece como Cuidador principal provisório
    /// e o convidado não aparece, porque ainda não está associado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PrincipalProvisorioApareceComoProvisorio()
    {
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rita Criadora");
        using var cliente = criador.Cliente;
        var fichaId = await ClienteAssociacao.CriarFichaComConviteParaPrincipalAsync(cliente, DadosTeste.EmailUnico("convidado"));

        var pessoas = await ClienteAssociacao.PessoasAsync(cliente, fichaId);

        var unica = Assert.Single(pessoas);
        AssertPessoa(unica, criador.Id, "Rita Criadora", criador.Email, "Cuidador", principal: true, provisorio: true);
    }

    /// <summary>
    /// RNF-005, RP-02: quem não está associado, o Utilizador com convite pendente e quem pede uma ficha inexistente recebem 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaoAssociadoEConvidadoPendenteRecebem404()
    {
        var emailConvidado = DadosTeste.EmailUnico("convidado");
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCriador = criador.Cliente;
        using var clienteEstranho = estranho.Cliente;
        var fichaId = await ClienteAssociacao.CriarFichaComConviteParaPrincipalAsync(clienteCriador, emailConvidado);
        var (clienteConvidado, _) = await ClienteAssociacao.NovoUtilizadorComEmailAsync(this.fabrica, "Rui Convidado", emailConvidado);
        using var convidado = clienteConvidado;

        using var doEstranho = await clienteEstranho.GetAsync($"api/v1/fichas/{fichaId}/pessoas");
        using var doConvidado = await convidado.GetAsync($"api/v1/fichas/{fichaId}/pessoas");
        using var inexistente = await clienteCriador.GetAsync($"api/v1/fichas/{Guid.NewGuid()}/pessoas");

        foreach (var resposta in new[] { doEstranho, doConvidado, inexistente })
        {
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(resposta));
        }
    }

    /// <summary>
    /// RP-13: numa ficha arquivada a lista de pessoas continua disponível.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaContinuaAListarAsPessoas()
    {
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = criador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        using var arquivo = await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        var pessoas = await ClienteAssociacao.PessoasAsync(cliente, fichaId);

        Assert.Equal(criador.Id, Assert.Single(pessoas).GetProperty("utilizadorId").GetGuid());
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await cliente.GetAsync($"api/v1/fichas/{Guid.NewGuid()}/pessoas");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("NAO_AUTENTICADO", await ClienteApi.CodigoErroAsync(resposta));
    }

    private static void AssertPessoa(System.Text.Json.JsonElement pessoa, Guid id, string nome, string email, string papel, bool principal, bool provisorio)
    {
        Assert.Equal(id, pessoa.GetProperty("utilizadorId").GetGuid());
        Assert.Equal(nome, pessoa.GetProperty("nome").GetString());
        Assert.Equal(email, pessoa.GetProperty("email").GetString());
        Assert.Equal(papel, pessoa.GetProperty("papel").GetString());
        Assert.Equal(principal, pessoa.GetProperty("principal").GetBoolean());
        Assert.Equal(provisorio, pessoa.GetProperty("provisorio").GetBoolean());
    }
}
