// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da remoção de acesso (F004, OP-25), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class RemocaoAcessoApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemocaoAcessoApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public RemocaoAcessoApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-131 CA1 e CA2: o Familiar removido deixa de estar associado na base de dados, sai da lista de pessoas e,
    /// no pedido seguinte, recebe 404 ao abrir a ficha, como se ela não existisse.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarRemovidoDeixaDeVerAFicha()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var clientePrincipal = principal.Cliente;
        using var clienteFamiliar = familiar.Cliente;

        using var resposta = await ClienteAssociacao.RemoverAcessoAsync(clientePrincipal, fichaId, familiar.Id);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(0, resposta.Content.Headers.ContentLength ?? 0);
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == familiar.Id)));
        Assert.Equal(principal.Id, Assert.Single(await ClienteAssociacao.PessoasAsync(clientePrincipal, fichaId)).GetProperty("utilizadorId").GetGuid());
        using var abrir = await clienteFamiliar.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.NotFound, abrir.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(abrir));
    }

    /// <summary>
    /// Matriz de Permissões, secção 6: um Cuidador que não é o principal sai da ficha por iniciativa própria e deixa de a ver.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorSaiDaFichaPorIniciativaPropria()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        principal.Cliente.Dispose();
        using var clienteCuidador = cuidador.Cliente;

        using var resposta = await ClienteAssociacao.RemoverAcessoAsync(clienteCuidador, fichaId, cuidador.Id);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        using var abrir = await clienteCuidador.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.NotFound, abrir.StatusCode);
    }

    /// <summary>
    /// DP-01: o Familiar não se remove a si próprio (403 <c>ACESSO_PROIBIDO</c>) e continua associado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoSeRemoveASiProprio()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        principal.Cliente.Dispose();
        using var clienteFamiliar = familiar.Cliente;

        using var resposta = await ClienteAssociacao.RemoverAcessoAsync(clienteFamiliar, fichaId, familiar.Id);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(resposta));
        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == familiar.Id)));
    }

    /// <summary>
    /// RF-133 CA1 e RF-134: o Cuidador principal não é removido, nem por si próprio; sozinho dá <c>FICHA_SEM_CUIDADOR</c> e,
    /// com outro Cuidador, <c>CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO</c>.
    /// </summary>
    /// <param name="comOutroCuidador">Indica se a ficha tem outro Cuidador.</param>
    /// <param name="codigoEsperado">Código de erro esperado.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false, "FICHA_SEM_CUIDADOR")]
    [InlineData(true, "CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO")]
    public async Task PrincipalNaoERemovido(bool comOutroCuidador, string codigoEsperado)
    {
        var (principal, outro, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, comOutroCuidador ? Papel.Cuidador : Papel.Familiar);
        using var clientePrincipal = principal.Cliente;
        outro.Cliente.Dispose();

        using var resposta = await ClienteAssociacao.RemoverAcessoAsync(clientePrincipal, fichaId, principal.Id);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(codigoEsperado, await ClienteApi.CodigoErroAsync(resposta));
        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == principal.Id && a.EPrincipal)));
    }

    /// <summary>
    /// RF-033 CA2: o titular da ficha própria não é removido, nem a pedido de outro Cuidador nem a pedido próprio (403).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TitularDaFichaPropriaNaoERemovido()
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

        using var porOutro = await ClienteAssociacao.RemoverAcessoAsync(clienteCuidador, fichaId, titular.Id);
        using var peloProprio = await ClienteAssociacao.RemoverAcessoAsync(clienteTitular, fichaId, titular.Id);

        foreach (var resposta in new[] { porOutro, peloProprio })
        {
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal("CUIDADOR_PRINCIPAL_PROTEGIDO", await ClienteApi.CodigoErroAsync(resposta));
        }
    }

    /// <summary>
    /// D6, RNF-005, RP-13: pessoa não associada dá 404 <c>PESSOA_NAO_ENCONTRADA</c>; quem não está associado à ficha recebe
    /// 404 <c>FICHA_NAO_ENCONTRADA</c>; numa ficha arquivada a remoção é recusada com 409 <c>FICHA_ARQUIVADA</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PessoaNaoAssociadaNaoAssociadoEFichaArquivadaSaoRecusados()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clientePrincipal = principal.Cliente;
        familiar.Cliente.Dispose();
        using var clienteEstranho = estranho.Cliente;

        using var pessoaNaoAssociada = await ClienteAssociacao.RemoverAcessoAsync(clientePrincipal, fichaId, estranho.Id);
        using var doEstranho = await ClienteAssociacao.RemoverAcessoAsync(clienteEstranho, fichaId, familiar.Id);
        using var arquivo = await clientePrincipal.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();
        using var arquivada = await ClienteAssociacao.RemoverAcessoAsync(clientePrincipal, fichaId, familiar.Id);

        Assert.Equal(HttpStatusCode.NotFound, pessoaNaoAssociada.StatusCode);
        Assert.Equal("PESSOA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(pessoaNaoAssociada));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == familiar.Id)));
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await ClienteAssociacao.RemoverAcessoAsync(cliente, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
