// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Processos;
using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários dos passos 2, 3 e 6 do <see cref="ProcessoPeriodico"/>: notificações de atraso das tomas aos Cuidadores e, no
/// grau Grave, aos Familiares (RF-110 a RF-113, RNF-042), e aviso antes de um compromisso (I056, RF-117).
/// </summary>
public class ProcessoPeriodicoNotificacoesTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;

    /// <summary>
    /// RNF-042: um ciclo depois de uma toma Grave ter passado os 60 minutos sem confirmação cria, pela ordem dos instantes-limite,
    /// as notificações aos Cuidadores (passo 2) e aos Familiares (passo 3) através do <c>AtrasoTomaService</c> do âmbito.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloNotificaCuidadoresEFamiliaresDeTomaGrave()
    {
        var repositorios = new RepositoriosEmMemoria();
        var relogio = new RelogioFixo(Agora.AddHours(2));
        var unidadeDeTrabalho = new UnidadeDeTrabalhoEmMemoria();
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Agora);
        var familiar = new Utilizador("Rui Familiar", "rui@exemplo.pt", "hash-ficticio", Agora);
        repositorios.Utilizadores.AddRange(new[] { cuidador, familiar });
        var ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        ficha.Associar(familiar.Id, Papel.Familiar, Agora);
        repositorios.Fichas.Add(ficha);
        var tratamento = new Tratamento(
            ficha.Id, "Insulina", "10 UI", GrauImportancia.Grave, new DateOnly(2026, 10, 21), null, new[] { new TimeOnly(11, 0) }, cuidador.Id, Agora);
        repositorios.Tratamentos.Add(tratamento);
        var toma = new Toma(tratamento.Id, Agora.AddMinutes(30), "Insulina", "10 UI", GrauImportancia.Grave);
        repositorios.Tomas.Add(toma);
        var envioEmail = ServicosDeTeste.EnvioEmailService(repositorios, unidadeDeTrabalho, relogio, new EmailSenderFalso());
        var notificacoes = new NotificacaoService(repositorios, repositorios, envioEmail, relogio);
        var atrasos = new AtrasoTomaService(repositorios, repositorios, repositorios, notificacoes, unidadeDeTrabalho, relogio);
        using var servicos = new ServiceCollection().AddSingleton(atrasos).BuildServiceProvider();
        var processo = new ProcessoPeriodico(
            servicos.GetRequiredService<IServiceScopeFactory>(),
            relogio,
            Options.Create(new OpcoesProcessoPeriodico { Ativo = true }),
            NullLogger<ProcessoPeriodico>.Instance);

        await processo.ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(cuidador.Id, Assert.Single(repositorios.Notificacoes, n => n.Tipo == TipoNotificacao.TomaEmAtraso).DestinatarioId);
        Assert.Equal(familiar.Id, Assert.Single(repositorios.Notificacoes, n => n.Tipo == TipoNotificacao.TomaGraveFamiliares).DestinatarioId);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal(2, repositorios.EnviosEmail.Count);
    }

    /// <summary>
    /// I056 (RF-117, RF-118, RNF-040): o passo 6 avisa o compromisso cujo instante do aviso chegou, através do
    /// <c>AvisoCompromissoService</c> do âmbito, e o passo 7 envia os e-mails no mesmo ciclo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloAvisaOCompromissoEEnviaOsEmails()
    {
        var repositorios = new RepositoriosEmMemoria();
        var relogio = new RelogioFixo(Agora.AddHours(2));
        var unidadeDeTrabalho = new UnidadeDeTrabalhoEmMemoria();
        var email = new EmailSenderFalso();
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Agora);
        var familiar = new Utilizador("Rui Familiar", "rui@exemplo.pt", "hash-ficticio", Agora);
        repositorios.Utilizadores.AddRange(new[] { cuidador, familiar });
        var ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        ficha.Associar(familiar.Id, Papel.Familiar, Agora);
        repositorios.Fichas.Add(ficha);
        var compromisso = new Compromisso(ficha.Id, Agora.AddHours(2).AddMinutes(20), "Centro de Saúde Fictício", "Consulta", 30, cuidador.Id, Agora);
        repositorios.Compromissos.Add(compromisso);
        var envioEmail = ServicosDeTeste.EnvioEmailService(repositorios, unidadeDeTrabalho, relogio, email);
        var notificacoes = new NotificacaoService(repositorios, repositorios, envioEmail, relogio);
        var avisos = new AvisoCompromissoService(repositorios, repositorios, notificacoes, unidadeDeTrabalho, relogio);
        using var servicos = new ServiceCollection().AddSingleton(avisos).AddSingleton(envioEmail).BuildServiceProvider();

        await NovoProcesso(servicos, relogio).ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(2, repositorios.Notificacoes.Count(n => n.Tipo == TipoNotificacao.ProximoCompromisso));
        Assert.Equal(Agora.AddHours(2), compromisso.AvisoEnviadoEmUtc);
        Assert.Equal(2, email.Enviados.Count);
        Assert.All(email.Enviados, e => Assert.Equal(TextosNotificacao.AssuntoProximoCompromisso, e.Assunto));
    }

    /// <summary>
    /// I056: a falha do passo 6 fica registada e não impede o passo 7 de enviar os e-mails devidos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDoAvisoDeCompromissoNaoParaOEnvioDosEmails()
    {
        var repositorios = new RepositoriosEmMemoria();
        var relogio = new RelogioFixo(Agora);
        var email = new EmailSenderFalso();
        var envioEmail = ServicosDeTeste.EnvioEmailService(repositorios, new UnidadeDeTrabalhoEmMemoria(), relogio, email);
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Agora);
        repositorios.Utilizadores.Add(cuidador);
        var ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        repositorios.Fichas.Add(ficha);
        var convite = new Convite(ficha.Id, "rui@exemplo.pt", Papel.Familiar, false, cuidador.Id, Agora);
        repositorios.Convites.Add(convite);
        var envio = envioEmail.RegistarDeConvite(convite, null, Agora);

        // Sem AvisoCompromissoService no âmbito, o passo 6 falha.
        using var servicos = new ServiceCollection().AddSingleton(envioEmail).BuildServiceProvider();

        await NovoProcesso(servicos, relogio).ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
    }

    private static ProcessoPeriodico NovoProcesso(ServiceProvider servicos, RelogioFixo relogio)
    {
        return new ProcessoPeriodico(
            servicos.GetRequiredService<IServiceScopeFactory>(),
            relogio,
            Options.Create(new OpcoesProcessoPeriodico { Ativo = true }),
            NullLogger<ProcessoPeriodico>.Instance);
    }
}
