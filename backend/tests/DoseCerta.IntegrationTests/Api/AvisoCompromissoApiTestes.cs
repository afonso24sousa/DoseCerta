// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DoseCerta.Api.Arranque;
using DoseCerta.Api.Processos;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração do aviso antes de um compromisso (I056: passo 6 do processo periódico, RF-116, RF-117, RF-118,
/// RF-122, RF-142, RNF-040, RNF-043), com o SQL Server e um serviço de e-mail controlado. Os compromissos são registados,
/// editados e removidos pela API (OP-41, OP-43, OP-44) e os avisos consultados em OP-51. O processo periódico está desligado
/// nos testes: o passo é chamado com o instante que cada teste precisa, e um teste corre o ciclo completo com o relógio real.
/// </summary>
[Collection(ColecaoApi.Nome)]
public sealed class AvisoCompromissoApiTestes : IDisposable
{
    private const string Motivo = "Consulta de rotina";
    private const string Local = "Centro de Saúde Fictício";

    private readonly FabricaApi fabrica;
    private readonly EmailSenderControlado email = new();
    private readonly WebApplicationFactory<PontoEntradaApi> fabricaComEmail;
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AvisoCompromissoApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public AvisoCompromissoApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
        this.fabricaComEmail = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<IEmailSender>();
            servicos.AddSingleton<IEmailSender>(this.email);
        }));
    }

    /// <summary>
    /// RF-117 CA1 e CA3, RF-118, RNF-040: um compromisso registado depois do instante do aviso é avisado no ciclo seguinte do
    /// processo periódico; cada Cuidador e o Familiar veem em OP-51 a notificação <c>ProximoCompromisso</c> com o motivo, o
    /// nome do utente, o local, a data e a hora de Lisboa; os e-mails saem no mesmo ciclo sem dados de saúde no assunto; o
    /// Utilizador sem associação não recebe nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloAvisaOCompromissoRegistadoDepoisDoInstanteDoAviso()
    {
        using var cenario = await this.CenarioAsync();
        var dataHora = DadosTeste.Agora().AddMinutes(30);
        var compromissoId = await RegistarAsync(cenario.Cuidador, cenario.FichaId, dataHora, 1440);
        var processo = this.fabricaComEmail.Services.GetServices<IHostedService>().OfType<ProcessoPeriodico>().Single();

        await processo.ExecutarCicloAsync(CancellationToken.None);

        var local = this.calendario.ParaLocal(dataHora);
        var mensagem = string.Format(
            CultureInfo.InvariantCulture,
            "Compromisso de Maria Fictícia Santos: {0}, em {1}, a {2:dd/MM/yyyy} às {2:HH:mm}.",
            Motivo,
            Local,
            local);
        foreach (var cliente in new[] { cenario.Cuidador, cenario.Familiar })
        {
            var item = Assert.Single(await AvisosAsync(cliente), i => i.GetProperty("mensagem").GetString() == mensagem);
            Assert.Equal(cenario.FichaId, item.GetProperty("fichaId").GetGuid());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("medicamento").ValueKind);
        }

        Assert.Empty(await AvisosAsync(cenario.Estranho));
        var notificacoes = await this.NotificacoesAsync(compromissoId);
        Assert.Equal(
            new[] { cenario.CuidadorId, cenario.OutroCuidadorId, cenario.FamiliarId }.ToHashSet(),
            notificacoes.Select(n => n.DestinatarioId).ToHashSet());
        foreach (var destino in new[] { cenario.CuidadorEmail, cenario.OutroCuidadorEmail, cenario.FamiliarEmail })
        {
            Assert.Contains(this.email.Tentativas, t => t.Destino == destino && t.Assunto == TextosNotificacao.AssuntoProximoCompromisso);
        }

        Assert.DoesNotContain(this.email.Tentativas, t => t.Assunto.Contains(Motivo, StringComparison.Ordinal));
    }

    /// <summary>
    /// RNF-043: dois passos em paralelo e um terceiro depois criam uma só notificação por destinatário.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ExecucoesConcorrentesNaoDuplicam()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        var compromissoId = await RegistarAsync(cenario.Cuidador, cenario.FichaId, agora.AddMinutes(20), 30);

        await Task.WhenAll(this.AvisarAsync(agora.AddMinutes(1)), this.AvisarAsync(agora.AddMinutes(1)));
        await this.AvisarAsync(agora.AddMinutes(2));

        var notificacoes = await this.NotificacoesAsync(compromissoId);
        Assert.Equal(3, notificacoes.Count);
        Assert.Equal(3, notificacoes.Select(n => n.DestinatarioId).Distinct().Count());
        var aviso = await this.fabrica.ComContextoAsync(c => c.Compromissos.AsNoTracking().SingleAsync(x => x.Id == compromissoId));
        Assert.Equal(agora.AddMinutes(1), aviso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RF-081 e RF-117: OP-43 muda a data depois do aviso; o aviso é reposto e sai de novo no instante novo, com a data nova.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EdicaoDaDataRepoeOAviso()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        var compromissoId = await RegistarAsync(cenario.Cuidador, cenario.FichaId, agora.AddMinutes(20), 30);
        await this.AvisarAsync(agora.AddMinutes(1));
        var novaData = agora.AddHours(3);

        using (var resposta = await cenario.Cuidador.PutAsJsonAsync($"api/v1/compromissos/{compromissoId}", Pedido(novaData, 60)))
        {
            resposta.EnsureSuccessStatusCode();
        }

        await this.AvisarAsync(agora.AddMinutes(2));
        Assert.Equal(3, (await this.NotificacoesAsync(compromissoId)).Count);
        await this.AvisarAsync(novaData.AddMinutes(-60));

        var novas = (await this.NotificacoesAsync(compromissoId)).Where(n => n.InstanteLimiteUtc == novaData.AddMinutes(-60)).ToList();
        Assert.Equal(3, novas.Count);
        var hora = this.calendario.ParaLocal(novaData).ToString("HH:mm", CultureInfo.InvariantCulture);
        Assert.All(novas, n => Assert.EndsWith($"às {hora}.", n.Mensagem, StringComparison.Ordinal));
    }

    /// <summary>
    /// RF-117 CA2: OP-44 remove o compromisso antes do aviso e o aviso não sai.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RemocaoCancelaOAviso()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        var compromissoId = await RegistarAsync(cenario.Cuidador, cenario.FichaId, agora.AddMinutes(20), 30);

        using (var resposta = await cenario.Cuidador.DeleteAsync($"api/v1/compromissos/{compromissoId}"))
        {
            resposta.EnsureSuccessStatusCode();
        }

        await this.AvisarAsync(agora.AddMinutes(1));
        Assert.Empty(await this.NotificacoesAsync(compromissoId));
    }

    /// <summary>
    /// RF-122 e RF-142: a ficha arquivada não gera avisos e, depois de reativada, o aviso que ficou devido enquanto esteve
    /// arquivada também não sai.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaEReativadaNaoGeraAvisosDoPeriodoArquivado()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        var compromissoId = await RegistarAsync(cenario.Cuidador, cenario.FichaId, agora.AddMinutes(20), 30);
        using (var arquivo = await cenario.Cuidador.PostAsync($"api/v1/fichas/{cenario.FichaId}/arquivo", null))
        {
            arquivo.EnsureSuccessStatusCode();
        }

        await this.AvisarAsync(agora.AddMinutes(1));
        Assert.Empty(await this.NotificacoesAsync(compromissoId));

        // A reativação acontece pelo menos um segundo depois do registo, para o aviso ficar no período arquivado.
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        using (var reativacao = await cenario.Cuidador.DeleteAsync($"api/v1/fichas/{cenario.FichaId}/arquivo"))
        {
            reativacao.EnsureSuccessStatusCode();
        }

        await this.AvisarAsync(DadosTeste.Agora().AddSeconds(1));
        Assert.Empty(await this.NotificacoesAsync(compromissoId));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.fabricaComEmail.Dispose();
    }

    private static object Pedido(DateTime dataHoraUtc, int antecedencia)
    {
        return new
        {
            dataHora = new DateTimeOffset(dataHoraUtc, TimeSpan.Zero),
            local = Local,
            motivo = Motivo,
            antecedenciaAvisoMinutos = antecedencia,
        };
    }

    private static async Task<Guid> RegistarAsync(HttpClient cliente, Guid fichaId, DateTime dataHoraUtc, int antecedencia)
    {
        using var resposta = await cliente.PostAsJsonAsync($"api/v1/fichas/{fichaId}/compromissos", Pedido(dataHoraUtc, antecedencia));
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private static async Task<List<JsonElement>> AvisosAsync(HttpClient cliente)
    {
        using var resposta = await cliente.GetAsync("api/v1/notificacoes");
        resposta.EnsureSuccessStatusCode();
        var corpo = await ClienteApi.JsonAsync(resposta);
        return corpo.EnumerateArray().Where(i => i.GetProperty("tipo").GetString() == "ProximoCompromisso").ToList();
    }

    private async Task AvisarAsync(DateTime agoraUtc)
    {
        await using var ambito = this.fabricaComEmail.Services.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<AvisoCompromissoService>().AvisarProximosAsync(agoraUtc, CancellationToken.None);
    }

    private Task<List<Notificacao>> NotificacoesAsync(Guid compromissoId)
    {
        return this.fabrica.ComContextoAsync(c => c.Notificacoes
            .AsNoTracking()
            .Where(n => n.ReferenciaId == compromissoId && n.Tipo == TipoNotificacao.ProximoCompromisso)
            .ToListAsync());
    }

    private async Task<Cenario> CenarioAsync()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var outro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Cuidador");
        var familiar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Joana Familiar");
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        outro.Cliente.Dispose();
        var fichaId = await ClienteFichas.CriarFichaAsync(cuidador.Cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, outro.Id, Papel.Cuidador);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiar.Id, Papel.Familiar);
        return new Cenario(
            cuidador.Cliente,
            cuidador.Id,
            cuidador.Email,
            outro.Id,
            outro.Email,
            familiar.Cliente,
            familiar.Id,
            familiar.Email,
            estranho.Cliente,
            fichaId);
    }

    /// <summary>
    /// Ficha de um Cuidador principal com outro Cuidador e um Familiar, e um Utilizador sem associação.
    /// </summary>
    /// <param name="Cuidador">Cliente do Cuidador principal.</param>
    /// <param name="CuidadorId">Identificador do Cuidador principal.</param>
    /// <param name="CuidadorEmail">E-mail do Cuidador principal.</param>
    /// <param name="OutroCuidadorId">Identificador do outro Cuidador.</param>
    /// <param name="OutroCuidadorEmail">E-mail do outro Cuidador.</param>
    /// <param name="Familiar">Cliente do Familiar.</param>
    /// <param name="FamiliarId">Identificador do Familiar.</param>
    /// <param name="FamiliarEmail">E-mail do Familiar.</param>
    /// <param name="Estranho">Cliente de um Utilizador não associado.</param>
    /// <param name="FichaId">Identificador da ficha.</param>
    private sealed record Cenario(
        HttpClient Cuidador,
        Guid CuidadorId,
        string CuidadorEmail,
        Guid OutroCuidadorId,
        string OutroCuidadorEmail,
        HttpClient Familiar,
        Guid FamiliarId,
        string FamiliarEmail,
        HttpClient Estranho,
        Guid FichaId) : IDisposable
    {
        public void Dispose()
        {
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
