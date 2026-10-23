// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração do registo da notificação e do e-mail de convite (F004, RF-041; OP-09 e OP-19), com o SQL Server.
/// O envio e os reenvios são testados à parte.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class EmailConviteApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailConviteApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public EmailConviteApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-041 CA1, D1: o convite para um e-mail com conta grava a notificação <c>ConviteRecebido</c> dessa conta e um e-mail pendente,
    /// ligado ao convite e à notificação, com a primeira tentativa devida de imediato.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteParaContaGravaNotificacaoEEmail()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        convidado.Cliente.Dispose();
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);

        var conviteId = await ConvidarAsync(cliente, fichaId, convidado.Email);

        var notificacao = await this.fabrica.ComContextoAsync(c => c.Notificacoes.SingleAsync(n => n.ReferenciaId == conviteId));
        Assert.Equal(convidado.Id, notificacao.DestinatarioId);
        Assert.Equal(fichaId, notificacao.FichaId);
        Assert.Equal(TipoNotificacao.ConviteRecebido, notificacao.Tipo);
        Assert.Equal(TipoReferencia.Convite, notificacao.ReferenciaTipo);
        Assert.Contains("Zé Cuidador", notificacao.Mensagem, StringComparison.Ordinal);
        var envio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail.SingleAsync(e => e.ConviteId == conviteId));
        Assert.Equal(notificacao.Id, envio.NotificacaoId);
        Assert.Equal(convidado.Email, envio.EnderecoDestino);
        Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
        Assert.Equal(0, envio.Reenvios);
        Assert.Equal(envio.CriadoEmUtc, envio.ProximaTentativaEmUtc);
    }

    /// <summary>
    /// RF-041 CA2: o convite para um e-mail sem conta (OP-19) e o convite para Cuidador principal da criação da ficha (OP-09)
    /// gravam só o e-mail pendente, sem notificação.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteSemContaGravaSoOEmail()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var daOp19 = await ConvidarAsync(cliente, fichaId, DadosTeste.EmailUnico("semconta"));
        var outraFicha = await ClienteAssociacao.CriarFichaComConviteParaPrincipalAsync(cliente, DadosTeste.EmailUnico("principal"));
        var daOp09 = await this.fabrica.ComContextoAsync(c => c.Convites.Where(v => v.FichaId == outraFicha).Select(v => v.Id).SingleAsync());

        foreach (var conviteId in new[] { daOp19, daOp09 })
        {
            var envio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail.SingleAsync(e => e.ConviteId == conviteId));
            Assert.Null(envio.NotificacaoId);
            Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
            Assert.False(await this.fabrica.ComContextoAsync(c => c.Notificacoes.AnyAsync(n => n.ReferenciaId == conviteId)));
        }
    }

    /// <summary>
    /// RF-043, RNF-046: um convite recusado por ser repetido não grava notificação nem e-mail.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteRecusadoNaoGravaEmail()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        convidado.Cliente.Dispose();
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ConvidarAsync(cliente, fichaId, convidado.Email);

        using var repetido = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email = convidado.Email, papel = "Cuidador" });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Notificacoes.CountAsync(n => n.FichaId == fichaId)));
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.EnviosEmail.CountAsync(e => c.Convites.Any(v => v.Id == e.ConviteId && v.FichaId == fichaId))));
    }

    /// <summary>
    /// RF-123 (I033) com convites: eliminar uma ficha sem registos apaga também os convites, as notificações de convite e os e-mails.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaApagaConvitesNotificacoesEEmails()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        convidado.Cliente.Dispose();
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var comConta = await ConvidarAsync(cliente, fichaId, convidado.Email);
        var semConta = await ConvidarAsync(cliente, fichaId, DadosTeste.EmailUnico("semconta"));

        using var eliminacao = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.NoContent, eliminacao.StatusCode);
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Notificacoes.AnyAsync(n => n.FichaId == fichaId)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.EnviosEmail.AnyAsync(e => e.ConviteId == comConta || e.ConviteId == semConta)));
    }

    private static async Task<Guid> ConvidarAsync(HttpClient cliente, Guid fichaId, string email)
    {
        using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email, papel = "Familiar" });
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }
}
