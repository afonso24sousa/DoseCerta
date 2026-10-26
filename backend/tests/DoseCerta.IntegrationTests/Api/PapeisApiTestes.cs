// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da promoção e da despromoção (F004, OP-23 e OP-24), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class PapeisApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="PapeisApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public PapeisApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-049 CA1, RP-01: o Familiar promovido recebe 200 com o novo papel e pode editar a ficha logo no pedido seguinte.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarPromovidoPassaAEscreverNoPedidoSeguinte()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var clientePrincipal = principal.Cliente;
        using var clienteFamiliar = familiar.Cliente;
        using var antes = await ClienteAssociacao.EditarFichaAsync(clienteFamiliar, fichaId);

        using var resposta = await ClienteAssociacao.AlterarPapelAsync(clientePrincipal, fichaId, familiar.Id, "Cuidador");

        Assert.Equal(HttpStatusCode.Forbidden, antes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal(familiar.Id, corpo.GetProperty("utilizadorId").GetGuid());
        Assert.Equal(familiar.Email, corpo.GetProperty("email").GetString());
        Assert.Equal("Cuidador", corpo.GetProperty("papel").GetString());
        Assert.False(corpo.GetProperty("principal").GetBoolean());
        Assert.False(corpo.GetProperty("provisorio").GetBoolean());
        using var depois = await ClienteAssociacao.EditarFichaAsync(clienteFamiliar, fichaId);
        Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
    }

    /// <summary>
    /// RF-130 CA1, RP-01: o Cuidador despromovido fica Familiar na base de dados e perde a escrita no pedido seguinte (403).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorDespromovidoPerdeAEscritaNoPedidoSeguinte()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        using var clientePrincipal = principal.Cliente;
        using var clienteCuidador = cuidador.Cliente;

        using var resposta = await ClienteAssociacao.AlterarPapelAsync(clientePrincipal, fichaId, cuidador.Id, "Familiar");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Familiar", (await ClienteApi.JsonAsync(resposta)).GetProperty("papel").GetString());
        var guardada = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId && a.UtilizadorId == cuidador.Id));
        Assert.Equal(Papel.Familiar, guardada.Papel);
        using var edicao = await ClienteAssociacao.EditarFichaAsync(clienteCuidador, fichaId);
        Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(edicao));
    }

    /// <summary>
    /// RF-133 e RF-134: o Cuidador principal não é despromovido; sozinho dá <c>FICHA_SEM_CUIDADOR</c> e, com outro Cuidador,
    /// <c>CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO</c>. Nada muda na base de dados.
    /// </summary>
    /// <param name="comOutroCuidador">Indica se a ficha tem outro Cuidador.</param>
    /// <param name="codigoEsperado">Código de erro esperado.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false, "FICHA_SEM_CUIDADOR")]
    [InlineData(true, "CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO")]
    public async Task PrincipalNaoEDespromovido(bool comOutroCuidador, string codigoEsperado)
    {
        var (principal, outro, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, comOutroCuidador ? Papel.Cuidador : Papel.Familiar);
        using var clientePrincipal = principal.Cliente;
        using var clienteOutro = outro.Cliente;

        using var resposta = await ClienteAssociacao.AlterarPapelAsync(clientePrincipal, fichaId, principal.Id, "Familiar");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(codigoEsperado, await ClienteApi.CodigoErroAsync(resposta));
        var guardada = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId && a.UtilizadorId == principal.Id));
        Assert.Equal(Papel.Cuidador, guardada.Papel);
        Assert.True(guardada.EPrincipal);
    }

    /// <summary>
    /// RF-033 CA1: o titular da ficha própria não é despromovido, nem a pedido de outro Cuidador nem a pedido próprio (403).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TitularDaFichaPropriaNaoEDespromovido()
    {
        var titular = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteTitular = titular.Cliente;
        using var clienteCuidador = cuidador.Cliente;
        using var criacao = await clienteTitular.PostAsJsonAsync("api/v1/fichas", new
        {
            propria = true,
            nomeCompleto = "Ana Fictícia",
            dataNascimento = "1980-02-29",
            contactoEmergencia = "+351 910 000 003",
        });
        criacao.EnsureSuccessStatusCode();
        var fichaId = (await ClienteApi.JsonAsync(criacao)).GetProperty("id").GetGuid();
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, cuidador.Id, Papel.Cuidador);

        using var porOutro = await ClienteAssociacao.AlterarPapelAsync(clienteCuidador, fichaId, titular.Id, "Familiar");
        using var peloProprio = await ClienteAssociacao.AlterarPapelAsync(clienteTitular, fichaId, titular.Id, "Familiar");

        foreach (var resposta in new[] { porOutro, peloProprio })
        {
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal("CUIDADOR_PRINCIPAL_PROTEGIDO", await ClienteApi.CodigoErroAsync(resposta));
        }
    }

    /// <summary>
    /// D6: a pessoa indicada tem de estar associada à ficha (404 <c>PESSOA_NAO_ENCONTRADA</c>), tanto ao promover como ao despromover.
    /// </summary>
    /// <param name="papel">Papel pretendido.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("Cuidador")]
    [InlineData("Familiar")]
    public async Task PessoaNaoAssociadaDa404(string papel)
    {
        var principal = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = principal.Cliente;
        estranho.Cliente.Dispose();
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);

        using var resposta = await ClienteAssociacao.AlterarPapelAsync(cliente, fichaId, estranho.Id, papel);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("PESSOA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(resposta));
    }

    /// <summary>
    /// RNF-028: papel em falta, com um texto que não é um papel ou com um número, dá 400 <c>VALIDACAO_INVALIDA</c> com o campo <c>papel</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PapelInvalidoDa400()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var cliente = principal.Cliente;
        familiar.Cliente.Dispose();

        foreach (var papel in new object?[] { null, "Administrador", 99 })
        {
            using var resposta = await ClienteAssociacao.AlterarPapelAsync(cliente, fichaId, familiar.Id, papel);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var corpo = await ClienteApi.JsonAsync(resposta);
            Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
            Assert.Contains(corpo.GetProperty("campos").EnumerateArray(), c => c.GetProperty("campo").GetString() == "papel");
        }

        var guardada = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId && a.UtilizadorId == familiar.Id));
        Assert.Equal(Papel.Familiar, guardada.Papel);
    }

    /// <summary>
    /// RNF-004, RF-048 e RP-13: o Familiar recebe 403 <c>ACESSO_PROIBIDO</c>, quem não está associado recebe 404 e,
    /// numa ficha arquivada, o Cuidador recebe 409 <c>FICHA_ARQUIVADA</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoAssociadoEFichaArquivadaSaoRecusados()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clientePrincipal = principal.Cliente;
        using var clienteFamiliar = familiar.Cliente;
        using var clienteEstranho = estranho.Cliente;

        using var doFamiliar = await ClienteAssociacao.AlterarPapelAsync(clienteFamiliar, fichaId, familiar.Id, "Cuidador");
        using var doEstranho = await ClienteAssociacao.AlterarPapelAsync(clienteEstranho, fichaId, familiar.Id, "Cuidador");
        using var arquivo = await clientePrincipal.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();
        using var arquivada = await ClienteAssociacao.AlterarPapelAsync(clientePrincipal, fichaId, familiar.Id, "Cuidador");

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
    }

    /// <summary>
    /// D7: pedir o papel atual dá 200 sem alterações, também sobre o Cuidador principal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PapelAtualDa200SemAlteracoes()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var cliente = principal.Cliente;
        familiar.Cliente.Dispose();

        using var doPrincipal = await ClienteAssociacao.AlterarPapelAsync(cliente, fichaId, principal.Id, "Cuidador");
        using var doFamiliar = await ClienteAssociacao.AlterarPapelAsync(cliente, fichaId, familiar.Id, "Familiar");

        Assert.Equal(HttpStatusCode.OK, doPrincipal.StatusCode);
        Assert.True((await ClienteApi.JsonAsync(doPrincipal)).GetProperty("principal").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, doFamiliar.StatusCode);
        Assert.Equal("Familiar", (await ClienteApi.JsonAsync(doFamiliar)).GetProperty("papel").GetString());
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await ClienteAssociacao.AlterarPapelAsync(cliente, Guid.NewGuid(), Guid.NewGuid(), "Cuidador");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
