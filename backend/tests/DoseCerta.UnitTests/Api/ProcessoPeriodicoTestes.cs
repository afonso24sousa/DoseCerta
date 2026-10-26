// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Processos;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Application.Tratamentos;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários do <see cref="ProcessoPeriodico"/> (Modelo de Classes do Backend, secção 8; passo 7, RF-136).
/// </summary>
public class ProcessoPeriodicoTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;

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
    /// Passos 4 e 7 (RF-056, RF-016): o ciclo termina o tratamento cuja data de fim já passou e elimina as credenciais
    /// terminadas que já expiraram, mantendo as que ainda estão dentro da validade.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CicloTerminaTratamentosConcluidosEEliminaCredenciaisExpiradas()
    {
        var repositorios = new RepositoriosEmMemoria();
        var relogio = new RelogioFixo(Agora);
        var unidadeDeTrabalho = new UnidadeDeTrabalhoEmMemoria();
        var autorizacao = new AutorizacaoFichaService(repositorios);
        var documentos = new DocumentoService(repositorios, new ArmazenamentoEmMemoria(), autorizacao, unidadeDeTrabalho, relogio);
        var tratamentos = new TratamentoService(repositorios, documentos, new TomaService(repositorios, repositorios, relogio), autorizacao, unidadeDeTrabalho, relogio);
        var ontem = DateOnly.FromDateTime(Agora).AddDays(-1);
        var concluido = new Tratamento(Guid.NewGuid(), "Metformina", "500 mg", GrauImportancia.Leve, ontem.AddDays(-5), ontem, new[] { new TimeOnly(8, 0) }, Guid.NewGuid(), Agora.AddDays(-6));
        repositorios.Tratamentos.Add(concluido);
        var expirada = new SessaoRevogada(Guid.NewGuid(), Guid.NewGuid(), Agora.AddHours(-9), Agora.AddHours(-1));
        var valida = new SessaoRevogada(Guid.NewGuid(), Guid.NewGuid(), Agora.AddHours(-1), Agora.AddHours(7));
        repositorios.Sessoes.AddRange(new[] { expirada, valida });
        using var servicos = new ServiceCollection()
            .AddSingleton(tratamentos)
            .AddSingleton<ISessaoRevogadaRepositorio>(repositorios)
            .BuildServiceProvider();
        var processo = Novo(servicos, relogio, ativo: true);

        await processo.ExecutarCicloAsync(CancellationToken.None);

        Assert.Equal(EstadoTratamento.Terminado, concluido.Estado);
        Assert.Same(valida, Assert.Single(repositorios.Sessoes));
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
