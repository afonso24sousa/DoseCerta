// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Emails;
using Microsoft.Extensions.Options;

namespace DoseCerta.Api.Processos;

/// <summary>
/// Processo periódico do backend, dentro do processo da API: um ciclo no arranque e depois de 30 em 30 segundos
/// (Modelo de Classes do Backend, secção 8). Nesta tarefa (F004) só tem o passo 7, o envio e os reenvios dos e-mails de convite;
/// os passos das tomas, das tarefas, dos compromissos, dos tratamentos e das sessões são acrescentados pelas tarefas dessas
/// funcionalidades. O processo não tem regras de negócio: delega nos serviços de aplicação.
/// </summary>
public sealed partial class ProcessoPeriodico : BackgroundService
{
    private readonly IServiceScopeFactory ambitos;
    private readonly IRelogio relogio;
    private readonly OpcoesProcessoPeriodico opcoes;
    private readonly ILogger<ProcessoPeriodico> registo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessoPeriodico"/> class.
    /// </summary>
    /// <param name="ambitos">Fábrica de âmbitos, um por ciclo, para os serviços de aplicação.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    /// <param name="opcoes">Configuração do processo.</param>
    /// <param name="registo">Registo de eventos.</param>
    public ProcessoPeriodico(
        IServiceScopeFactory ambitos,
        IRelogio relogio,
        IOptions<OpcoesProcessoPeriodico> opcoes,
        ILogger<ProcessoPeriodico> registo)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        this.ambitos = ambitos;
        this.relogio = relogio;
        this.opcoes = opcoes.Value;
        this.registo = registo;
    }

    /// <summary>
    /// Executa um ciclo: cada passo corre no seu âmbito e a falha de um passo fica registada sem parar o processo.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task ExecutarCicloAsync(CancellationToken cancelamento)
    {
        try
        {
            await using var ambito = this.ambitos.CreateAsyncScope();
            var envioEmail = ambito.ServiceProvider.GetRequiredService<EnvioEmailService>();
            await envioEmail.ProcessarPendentesAsync(this.relogio.AgoraUtc(), cancelamento).ConfigureAwait(false);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            this.RegistarFalhaDoPasso("EnvioEmailService.ProcessarPendentes", erro);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.opcoes.Ativo)
        {
            this.RegistarDesligado();
            return;
        }

        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(this.opcoes.IntervaloSegundos));
        do
        {
            await this.ExecutarCicloAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await temporizador.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Processo periódico desligado pela configuração.")]
    private partial void RegistarDesligado();

    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "Falha no passo {Passo} do processo periódico.")]
    private partial void RegistarFalhaDoPasso(string passo, Exception erro);
}
