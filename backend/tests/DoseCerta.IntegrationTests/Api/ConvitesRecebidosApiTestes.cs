// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da área de convites (F004, OP-20), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ConvitesRecebidosApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvitesRecebidosApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ConvitesRecebidosApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-042 CA1: o Utilizador registado vê os convites pendentes para o seu e-mail, com a ficha, o papel oferecido e quem convidou,
    /// do mais recente para o mais antigo; os convites para outros e-mails não aparecem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task UtilizadorVeOsConvitesParaOSeuEmail()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Convidado");
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        var fichaMaria = await ClienteFichas.CriarFichaAsync(clienteCuidador, "Maria Fictícia Santos");
        var fichaJoaquim = await ClienteFichas.CriarFichaAsync(clienteCuidador, "Joaquim Fictício");
        var primeiro = await ConvidarAsync(clienteCuidador, fichaMaria, convidado.Email, "Familiar");
        await this.RecuarCriacaoAsync(primeiro, minutos: 10);
        var segundo = await ConvidarAsync(clienteCuidador, fichaJoaquim, convidado.Email.ToUpperInvariant(), "Cuidador");
        await ConvidarAsync(clienteCuidador, fichaMaria, DadosTeste.EmailUnico("outra"), "Familiar");

        var recebidos = await ClienteAssociacao.ConvitesRecebidosAsync(clienteConvidado);

        Assert.Equal(new[] { segundo, primeiro }, recebidos.Select(r => r.GetProperty("id").GetGuid()));
        var maisAntigo = recebidos[1];
        Assert.Equal(fichaMaria, maisAntigo.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", maisAntigo.GetProperty("nomeFicha").GetString());
        Assert.Equal("Familiar", maisAntigo.GetProperty("papelOferecido").GetString());
        Assert.False(maisAntigo.GetProperty("paraPrincipal").GetBoolean());
        Assert.Equal("Zé Cuidador", maisAntigo.GetProperty("convidadoPor").GetString());
        Assert.Equal("Pendente", maisAntigo.GetProperty("estado").GetString());
        Assert.Empty(await ClienteAssociacao.ConvitesRecebidosAsync(clienteCuidador));
    }

    /// <summary>
    /// RF-044 CA1: o convite para um e-mail sem conta aparece na área de convites da conta criada depois com esse e-mail,
    /// e o registo (OP-01) já indica um convite pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteAntesDoRegistoApareceDepoisDeCriarAConta()
    {
        var email = DadosTeste.EmailUnico("joao");
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCuidador = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(clienteCuidador);
        var conviteId = await ConvidarAsync(clienteCuidador, fichaId, email, "Familiar");

        using var anonimo = this.fabrica.CreateClient();
        using var registo = await anonimo.PostAsJsonAsync("api/v1/auth/registo", new { nome = "João Fictício", email, palavraPasse = ClienteApi.PalavraPasse });
        registo.EnsureSuccessStatusCode();
        var token = await ClienteApi.IniciarSessaoAsync(anonimo, email);
        anonimo.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(1, (await ClienteApi.JsonAsync(registo)).GetProperty("convitesPendentes").GetInt32());
        Assert.Equal(conviteId, Assert.Single(await ClienteAssociacao.ConvitesRecebidosAsync(anonimo)).GetProperty("id").GetGuid());
    }

    /// <summary>
    /// RF-046, UC-14 A5: o convite já respondido deixa de aparecer; o convite de uma ficha arquivada depois de convidar continua
    /// a aparecer, porque o convidado pode responder na mesma.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RespondidoNaoApareceEFichaArquivadaContinuaAAparecer()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        var fichaRespondida = await ClienteFichas.CriarFichaAsync(clienteCuidador, "Respondida Fictícia");
        var fichaArquivada = await ClienteFichas.CriarFichaAsync(clienteCuidador, "Arquivada Fictícia");
        var respondido = await ConvidarAsync(clienteCuidador, fichaRespondida, convidado.Email, "Familiar");
        var naArquivada = await ConvidarAsync(clienteCuidador, fichaArquivada, convidado.Email, "Familiar");
        await this.fabrica.ComContextoAsync(async c =>
        {
            var convite = await c.Convites.SingleAsync(v => v.Id == respondido);
            convite.Recusar(convidado.Id, DadosTeste.Agora());
            return await c.SaveChangesAsync();
        });
        using var arquivo = await clienteCuidador.PostAsync($"api/v1/fichas/{fichaArquivada}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        var recebidos = await ClienteAssociacao.ConvitesRecebidosAsync(clienteConvidado);

        Assert.Equal(naArquivada, Assert.Single(recebidos).GetProperty("id").GetGuid());
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await cliente.GetAsync("api/v1/convites");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("NAO_AUTENTICADO", await ClienteApi.CodigoErroAsync(resposta));
    }

    private static async Task<Guid> ConvidarAsync(HttpClient cliente, Guid fichaId, string email, string papel)
    {
        using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email, papel });
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private Task<int> RecuarCriacaoAsync(Guid conviteId, int minutos)
    {
        return this.fabrica.ComContextoAsync(c => c.Convites
            .Where(v => v.Id == conviteId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.CriadoEmUtc, v => v.CriadoEmUtc.AddMinutes(-minutos))));
    }
}
