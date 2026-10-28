// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoseCerta.Api.Arranque;
using DoseCerta.Api.Processos;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Emails;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Tomas;
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
/// Testes de integração das notificações de atraso das tomas (passos 2 e 3 do processo periódico), do envio dos e-mails de
/// notificação e de OP-51 (F011, I047), com o SQL Server e um serviço de e-mail controlado. O processo periódico está desligado
/// nos testes: os passos são chamados com o instante que cada teste precisa, e um teste corre o ciclo completo com o relógio real.
/// As tomas ficam previstas para daqui a uma hora, depois do instante desde o qual a ficha está ativa.
/// </summary>
[Collection(ColecaoApi.Nome)]
public sealed class NotificacoesApiTestes : IDisposable
{
    private static readonly string[] AsOito = { "08:00" };

    private readonly FabricaApi fabrica;
    private readonly EmailSenderControlado email = new();
    private readonly WebApplicationFactory<PontoEntradaApi> fabricaComEmail;
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificacoesApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public NotificacoesApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
        this.fabricaComEmail = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<IEmailSender>();
            servicos.AddSingleton<IEmailSender>(this.email);
        }));
    }

    /// <summary>
    /// RF-110 CA1 e CA3, RF-066: numa toma Leve, o passo 2 às T + 2 h ainda não notifica; um segundo depois, os dois Cuidadores
    /// (principal e outro) recebem uma notificação <c>TomaEmAtraso</c> com o e-mail registado; o passo 3 nunca notifica o
    /// Familiar e a toma continua <c>Pendente</c> na base de dados, com a marca do aviso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task LeveNotificaOsCuidadoresComAToleranciaExata()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddHours(1);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Leve);

        await this.NotificarAsync(prevista.AddHours(2));
        Assert.Empty(await this.NotificacoesDaTomaAsync(tomaId));

        await this.NotificarAsync(prevista.AddHours(2).AddSeconds(1));
        await this.NotificarAsync(prevista.AddHours(3));

        var notificacoes = await this.NotificacoesDaTomaAsync(tomaId);
        Assert.Equal(new[] { cenario.CuidadorId, cenario.OutroCuidadorId }.Order(), notificacoes.Select(n => n.DestinatarioId).Order());
        Assert.All(notificacoes, n =>
        {
            Assert.Equal(TipoNotificacao.TomaEmAtraso, n.Tipo);
            Assert.Equal(prevista.AddHours(2), n.InstanteLimiteUtc);
        });
        var ids = notificacoes.Select(n => (Guid?)n.Id).ToList();
        Assert.Equal(2, await this.fabrica.ComContextoAsync(c => c.EnviosEmail.CountAsync(e => ids.Contains(e.NotificacaoId))));
        var toma = await this.fabrica.ComContextoAsync(c => c.Tomas.AsNoTracking().SingleAsync(t => t.Id == tomaId));
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal(prevista.AddHours(2).AddSeconds(1), toma.CuidadoresNotificadosEmUtc);
        Assert.Null(toma.FamiliaresNotificadosEmUtc);
    }

    /// <summary>
    /// RF-112, RF-113 CA1, RNF-043: numa toma Grave, os Cuidadores são notificados depois dos 30 minutos e o Familiar 30 minutos
    /// depois; repetir os passos não duplica. Os e-mails são enviados e o Familiar vê a notificação em OP-51, com a ficha, o
    /// medicamento, a hora prevista, a hora da notificação e o e-mail enviado (RF-140 CA1).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GraveNotificaOsFamiliaresEOHistoricoMostraANotificacao()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddHours(1);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Grave);

        await this.NotificarAsync(prevista.AddMinutes(31));
        await this.NotificarAsync(prevista.AddMinutes(60));
        Assert.DoesNotContain(await this.NotificacoesDaTomaAsync(tomaId), n => n.Tipo == TipoNotificacao.TomaGraveFamiliares);
        await this.NotificarAsync(prevista.AddMinutes(61));
        await this.NotificarAsync(prevista.AddMinutes(61));
        await this.ProcessarEmailsAsync(prevista.AddMinutes(62));

        var notificacoes = await this.NotificacoesDaTomaAsync(tomaId);
        Assert.Equal(3, notificacoes.Count);
        var doFamiliar = Assert.Single(notificacoes, n => n.Tipo == TipoNotificacao.TomaGraveFamiliares);
        Assert.Equal(cenario.FamiliarId, doFamiliar.DestinatarioId);
        Assert.Equal(prevista.AddMinutes(60), doFamiliar.InstanteLimiteUtc);
        var tentativa = Assert.Single(this.email.Tentativas, t => t.Destino == cenario.FamiliarEmail);
        Assert.True(tentativa.Aceite);
        Assert.Equal(TextosNotificacao.AssuntoTomaGraveFamiliares, tentativa.Assunto);
        Assert.StartsWith(doFamiliar.Mensagem, tentativa.Corpo, StringComparison.Ordinal);

        using var resposta = await cenario.Familiar.GetAsync("api/v1/notificacoes");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var item = Assert.Single((await ClienteApi.JsonAsync(resposta)).EnumerateArray());
        var horaPrevista = this.calendario.ParaLocal(prevista).ToString("HH:mm", CultureInfo.InvariantCulture);
        Assert.Equal(doFamiliar.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("TomaGraveFamiliares", item.GetProperty("tipo").GetString());
        Assert.Equal($"A toma de Insulina de Maria Fictícia Santos das {horaPrevista} não foi confirmada.", item.GetProperty("mensagem").GetString());
        Assert.Equal(cenario.FichaId, item.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", item.GetProperty("nomeFicha").GetString());
        Assert.Equal("Insulina", item.GetProperty("medicamento").GetString());
        Assert.Equal(this.calendario.ParaLocal(prevista), item.GetProperty("previstaEm").GetDateTimeOffset());
        Assert.Equal(this.calendario.ParaLocal(prevista).Offset, item.GetProperty("previstaEm").GetDateTimeOffset().Offset);
        Assert.Equal(this.calendario.ParaLocal(prevista.AddMinutes(61)), item.GetProperty("criadaEm").GetDateTimeOffset());
        Assert.Equal("Enviado", item.GetProperty("email").GetString());
    }

    /// <summary>
    /// RF-112 CA2 e RF-113 CA2: a toma confirmada pelo Cuidador (OP-33) antes da deteção não gera notificações; a outra toma,
    /// por confirmar, gera.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaConfirmadaAntesDaDetecaoNaoGeraNotificacao()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddMinutes(-10);
        var confirmadaId = await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Grave);
        var porConfirmarId = await this.NovaTomaAsync(cenario.TratamentoId, prevista.AddSeconds(1), GrauImportancia.Grave);
        using var confirmacao = await cenario.Cuidador.PostAsync($"api/v1/tomas/{confirmadaId}/confirmacao", null);
        confirmacao.EnsureSuccessStatusCode();

        await this.NotificarAsync(prevista.AddMinutes(90));

        Assert.Empty(await this.NotificacoesDaTomaAsync(confirmadaId));
        Assert.Equal(3, (await this.NotificacoesDaTomaAsync(porConfirmarId)).Count);
    }

    /// <summary>
    /// RF-118 CA1, RF-119 CA1 e RF-141 CA1: com o serviço de e-mail sempre em falha, a notificação existe na mesma, o e-mail é
    /// tentado e reenviado três vezes de 5 em 5 minutos e o histórico mostra-o como <c>NaoEntregue</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDoEmailFicaNaoEntregueNoHistorico()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddHours(1);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Moderado);
        var criada = prevista.AddMinutes(61);
        await this.NotificarAsync(criada);
        this.email.Falhar = true;

        foreach (var minutos in new[] { 0, 5, 10, 15, 20 })
        {
            await this.ProcessarEmailsAsync(criada.AddMinutes(minutos));
        }

        var doCuidador = Assert.Single(await this.NotificacoesDaTomaAsync(tomaId), n => n.DestinatarioId == cenario.CuidadorId);
        var envio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail.AsNoTracking().SingleAsync(e => e.NotificacaoId == doCuidador.Id));
        Assert.Equal(EstadoEnvioEmail.NaoEntregue, envio.Estado);
        Assert.Equal(3, envio.Reenvios);
        Assert.Equal(4, this.email.Tentativas.Count(t => t.Destino == cenario.CuidadorEmail));
        using var resposta = await cenario.Cuidador.GetAsync("api/v1/notificacoes");
        var item = Assert.Single((await ClienteApi.JsonAsync(resposta)).EnumerateArray());
        Assert.Equal(doCuidador.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("TomaEmAtraso", item.GetProperty("tipo").GetString());
        Assert.Equal("NaoEntregue", item.GetProperty("email").GetString());
    }

    /// <summary>
    /// OP-51: cada Utilizador só vê as suas notificações (RF-140 CA3); <c>de</c> e <c>ate</c> filtram pelo dia de Lisboa da
    /// criação; <c>ate</c> anterior a <c>de</c> ou uma data inválida dão 400 com o campo; sem token dá 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task HistoricoSoDoProprioComFiltrosEValidacao()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddHours(1);
        await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Moderado);
        var criada = prevista.AddMinutes(61);
        await this.NotificarAsync(criada);
        var dia = this.Dia(criada);
        var vespera = this.calendario.DiaLocal(criada).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var anonimo = this.fabrica.CreateClient();

        using var doEstranho = await cenario.Estranho.GetAsync("api/v1/notificacoes");
        using var doFamiliar = await cenario.Familiar.GetAsync("api/v1/notificacoes");
        using var noDia = await cenario.Cuidador.GetAsync($"api/v1/notificacoes?de={dia}&ate={dia}");
        using var ateAVespera = await cenario.Cuidador.GetAsync($"api/v1/notificacoes?ate={vespera}");
        using var invertido = await cenario.Cuidador.GetAsync($"api/v1/notificacoes?de={dia}&ate={vespera}");
        using var invalido = await cenario.Cuidador.GetAsync("api/v1/notificacoes?de=amanha");
        using var semToken = await anonimo.GetAsync("api/v1/notificacoes");

        Assert.Equal(HttpStatusCode.OK, doEstranho.StatusCode);
        Assert.Equal(0, (await ClienteApi.JsonAsync(doEstranho)).GetArrayLength());
        Assert.Equal(0, (await ClienteApi.JsonAsync(doFamiliar)).GetArrayLength());
        Assert.Equal(1, (await ClienteApi.JsonAsync(noDia)).GetArrayLength());
        Assert.Equal(0, (await ClienteApi.JsonAsync(ateAVespera)).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
        Assert.Contains("ate", await CamposAsync(invertido));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Contains("de", await CamposAsync(invalido));
        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
    }

    /// <summary>
    /// US-I047-03, RF-041 e RF-141: um Utilizador só com um convite pendente, sem associação à ficha, consulta em OP-51 a
    /// notificação <c>ConviteRecebido</c>, sem medicamento nem hora prevista e com o e-mail ainda por enviar; depois de o e-mail
    /// falhar nas 3 tentativas de reenvio, a notificação continua no histórico, identificada como <c>NaoEntregue</c> (RF-119).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task UtilizadorComConvitePendenteVeANotificacaoMesmoSemEmail()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        var convidado = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Convidado");
        using var clienteCuidador = cuidador.Cliente;
        using var clienteConvidado = convidado.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(clienteCuidador);
        using var convite = await ClienteAssociacao.ConvidarAsync(clienteCuidador, fichaId, new { email = convidado.Email, papel = "Familiar" });
        convite.EnsureSuccessStatusCode();
        var conviteId = (await ClienteApi.JsonAsync(convite)).GetProperty("id").GetGuid();

        using var antes = await clienteConvidado.GetAsync("api/v1/notificacoes");
        var item = Assert.Single((await ClienteApi.JsonAsync(antes)).EnumerateArray());
        Assert.Equal("ConviteRecebido", item.GetProperty("tipo").GetString());
        Assert.Equal(fichaId, item.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", item.GetProperty("nomeFicha").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("medicamento").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("previstaEm").ValueKind);
        Assert.Equal("Pendente", item.GetProperty("email").GetString());

        this.email.Falhar = true;
        var inicio = await this.fabrica.ComContextoAsync(c => c.EnviosEmail
            .Where(e => e.ConviteId == conviteId).Select(e => e.ProximaTentativaEmUtc!.Value).SingleAsync());
        foreach (var minutos in new[] { 0, 5, 10, 15 })
        {
            await this.ProcessarEmailsAsync(inicio.AddMinutes(minutos));
        }

        using var depois = await clienteConvidado.GetAsync("api/v1/notificacoes");
        var naoEntregue = Assert.Single((await ClienteApi.JsonAsync(depois)).EnumerateArray());
        Assert.Equal(item.GetProperty("id").GetGuid(), naoEntregue.GetProperty("id").GetGuid());
        Assert.Equal("NaoEntregue", naoEntregue.GetProperty("email").GetString());
    }

    /// <summary>
    /// RF-112 CA3 e RF-142 CA2: com a ficha arquivada (OP-12) os passos não criam notificações da toma.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaNaoGeraNotificacoes()
    {
        using var cenario = await this.CenarioAsync();
        var prevista = DadosTeste.Agora().AddHours(1);
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, prevista, GrauImportancia.Grave);
        using var arquivo = await cenario.Cuidador.PostAsync($"api/v1/fichas/{cenario.FichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();

        await this.NotificarAsync(prevista.AddMinutes(61));

        Assert.Empty(await this.NotificacoesDaTomaAsync(tomaId));
    }

    /// <summary>
    /// RNF-040 e RNF-042: o processo periódico registado no arranque, com o relógio real, cria num só ciclo as notificações em
    /// falta de uma toma Grave com mais de 60 minutos (Cuidadores e Familiar) e envia os respetivos e-mails.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ProcessoPeriodicoNotificaEEnviaOsEmails()
    {
        using var cenario = await this.CenarioAsync();
        var agora = DadosTeste.Agora();
        await this.fabrica.ComContextoAsync(c => c.Fichas
            .Where(f => f.Id == cenario.FichaId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.CriadaEmUtc, agora.AddHours(-3)).SetProperty(f => f.AtivaDesdeUtc, agora.AddHours(-3))));
        var tomaId = await this.NovaTomaAsync(cenario.TratamentoId, agora.AddMinutes(-62), GrauImportancia.Grave);
        var processo = this.fabricaComEmail.Services.GetServices<IHostedService>().OfType<ProcessoPeriodico>().Single();

        await processo.ExecutarCicloAsync(CancellationToken.None);

        var notificacoes = await this.NotificacoesDaTomaAsync(tomaId);
        Assert.Equal(2, notificacoes.Count(n => n.Tipo == TipoNotificacao.TomaEmAtraso));
        Assert.Equal(cenario.FamiliarId, Assert.Single(notificacoes, n => n.Tipo == TipoNotificacao.TomaGraveFamiliares).DestinatarioId);
        foreach (var destino in new[] { cenario.CuidadorEmail, cenario.OutroCuidadorEmail, cenario.FamiliarEmail })
        {
            Assert.Contains(this.email.Tentativas, t => t.Destino == destino && t.Aceite);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.fabricaComEmail.Dispose();
    }

    private static async Task<IEnumerable<string?>> CamposAsync(HttpResponseMessage resposta)
    {
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        return corpo.GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
    }

    private static async Task<Guid> RegistarAsync(HttpClient cliente, Guid fichaId)
    {
        var dia = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var pedido = new
        {
            medicamento = "Insulina",
            dose = "10 UI",
            grauImportancia = "Grave",
            dataInicio = dia,
            dataFim = dia,
            horarios = AsOito,
        };
        using var resposta = await cliente.PostAsJsonAsync($"api/v1/fichas/{fichaId}/tratamentos", pedido);
        resposta.EnsureSuccessStatusCode();
        return (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private string Dia(DateTime instanteUtc)
    {
        return this.calendario.DiaLocal(instanteUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private Task<Guid> NovaTomaAsync(Guid tratamentoId, DateTime previstaUtc, GrauImportancia grau)
    {
        return this.fabrica.ComContextoAsync(async contexto =>
        {
            var toma = new Toma(tratamentoId, previstaUtc, "Insulina", "10 UI", grau);
            contexto.Tomas.Add(toma);
            await contexto.SaveChangesAsync();
            return toma.Id;
        });
    }

    private Task<List<Notificacao>> NotificacoesDaTomaAsync(Guid tomaId)
    {
        return this.fabrica.ComContextoAsync(c => c.Notificacoes.AsNoTracking().Where(n => n.ReferenciaId == tomaId).ToListAsync());
    }

    private async Task NotificarAsync(DateTime agoraUtc)
    {
        await using var ambito = this.fabricaComEmail.Services.CreateAsyncScope();
        var atrasos = ambito.ServiceProvider.GetRequiredService<AtrasoTomaService>();
        await atrasos.NotificarCuidadoresAsync(agoraUtc, CancellationToken.None);
        await atrasos.NotificarFamiliaresAsync(agoraUtc, CancellationToken.None);
    }

    private async Task ProcessarEmailsAsync(DateTime agoraUtc)
    {
        await using var ambito = this.fabricaComEmail.Services.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<EnvioEmailService>().ProcessarPendentesAsync(agoraUtc, CancellationToken.None);
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
        var tratamentoId = await RegistarAsync(cuidador.Cliente, fichaId);
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
            fichaId,
            tratamentoId);
    }

    /// <summary>
    /// Ficha de um Cuidador principal com outro Cuidador, um Familiar e um tratamento, e um Utilizador sem associação.
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
    /// <param name="TratamentoId">Identificador do tratamento.</param>
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
        Guid FichaId,
        Guid TratamentoId) : IDisposable
    {
        public void Dispose()
        {
            this.Cuidador.Dispose();
            this.Familiar.Dispose();
            this.Estranho.Dispose();
        }
    }
}
