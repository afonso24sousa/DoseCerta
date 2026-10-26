// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Arranque;
using DoseCerta.Api.Processos;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Emails;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração do envio e dos reenvios do e-mail de convite (F004, RF-135, RF-136; passo 7 do processo periódico),
/// com o SQL Server e um serviço de e-mail controlado. O processo periódico está desligado nos testes; os ciclos são chamados
/// diretamente, com o instante que cada teste precisa.
/// </summary>
[Collection(ColecaoApi.Nome)]
public sealed class EnvioEmailConviteApiTestes : IDisposable
{
    private readonly FabricaApi fabrica;
    private readonly EmailSenderControlado email = new();
    private readonly WebApplicationFactory<PontoEntradaApi> fabricaComEmail;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailConviteApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public EnvioEmailConviteApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
        this.fabricaComEmail = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<IEmailSender>();
            servicos.AddSingleton<IEmailSender>(this.email);
        }));
    }

    /// <summary>
    /// RF-136 CA1 e CA2, RF-135 CA1: com o serviço de e-mail sempre em falha, o convite é criado (201), o e-mail é tentado uma vez
    /// e reenviado três vezes, de 5 em 5 minutos, e fica não entregue; o convite continua na área de convites do convidado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDoEmailNaoImpedeOConviteEReenviaTresVezes()
    {
        var (cuidador, convidado, conviteId) = await this.ConviteAsync();
        using var clienteCuidador = cuidador;
        using var clienteConvidado = convidado.Cliente;
        this.email.Falhar = true;
        var inicio = await this.ProximaTentativaAsync(conviteId);

        foreach (var minutos in new[] { 0, 4, 5, 10, 15, 20 })
        {
            await this.ProcessarAsync(inicio.AddMinutes(minutos));
        }

        var envio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail.SingleAsync(e => e.ConviteId == conviteId));
        Assert.Equal(EstadoEnvioEmail.NaoEntregue, envio.Estado);
        Assert.Equal(3, envio.Reenvios);
        Assert.Equal(inicio, envio.PrimeiraTentativaEmUtc);
        Assert.Equal(inicio.AddMinutes(15), envio.UltimaTentativaEmUtc);
        Assert.Null(envio.ProximaTentativaEmUtc);
        Assert.Equal(4, this.email.Tentativas.Count(t => t.Destino == convidado.Email));
        var recebido = Assert.Single(await ClienteAssociacao.ConvitesRecebidosAsync(clienteConvidado));
        Assert.Equal(conviteId, recebido.GetProperty("id").GetGuid());
        Assert.Equal(EstadoConvite.Pendente, await this.fabrica.ComContextoAsync(c => c.Convites.Where(v => v.Id == conviteId).Select(v => v.Estado).SingleAsync()));
    }

    /// <summary>
    /// RF-041, RNF-018: o e-mail de convite é submetido ao serviço de e-mail com o assunto e o corpo do convite, e fica enviado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailDeConviteEEnviado()
    {
        var (cuidador, convidado, conviteId) = await this.ConviteAsync();
        cuidador.Dispose();
        convidado.Cliente.Dispose();

        await this.ProcessarAsync(await this.ProximaTentativaAsync(conviteId));

        var envio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail.SingleAsync(e => e.ConviteId == conviteId));
        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        var tentativa = Assert.Single(this.email.Tentativas, t => t.Destino == convidado.Email);
        Assert.True(tentativa.Aceite);
        Assert.Equal("DoseCerta: recebeu um convite para uma ficha", tentativa.Assunto);
        Assert.Contains("Recebeu um convite de Zé Cuidador para a ficha de Maria Fictícia Santos no DoseCerta, como Familiar.", tentativa.Corpo, StringComparison.Ordinal);
    }

    /// <summary>
    /// O processo periódico registado no arranque executa o passo 7: um ciclo envia o e-mail de convite devido.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ProcessoPeriodicoEnviaOEmail()
    {
        var (cuidador, convidado, conviteId) = await this.ConviteAsync();
        cuidador.Dispose();
        convidado.Cliente.Dispose();
        var processo = this.fabricaComEmail.Services.GetServices<IHostedService>().OfType<ProcessoPeriodico>().Single();

        await processo.ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(
            EstadoEnvioEmail.Enviado,
            await this.fabrica.ComContextoAsync(c => c.EnviosEmail.Where(e => e.ConviteId == conviteId).Select(e => e.Estado).SingleAsync()));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.fabricaComEmail.Dispose();
    }

    private async Task<(HttpClient Cuidador, (HttpClient Cliente, string Email, Guid Id) Convidado, Guid ConviteId)> ConviteAsync()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Convidado");
        var fichaId = await ClienteFichas.CriarFichaAsync(cuidador.Cliente);
        using var resposta = await ClienteAssociacao.ConvidarAsync(cuidador.Cliente, fichaId, new { email = convidado.Email, papel = "Familiar" });
        resposta.EnsureSuccessStatusCode();
        return (cuidador.Cliente, convidado, (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid());
    }

    private Task<DateTime> ProximaTentativaAsync(Guid conviteId)
    {
        return this.fabrica.ComContextoAsync(c => c.EnviosEmail.Where(e => e.ConviteId == conviteId).Select(e => e.ProximaTentativaEmUtc!.Value).SingleAsync());
    }

    private async Task ProcessarAsync(DateTime agoraUtc)
    {
        await using var ambito = this.fabricaComEmail.Services.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<EnvioEmailService>().ProcessarPendentesAsync(agoraUtc, CancellationToken.None);
    }
}
