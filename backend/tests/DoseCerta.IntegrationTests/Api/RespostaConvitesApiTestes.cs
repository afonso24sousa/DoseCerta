// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da aceitação e da recusa de convites (F004, OP-21 e OP-22), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class RespostaConvitesApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="RespostaConvitesApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public RespostaConvitesApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-045 CA1, RF-048: o convidado aceita o convite para Familiar, recebe 200 com o seu papel, passa a consultar a ficha,
    /// não a edita e o convite fica aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarAceitaEFicaSoComConsulta()
    {
        var (cuidador, convidado, fichaId, conviteId) = await this.ConviteAsync("Familiar");
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        using var antes = await clienteConvidado.GetAsync($"api/v1/fichas/{fichaId}");

        using var resposta = await ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId);

        Assert.Equal(HttpStatusCode.NotFound, antes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal(convidado.Id, corpo.GetProperty("utilizadorId").GetGuid());
        Assert.Equal("Rui Convidado", corpo.GetProperty("nome").GetString());
        Assert.Equal(convidado.Email, corpo.GetProperty("email").GetString());
        Assert.Equal("Familiar", corpo.GetProperty("papel").GetString());
        Assert.False(corpo.GetProperty("principal").GetBoolean());
        Assert.False(corpo.GetProperty("provisorio").GetBoolean());
        using var consulta = await clienteConvidado.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        using var edicao = await ClienteAssociacao.EditarFichaAsync(clienteConvidado, fichaId);
        Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
        Assert.Empty(await ClienteAssociacao.ConvitesRecebidosAsync(clienteConvidado));
        var convite = await this.fabrica.ComContextoAsync(c => c.Convites.SingleAsync(v => v.Id == conviteId));
        Assert.Equal(EstadoConvite.Aceite, convite.Estado);
        Assert.Equal(convidado.Id, convite.UtilizadorConvidadoId);
    }

    /// <summary>
    /// RF-045, RF-047: quem aceita um convite para Cuidador passa a poder editar a ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorAceitaEPassaAEscrever()
    {
        var (cuidador, convidado, fichaId, conviteId) = await this.ConviteAsync("Cuidador");
        cuidador.Cliente.Dispose();
        using var clienteConvidado = convidado.Cliente;

        using var resposta = await ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Cuidador", (await ClienteApi.JsonAsync(resposta)).GetProperty("papel").GetString());
        using var edicao = await ClienteAssociacao.EditarFichaAsync(clienteConvidado, fichaId);
        Assert.Equal(HttpStatusCode.OK, edicao.StatusCode);
    }

    /// <summary>
    /// RF-046 CA1 e RF-043 CA2: recusar dá 204, não associa o convidado e permite convidar de novo o mesmo e-mail para a mesma ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RecusarNaoAssociaEPermiteNovoConvite()
    {
        var (cuidador, convidado, fichaId, conviteId) = await this.ConviteAsync("Familiar");
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;

        using var resposta = await ClienteAssociacao.RecusarConviteAsync(clienteConvidado, conviteId);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        using var consulta = await clienteConvidado.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
        Assert.Equal(EstadoConvite.Recusado, await this.fabrica.ComContextoAsync(c => c.Convites.Where(v => v.Id == conviteId).Select(v => v.Estado).SingleAsync()));
        using var novoConvite = await ClienteAssociacao.ConvidarAsync(clienteCuidador, fichaId, new { email = convidado.Email, papel = "Familiar" });
        Assert.Equal(HttpStatusCode.Created, novoConvite.StatusCode);
    }

    /// <summary>
    /// UC-14 E2, RP-09: um convite dirigido a outro e-mail, ou que não existe, dá 404 <c>CONVITE_NAO_ENCONTRADO</c> ao aceitar e ao
    /// recusar, e o convite continua pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteDeOutroOuInexistenteDa404()
    {
        var (cuidador, convidado, _, conviteId) = await this.ConviteAsync("Familiar");
        cuidador.Cliente.Dispose();
        convidado.Cliente.Dispose();
        var outro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteOutro = outro.Cliente;

        using var aceitarDeOutro = await ClienteAssociacao.AceitarConviteAsync(clienteOutro, conviteId);
        using var recusarDeOutro = await ClienteAssociacao.RecusarConviteAsync(clienteOutro, conviteId);
        using var aceitarInexistente = await ClienteAssociacao.AceitarConviteAsync(clienteOutro, Guid.NewGuid());

        foreach (var resposta in new[] { aceitarDeOutro, recusarDeOutro, aceitarInexistente })
        {
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal("CONVITE_NAO_ENCONTRADO", await ClienteApi.CodigoErroAsync(resposta));
        }

        Assert.Equal(EstadoConvite.Pendente, await this.fabrica.ComContextoAsync(c => c.Convites.Where(v => v.Id == conviteId).Select(v => v.Estado).SingleAsync()));
    }

    /// <summary>
    /// UC-14 E1: um convite já aceite não é aceite nem recusado outra vez (409 <c>CONVITE_JA_RESPONDIDO</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteJaRespondidoDa409()
    {
        var (cuidador, convidado, _, conviteId) = await this.ConviteAsync("Familiar");
        cuidador.Cliente.Dispose();
        using var clienteConvidado = convidado.Cliente;
        using var primeira = await ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId);
        primeira.EnsureSuccessStatusCode();

        using var aceitarDeNovo = await ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId);
        using var recusarDepois = await ClienteAssociacao.RecusarConviteAsync(clienteConvidado, conviteId);

        foreach (var resposta in new[] { aceitarDeNovo, recusarDepois })
        {
            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal("CONVITE_JA_RESPONDIDO", await ClienteApi.CodigoErroAsync(resposta));
        }
    }

    /// <summary>
    /// D4: um Familiar da ficha muda o e-mail da conta (OP-05) para o e-mail de um convite pendente dessa ficha; ao aceitar recebe
    /// 409 <c>PESSOA_JA_ASSOCIADA</c>, continua Familiar e o convite fica pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PessoaJaAssociadaNaoAceita()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var clientePrincipal = principal.Cliente;
        using var clienteFamiliar = familiar.Cliente;
        var emailNovo = DadosTeste.EmailUnico("novo");
        var conviteId = await ConvidarAsync(clientePrincipal, fichaId, emailNovo, "Cuidador");
        using var mudanca = await clienteFamiliar.PutAsJsonAsync("api/v1/conta", new { nome = "Rui Associado", email = emailNovo });
        mudanca.EnsureSuccessStatusCode();

        using var resposta = await ClienteAssociacao.AceitarConviteAsync(clienteFamiliar, conviteId);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("PESSOA_JA_ASSOCIADA", await ClienteApi.CodigoErroAsync(resposta));
        Assert.Equal(EstadoConvite.Pendente, await this.fabrica.ComContextoAsync(c => c.Convites.Where(v => v.Id == conviteId).Select(v => v.Estado).SingleAsync()));
        var associacao = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId && a.UtilizadorId == familiar.Id));
        Assert.Equal(Papel.Familiar, associacao.Papel);
    }

    /// <summary>
    /// UC-14 A5, RP-13: arquivada a ficha depois do convite, o convidado aceita na mesma (sem <c>FICHA_ARQUIVADA</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaFichaArquivadaOConvidadoAceitaNaMesma()
    {
        var (cuidador, convidado, fichaId, conviteId) = await this.ConviteAsync("Familiar");
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        using var arquivo = await clienteCuidador.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        using var resposta = await ClienteAssociacao.AceitarConviteAsync(clienteConvidado, conviteId);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    /// <summary>
    /// RF-025 e RF-026 CA1: com o convite para Cuidador principal da OP-09, quem cria a conta com esse e-mail vê o convite, aceita-o
    /// e passa a Cuidador principal; o criador provisório deixa de estar associado e recebe 404 na ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AceitarConviteParaPrincipalTransfereOPapel()
    {
        var email = DadosTeste.EmailUnico("principal");
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCriador = criador.Cliente;
        var fichaId = await ClienteAssociacao.CriarFichaComConviteParaPrincipalAsync(clienteCriador, email);
        var (clienteConvidado, convidadoId) = await ClienteAssociacao.NovoUtilizadorComEmailAsync(this.fabrica, "Rui Principal", email);
        using var convidado = clienteConvidado;
        var recebido = Assert.Single(await ClienteAssociacao.ConvitesRecebidosAsync(convidado));
        Assert.True(recebido.GetProperty("paraPrincipal").GetBoolean());

        using var resposta = await ClienteAssociacao.AceitarConviteAsync(convidado, recebido.GetProperty("id").GetGuid());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.True(corpo.GetProperty("principal").GetBoolean());
        Assert.False(corpo.GetProperty("provisorio").GetBoolean());
        Assert.Equal("Cuidador", corpo.GetProperty("papel").GetString());
        var pessoas = await ClienteAssociacao.PessoasAsync(convidado, fichaId);
        Assert.Equal(convidadoId, Assert.Single(pessoas).GetProperty("utilizadorId").GetGuid());
        using var doCriador = await clienteCriador.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.NotFound, doCriador.StatusCode);
    }

    /// <summary>
    /// RF-027 CA1: recusar o convite para Cuidador principal mantém o criador como Cuidador principal, já não provisório,
    /// e o criador pode convidar outra pessoa para Cuidador principal (OP-19).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RecusarConviteParaPrincipalMantemOCriadorEPermiteOutroConvite()
    {
        var email = DadosTeste.EmailUnico("principal");
        var criador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCriador = criador.Cliente;
        var fichaId = await ClienteAssociacao.CriarFichaComConviteParaPrincipalAsync(clienteCriador, email);
        var (clienteConvidado, _) = await ClienteAssociacao.NovoUtilizadorComEmailAsync(this.fabrica, "Rui Recusa", email);
        using var convidado = clienteConvidado;
        var conviteId = Assert.Single(await ClienteAssociacao.ConvitesRecebidosAsync(convidado)).GetProperty("id").GetGuid();

        using var resposta = await ClienteAssociacao.RecusarConviteAsync(convidado, conviteId);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var criadorNaFicha = Assert.Single(await ClienteAssociacao.PessoasAsync(clienteCriador, fichaId));
        Assert.True(criadorNaFicha.GetProperty("principal").GetBoolean());
        Assert.False(criadorNaFicha.GetProperty("provisorio").GetBoolean());
        using var outroConvite = await ClienteAssociacao.ConvidarAsync(
            clienteCriador, fichaId, new { email = DadosTeste.EmailUnico("outro"), papel = "Cuidador", paraPrincipal = true });
        Assert.Equal(HttpStatusCode.Created, outroConvite.StatusCode);
    }

    /// <summary>
    /// RF-017: sem token, aceitar e recusar são recusados com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var aceitar = await ClienteAssociacao.AceitarConviteAsync(cliente, Guid.NewGuid());
        using var recusar = await ClienteAssociacao.RecusarConviteAsync(cliente, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, aceitar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, recusar.StatusCode);
    }

    private static async Task<Guid> ConvidarAsync(HttpClient cliente, Guid fichaId, string email, string papel)
    {
        using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email, papel });
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private async Task<((HttpClient Cliente, string Email, Guid Id) Cuidador, (HttpClient Cliente, string Email, Guid Id) Convidado, Guid FichaId, Guid ConviteId)> ConviteAsync(string papel)
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Convidado");
        var fichaId = await ClienteFichas.CriarFichaAsync(cuidador.Cliente);
        var conviteId = await ConvidarAsync(cuidador.Cliente, fichaId, convidado.Email, papel);
        return (cuidador, convidado, fichaId, conviteId);
    }
}
