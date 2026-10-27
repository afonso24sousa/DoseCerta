// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Processos;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários do <see cref="ProcessoPeriodico"/> (Modelo de Classes do Backend, secção 8.1; passos 1 e 5 das tomas,
/// RF-060 e RF-068, e passo dos e-mails, RF-136).
/// </summary>
public class ProcessoPeriodicoTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;
    private static readonly TimeOnly[] AsOito = { new(8, 0) };

    /// <summary>
    /// Um ciclo processa os e-mails devidos através do <c>EnvioEmailService</c> do âmbito.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloEnviaOsEmailsDevidos()
    {
        var repositorios = new RepositoriosEmMemoria();
        var email = new EmailSenderFalso();
        var relogio = new RelogioFixo(Agora);
        var envioEmail = ServicosDeTeste.EnvioEmailService(repositorios, new UnidadeDeTrabalhoEmMemoria(), relogio, email);
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Agora);
        repositorios.Utilizadores.Add(cuidador);
        var ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        repositorios.Fichas.Add(ficha);
        var convite = new Convite(ficha.Id, "rui@exemplo.pt", Papel.Familiar, false, cuidador.Id, Agora);
        repositorios.Convites.Add(convite);
        var envio = envioEmail.RegistarDeConvite(convite, null, Agora);
        using var servicos = new ServiceCollection().AddSingleton(envioEmail).BuildServiceProvider();
        var processo = Novo(servicos, relogio, ativo: true);

        await processo.ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Single(email.Enviados);
    }

    /// <summary>
    /// Um ciclo prolonga a janela móvel das tomas (passo 1) e passa a não confirmada a toma pendente com 24 horas (passo 5, TR-06).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloProlongaAJanelaEConcluiAsTomasNaoConfirmadas()
    {
        var repositorios = new RepositoriosEmMemoria();
        var relogio = new RelogioFixo(Agora.AddDays(2));
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Agora);
        repositorios.Utilizadores.Add(cuidador);
        var ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        repositorios.Fichas.Add(ficha);
        var tratamento = new Tratamento(
            ficha.Id, "Metformina", "500 mg", GrauImportancia.Moderado, new DateOnly(2026, 10, 21), null, AsOito, cuidador.Id, Agora);
        repositorios.Tratamentos.Add(tratamento);
        var antiga = new Toma(tratamento.Id, relogio.Agora.AddHours(-25), "Metformina", "500 mg", GrauImportancia.Moderado);
        repositorios.Tomas.Add(antiga);
        var tomaService = new TomaService(
            repositorios,
            repositorios,
            repositorios,
            repositorios,
            new AutorizacaoFichaService(repositorios),
            new UnidadeDeTrabalhoEmMemoria(),
            relogio);
        using var servicos = new ServiceCollection().AddSingleton(tomaService).BuildServiceProvider();
        var processo = Novo(servicos, relogio, ativo: true);

        await processo.ExecutarCicloAsync(CancellationToken.None);

        // 08:00 de Lisboa de 24 de outubro a 22 de novembro: a janela de 30 dias a contar de 23 de outubro, 09:00 UTC.
        Assert.Equal(EstadoToma.NaoConfirmada, antiga.Estado);
        Assert.Equal(31, repositorios.Tomas.Count);
        Assert.All(repositorios.Tomas.Where(t => t != antiga), t => Assert.Equal(EstadoToma.Pendente, t.Estado));
    }

    /// <summary>
    /// A falha de um passo fica registada e não sai do ciclo, para o processo continuar nos ciclos seguintes.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDeUmPassoNaoParaOProcesso()
    {
        using var servicos = new ServiceCollection().BuildServiceProvider();
        var processo = Novo(servicos, new RelogioFixo(Agora), ativo: true);

        var erro = await Record.ExceptionAsync(() => processo.ExecutarCicloAsync(CancellationToken.None));

        Assert.Null(erro);
    }

    /// <summary>
    /// Desligado pela configuração (testes de integração), o processo termina logo no arranque, sem ciclos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DesligadoTerminaNoArranque()
    {
        using var servicos = new ServiceCollection().BuildServiceProvider();
        using var processo = Novo(servicos, new RelogioFixo(Agora), ativo: false);

        await processo.StartAsync(CancellationToken.None);

        Assert.True(processo.ExecuteTask!.IsCompletedSuccessfully);
        await processo.StopAsync(CancellationToken.None);
    }

    private static ProcessoPeriodico Novo(ServiceProvider servicos, RelogioFixo relogio, bool ativo)
    {
        return new ProcessoPeriodico(
            servicos.GetRequiredService<IServiceScopeFactory>(),
            relogio,
            Options.Create(new OpcoesProcessoPeriodico { Ativo = ativo }),
            NullLogger<ProcessoPeriodico>.Instance);
    }
}
