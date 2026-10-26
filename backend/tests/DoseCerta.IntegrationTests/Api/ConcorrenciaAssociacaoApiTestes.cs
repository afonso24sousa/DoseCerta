// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da F004 com pedidos simultâneos, com a API completa e o SQL Server (RF-043, RF-132, RNF-046; D8).
/// A recusa pode vir da verificação prévia ou do índice único; o caminho do índice é provado em
/// <c>UnidadeDeTrabalhoUnicidadeAssociacaoTestes</c>.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ConcorrenciaAssociacaoApiTestes
{
    private const int Pedidos = 5;

    private static readonly string[] CodigosDaAceitacaoRepetida = { "CONVITE_JA_RESPONDIDO", "PESSOA_JA_ASSOCIADA" };

    private static readonly HttpStatusCode[] EstadosDaTransferenciaSimultanea = { HttpStatusCode.OK, HttpStatusCode.InternalServerError };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcorrenciaAssociacaoApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ConcorrenciaAssociacaoApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-043 com pedidos simultâneos: só um convite é criado; os outros recebem 409 <c>CONVITE_PENDENTE_EXISTE</c>, nunca 500,
    /// e fica um só e-mail de convite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitesSimultaneosCriamSoUmConvite()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var email = DadosTeste.EmailUnico("rui");

        var respostas = await Task.WhenAll(Enumerable.Range(0, Pedidos)
            .Select(_ => ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email, papel = "Familiar" })));
        try
        {
            Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
            foreach (var recusada in respostas.Where(r => r.StatusCode != HttpStatusCode.Created))
            {
                Assert.Equal(HttpStatusCode.Conflict, recusada.StatusCode);
                Assert.Equal("CONVITE_PENDENTE_EXISTE", await ClienteApi.CodigoErroAsync(recusada));
            }
        }
        finally
        {
            Array.ForEach(respostas, r => r.Dispose());
        }

        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Convites.CountAsync(v => v.FichaId == fichaId)));
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.EnviosEmail.CountAsync(e => c.Convites.Any(v => v.Id == e.ConviteId && v.FichaId == fichaId))));
    }

    /// <summary>
    /// D8 com pedidos simultâneos: o mesmo convite aceite várias vezes ao mesmo tempo associa o convidado uma só vez; os outros pedidos
    /// recebem 409 (<c>CONVITE_JA_RESPONDIDO</c> se chegam depois da primeira resposta, <c>PESSOA_JA_ASSOCIADA</c> se a corrida chega
    /// ao índice), nunca 500.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AceitacoesSimultaneasAssociamUmaSoVez()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(clienteCuidador);
        using var convite = await ClienteAssociacao.ConvidarAsync(clienteCuidador, fichaId, new { email = convidado.Email, papel = "Familiar" });
        convite.EnsureSuccessStatusCode();
        var conviteId = (await ClienteApi.JsonAsync(convite)).GetProperty("id").GetGuid();

        var respostas = await Task.WhenAll(Enumerable.Range(0, Pedidos).Select(_ => ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId)));
        try
        {
            Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
            foreach (var recusada in respostas.Where(r => r.StatusCode != HttpStatusCode.OK))
            {
                Assert.Equal(HttpStatusCode.Conflict, recusada.StatusCode);
                Assert.Contains(await ClienteApi.CodigoErroAsync(recusada), CodigosDaAceitacaoRepetida);
            }
        }
        finally
        {
            Array.ForEach(respostas, r => r.Dispose());
        }

        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.FichaId == fichaId && a.UtilizadorId == convidado.Id)));
    }

    /// <summary>
    /// D8, limitação conhecida: duas transferências do Cuidador principal em simultâneo, para Cuidadores diferentes, podem dar 500
    /// (a violação de <c>UQ_Associacao_Principal</c> não é traduzida), mas a ficha fica sempre com exatamente um Cuidador principal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferenciasSimultaneasDeixamSempreUmSoPrincipal()
    {
        var (principal, segundo, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Cuidador);
        var terceiro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = principal.Cliente;
        segundo.Cliente.Dispose();
        terceiro.Cliente.Dispose();
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, terceiro.Id, Papel.Cuidador);

        var respostas = await Task.WhenAll(
            ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, segundo.Id),
            ClienteAssociacao.TransferirPrincipalAsync(cliente, fichaId, terceiro.Id));
        try
        {
            Assert.Contains(respostas, r => r.StatusCode == HttpStatusCode.OK);
            Assert.All(respostas, r => Assert.Contains(r.StatusCode, EstadosDaTransferenciaSimultanea));
        }
        finally
        {
            Array.ForEach(respostas, r => r.Dispose());
        }

        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.FichaId == fichaId && a.EPrincipal)));
        Assert.Equal(3, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.FichaId == fichaId && a.Papel == Papel.Cuidador)));
    }
}
