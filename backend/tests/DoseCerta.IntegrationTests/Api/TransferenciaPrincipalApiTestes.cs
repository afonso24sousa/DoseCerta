// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da transferência do papel de Cuidador principal (F004, OP-26), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class TransferenciaPrincipalApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferenciaPrincipalApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public TransferenciaPrincipalApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-132 CA1, RNF-046: o segundo Cuidador passa a Cuidador principal, o anterior continua Cuidador e a base de dados
    /// fica com um único Cuidador principal (índice <c>UQ_Associacao_Principal</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferenciaDeixaUmUnicoPrincipal()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        using var clientePrincipal = principal.Cliente;
        cuidador.Cliente.Dispose();

        using var resposta = await ClienteAssociacao.TransferirPrincipalAsync(clientePrincipal, fichaId, cuidador.Id);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal(cuidador.Id, corpo.GetProperty("utilizadorId").GetGuid());
        Assert.Equal(cuidador.Email, corpo.GetProperty("email").GetString());
        Assert.Equal("Cuidador", corpo.GetProperty("papel").GetString());
        Assert.True(corpo.GetProperty("principal").GetBoolean());
        Assert.False(corpo.GetProperty("provisorio").GetBoolean());
        var associacoes = await this.fabrica.ComContextoAsync(c => c.Associacoes.Where(a => a.FichaId == fichaId).ToListAsync());
        Assert.Equal(cuidador.Id, Assert.Single(associacoes, a => a.EPrincipal).UtilizadorId);
        var anterior = Assert.Single(associacoes, a => a.UtilizadorId == principal.Id);
        Assert.Equal(Papel.Cuidador, anterior.Papel);
        Assert.False(anterior.EPrincipal);
    }

    /// <summary>
    /// RF-132, <c>UQ_Associacao_Principal</c>: transferir o papel para B e de volta para A funciona nos dois sentidos. O EF Core ordena
    /// os <c>UPDATE</c> pela chave da associação, por isso pelo menos uma das transferências grava a linha do novo principal antes da
    /// do anterior; o índice único filtrado não pode recusar nenhuma delas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferenciaFuncionaNosDoisSentidos()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        using var cliente = principal.Cliente;
        cuidador.Cliente.Dispose();

        using var paraB = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, cuidador.Id);
        using var deVoltaParaA = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, principal.Id);

        Assert.Equal(HttpStatusCode.OK, paraB.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deVoltaParaA.StatusCode);
        var principais = await this.fabrica.ComContextoAsync(c => c.Associacoes.Where(a => a.FichaId == fichaId && a.EPrincipal).ToListAsync());
        Assert.Equal(principal.Id, Assert.Single(principais).UtilizadorId);
    }

    /// <summary>
    /// DP-02, RP-08, RF-133 CA2: um Cuidador que não é o principal transfere o papel para si próprio e, a seguir,
    /// despromove o anterior Cuidador principal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorQueNaoEOPrincipalTransfereEDespromoveOAnterior()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        principal.Cliente.Dispose();
        using var clienteCuidador = cuidador.Cliente;

        using var transferencia = await ClienteAssociacao.TransferirPrincipalAsync(clienteCuidador, fichaId, cuidador.Id);
        using var despromocao = await ClienteAssociacao.AlterarPapelAsync(clienteCuidador, fichaId, principal.Id, "Familiar");

        Assert.Equal(HttpStatusCode.OK, transferencia.StatusCode);
        Assert.Equal(HttpStatusCode.OK, despromocao.StatusCode);
        var pessoas = await ClienteAssociacao.PessoasAsync(clienteCuidador, fichaId);
        Assert.Equal(cuidador.Id, pessoas[0].GetProperty("utilizadorId").GetGuid());
        Assert.True(pessoas[0].GetProperty("principal").GetBoolean());
        Assert.Equal("Familiar", pessoas[1].GetProperty("papel").GetString());
    }

    /// <summary>
    /// RF-034, UC-16 E1: na ficha própria o papel não se transfere (403), nem para outro Cuidador nem a pedido do titular.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaFichaPropriaOPapelNaoSeTransfere()
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

        using var peloTitular = await ClienteAssociacao.TransferirPrincipalAsync(clienteTitular, fichaId, cuidador.Id);
        using var peloCuidador = await ClienteAssociacao.TransferirPrincipalAsync(clienteCuidador, fichaId, cuidador.Id);

        foreach (var resposta in new[] { peloTitular, peloCuidador })
        {
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal("CUIDADOR_PRINCIPAL_PROTEGIDO", await ClienteApi.CodigoErroAsync(resposta));
        }

        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == titular.Id && a.EPrincipal)));
    }

    /// <summary>
    /// UC-16 E2, D6: o destinatário Familiar ou não associado à ficha dá 409 <c>DESTINATARIO_NAO_E_CUIDADOR</c> e nada muda.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DestinatarioQueNaoECuidadorDa409()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = principal.Cliente;
        familiar.Cliente.Dispose();
        estranho.Cliente.Dispose();

        using var paraFamiliar = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, familiar.Id);
        using var paraEstranho = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, estranho.Id);

        foreach (var resposta in new[] { paraFamiliar, paraEstranho })
        {
            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal("DESTINATARIO_NAO_E_CUIDADOR", await ClienteApi.CodigoErroAsync(resposta));
        }

        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == principal.Id && a.EPrincipal)));
    }

    /// <summary>
    /// D7: transferir o papel para quem já é Cuidador principal dá 200 sem alterações.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferirParaOPrincipalAtualDa200SemAlteracoes()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        using var cliente = principal.Cliente;
        cuidador.Cliente.Dispose();

        using var resposta = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, principal.Id);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(principal.Id, (await ClienteApi.JsonAsync(resposta)).GetProperty("utilizadorId").GetGuid());
        Assert.True(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId && a.UtilizadorId == principal.Id && a.EPrincipal)));
    }

    /// <summary>
    /// RNF-028: o destinatário em falta ou que não é um identificador dá 400 <c>VALIDACAO_INVALIDA</c> com o campo <c>utilizadorId</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DestinatarioEmFaltaOuInvalidoDa400()
    {
        var (principal, cuidador, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        using var cliente = principal.Cliente;
        cuidador.Cliente.Dispose();

        foreach (var valor in new object?[] { null, "nao-e-um-guid", Guid.Empty })
        {
            using var resposta = await ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, valor);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var corpo = await ClienteApi.JsonAsync(resposta);
            Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
            Assert.Contains(corpo.GetProperty("campos").EnumerateArray(), c => c.GetProperty("campo").GetString() == "utilizadorId");
        }
    }

    /// <summary>
    /// RNF-004, RNF-005, RP-13: o Familiar recebe 403, quem não está associado recebe 404 e, numa ficha arquivada, 409.
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
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, estranho.Id, Papel.Cuidador);
        using var remocao = await ClienteAssociacao.RemoverAcessoAsync(clientePrincipal, fichaId, estranho.Id);
        remocao.EnsureSuccessStatusCode();

        using var doFamiliar = await ClienteAssociacao.TransferirPrincipalAsync(clienteFamiliar, fichaId, principal.Id);
        using var doEstranho = await ClienteAssociacao.TransferirPrincipalAsync(clienteEstranho, fichaId, principal.Id);
        using var arquivo = await clientePrincipal.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();
        using var arquivada = await ClienteAssociacao.TransferirPrincipalAsync(clientePrincipal, fichaId, principal.Id);

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await ClienteAssociacao.TransferirPrincipalAsync(cliente, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
