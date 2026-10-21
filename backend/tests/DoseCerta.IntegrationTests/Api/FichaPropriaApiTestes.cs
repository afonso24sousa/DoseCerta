// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da ficha própria (F003, OP-09 com <c>propria=true</c>), com a API completa e o SQL Server (RF-030 a RF-032).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class FichaPropriaApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichaPropriaApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public FichaPropriaApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-030 CA1: a ficha própria é criada com o titular como Cuidador principal, nunca provisório, e sem convite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaPropriaDeixaOTitularComoPrincipal()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;

        using var resposta = await cliente.PostAsJsonAsync("api/v1/fichas", PedidoProprio());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        var fichaId = corpo.GetProperty("id").GetGuid();
        Assert.True(corpo.GetProperty("eProprio").GetBoolean());
        Assert.Equal("Ativa", corpo.GetProperty("estado").GetString());
        Assert.Equal("Cuidador", corpo.GetProperty("meuPapel").GetString());
        Assert.True(corpo.GetProperty("souPrincipal").GetBoolean());
        var ficha = await this.fabrica.ComContextoAsync(c => c.Fichas.SingleAsync(f => f.Id == fichaId));
        Assert.Equal(TipoFicha.Propria, ficha.Tipo);
        Assert.Equal(conta.Id, ficha.UtilizadorProprioId);
        Assert.Equal(conta.Id, ficha.CriadaPorId);
        var associacao = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId));
        Assert.Equal(conta.Id, associacao.UtilizadorId);
        Assert.Equal(Papel.Cuidador, associacao.Papel);
        Assert.True(associacao.EPrincipal);
        Assert.False(associacao.EProvisorio);
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-031 CA1 com pedidos simultâneos: só um cria a ficha própria; os outros recebem 409 <c>FICHA_PROPRIA_JA_EXISTE</c>, nunca 500.
    /// A recusa pode vir da verificação prévia ou do índice <c>UQ_Ficha_UtilizadorProprio</c>; o caminho do índice é provado em
    /// <c>UnidadeDeTrabalhoUnicidadeTestes</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PedidosSimultaneosCriamSoUmaFichaPropria()
    {
        const int Pedidos = 5;
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;

        var respostas = await Task.WhenAll(Enumerable.Range(0, Pedidos).Select(_ => cliente.PostAsJsonAsync("api/v1/fichas", PedidoProprio())));
        try
        {
            Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
            var recusadas = respostas.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
            Assert.Equal(Pedidos - 1, recusadas.Count);
            Assert.All(recusadas, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
            foreach (var recusada in recusadas)
            {
                Assert.Equal("FICHA_PROPRIA_JA_EXISTE", await ClienteApi.CodigoErroAsync(recusada));
            }

            Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Fichas.CountAsync(f => f.UtilizadorProprioId == conta.Id)));
            Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.UtilizadorId == conta.Id)));
        }
        finally
        {
            foreach (var resposta in respostas)
            {
                resposta.Dispose();
            }
        }
    }

    /// <summary>
    /// OP-09: com <c>propria=true</c> o campo <c>cuidadorPrincipal</c> é ignorado; o titular fica Cuidador principal e não é criado convite
    /// (decisão do Executor: a documentação só exige o campo numa ficha de terceiro).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorPrincipalEIgnoradoNaFichaPropria()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;

        using var resposta = await cliente.PostAsJsonAsync("api/v1/fichas", new
        {
            propria = true,
            nomeCompleto = "Pessoa Fictícia",
            dataNascimento = "1980-02-29",
            contactoEmergencia = "+351 910 000 003",
            cuidadorPrincipal = new { eu = false, email = DadosTeste.EmailUnico("rui") },
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var fichaId = (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
        var associacao = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId));
        Assert.Equal(conta.Id, associacao.UtilizadorId);
        Assert.True(associacao.EPrincipal);
        Assert.False(associacao.EProvisorio);
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-031 CA2: quem já tem ficha própria pode criar fichas de terceiros.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ComFichaPropriaPodeCriarFichaDeTerceiro()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;

        using var propria = await cliente.PostAsJsonAsync("api/v1/fichas", PedidoProprio());
        using var terceiro = await cliente.PostAsJsonAsync("api/v1/fichas", ClienteFichas.PedidoTerceiro());

        Assert.Equal(HttpStatusCode.Created, propria.StatusCode);
        Assert.Equal(HttpStatusCode.Created, terceiro.StatusCode);
        Assert.False((await ClienteApi.JsonAsync(terceiro)).GetProperty("eProprio").GetBoolean());
        Assert.Equal(2, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.UtilizadorId == conta.Id && a.EPrincipal)));
    }

    /// <summary>
    /// RF-032 CA1: a ficha própria aceita documentos na criação e é listada, editada, arquivada e reativada como uma ficha de terceiro.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaPropriaTemAsMesmasOperacoesDasOutras()
    {
        var conta = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = conta.Cliente;
        using var pedido = ClienteFichas.Multipart(PedidoProprio(), (ClienteFichas.Pdf, "receita.pdf", "ReceitaMedica"));

        using var criada = await cliente.PostAsync("api/v1/fichas", pedido);
        var fichaId = (await ClienteApi.JsonAsync(criada)).GetProperty("id").GetGuid();
        var documentos = await ClienteApi.JsonAsync(await cliente.GetAsync($"api/v1/fichas/{fichaId}/documentos"));
        var lista = await ClienteApi.JsonAsync(await cliente.GetAsync("api/v1/fichas"));
        using var editada = await cliente.PutAsJsonAsync($"api/v1/fichas/{fichaId}", new
        {
            nomeCompleto = "Pessoa Fictícia Costa",
            dataNascimento = "1980-02-29",
            contactoEmergencia = "+351 910 000 004",
            grauAutonomiaId = 1,
        });
        using var arquivo = await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        using var reativacao = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}/arquivo");

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        Assert.Equal("ReceitaMedica", Assert.Single(documentos.EnumerateArray()).GetProperty("categoria").GetString());
        var resumo = Assert.Single(lista.EnumerateArray());
        Assert.Equal(fichaId, resumo.GetProperty("id").GetGuid());
        Assert.True(resumo.GetProperty("eProprio").GetBoolean());
        Assert.True(resumo.GetProperty("souPrincipal").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(editada);
        Assert.Equal("Pessoa Fictícia Costa", corpo.GetProperty("nomeCompleto").GetString());
        Assert.True(corpo.GetProperty("eProprio").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, arquivo.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, reativacao.StatusCode);
        var ficha = await this.fabrica.ComContextoAsync(c => c.Fichas.SingleAsync(f => f.Id == fichaId));
        Assert.Equal(EstadoFicha.Ativa, ficha.Estado);
        Assert.Equal(TipoFicha.Propria, ficha.Tipo);
    }

    /// <summary>
    /// RF-032 e RNF-005: na ficha própria, o Familiar só consulta (403 ao editar) e quem não está associado recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaFichaPropriaSoConsulta()
    {
        var titular = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = titular.Cliente;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var contaEstranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var estranho = contaEstranho.Cliente;
        using var criada = await cliente.PostAsJsonAsync("api/v1/fichas", PedidoProprio());
        var fichaId = (await ClienteApi.JsonAsync(criada)).GetProperty("id").GetGuid();
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, contaFamiliar.Id, Papel.Familiar);

        using var consulta = await familiar.GetAsync($"api/v1/fichas/{fichaId}");
        using var edicao = await familiar.PutAsJsonAsync($"api/v1/fichas/{fichaId}", PedidoProprio());
        using var doEstranho = await estranho.GetAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(consulta);
        Assert.True(corpo.GetProperty("eProprio").GetBoolean());
        Assert.Equal("Familiar", corpo.GetProperty("meuPapel").GetString());
        Assert.False(corpo.GetProperty("souPrincipal").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(edicao));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
    }

    private static object PedidoProprio()
    {
        return new
        {
            propria = true,
            nomeCompleto = "Pessoa Fictícia",
            dataNascimento = "1980-02-29",
            contactoEmergencia = "+351 910 000 003",
        };
    }
}
