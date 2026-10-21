// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Convites;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Emails;

/// <summary>
/// Regista os e-mails a enviar, separados do convite e da notificação que os originam, e submete-os ao serviço de e-mail com até
/// 3 reenvios de 5 em 5 minutos; a falha nunca desfaz a operação de origem (RF-118, RF-119, RF-135, RF-136, RF-141, DM-13).
/// Nesta tarefa só trata dos e-mails de convite (F004); os e-mails das restantes notificações são da F011.
/// </summary>
public sealed class EnvioEmailService
{
    private readonly IEnvioEmailRepositorio envios;
    private readonly IConviteRepositorio convites;
    private readonly IEmailSender emailSender;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailService"/> class.
    /// </summary>
    /// <param name="envios">Repositório dos e-mails a enviar.</param>
    /// <param name="convites">Repositório dos convites, para o texto do e-mail.</param>
    /// <param name="emailSender">Serviço de e-mail transacional.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada atualização de um e-mail.</param>
    /// <param name="relogio">Relógio do sistema, para as datas em hora de Lisboa.</param>
    public EnvioEmailService(
        IEnvioEmailRepositorio envios,
        IConviteRepositorio convites,
        IEmailSender emailSender,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.envios = envios;
        this.convites = convites;
        this.emailSender = emailSender;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// Regista, na transação do convite, o e-mail de convite como pendente, com a primeira tentativa devida de imediato (RF-041, RNF-018).
    /// </summary>
    /// <param name="convite">Convite criado.</param>
    /// <param name="notificacaoId">Notificação <c>ConviteRecebido</c> do mesmo convite, se o e-mail já tiver conta.</param>
    /// <param name="agoraUtc">Instante da criação, em UTC.</param>
    /// <returns>O e-mail registado.</returns>
    public EnvioEmail RegistarDeConvite(Convite convite, Guid? notificacaoId, DateTime agoraUtc)
    {
        ArgumentNullException.ThrowIfNull(convite);
        var envio = new EnvioEmail(notificacaoId, convite.Id, convite.EmailConvidado, TextosConvite.Assunto, agoraUtc);
        this.envios.Adicionar(envio);
        return envio;
    }

    /// <summary>
    /// Passo 7 do processo periódico: submete cada e-mail pendente cuja próxima tentativa chegou e regista o resultado, cada um
    /// na sua própria transação. Uma falha agenda o reenvio 5 minutos depois; depois de 3 reenvios falhados o e-mail fica
    /// não entregue (RF-118, RF-119, RF-136). Uma exceção do serviço de e-mail conta como falha, para o e-mail não ficar preso.
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de e-mails tentados.</returns>
    public async Task<int> ProcessarPendentesAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var devidos = await this.envios.PendentesDevidosAsync(agoraUtc, cancelamento).ConfigureAwait(false);
        foreach (var envio in devidos)
        {
            var aceite = await this.EnviarAsync(envio, cancelamento).ConfigureAwait(false);
            await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
                () =>
                {
                    envio.IniciarTentativa(agoraUtc);
                    if (aceite)
                    {
                        envio.MarcarEnviado(agoraUtc);
                    }
                    else
                    {
                        envio.RegistarFalha(agoraUtc);
                    }

                    return Task.FromResult(true);
                },
                cancelamento).ConfigureAwait(false);
        }

        return devidos.Count;
    }

    private async Task<bool> EnviarAsync(EnvioEmail envio, CancellationToken cancelamento)
    {
        try
        {
            var corpo = await this.CorpoDoEmailAsync(envio, cancelamento).ConfigureAwait(false);
            return await this.emailSender.EnviarAsync(envio.EnderecoDestino, envio.Assunto, corpo, cancelamento).ConfigureAwait(false);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<string> CorpoDoEmailAsync(EnvioEmail envio, CancellationToken cancelamento)
    {
        var convite = await this.convites.ObterResumoAsync(envio.ConviteId!.Value, cancelamento).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O e-mail de convite não tem convite.");
        return TextosConvite.CorpoEmail(convite, this.relogio.Calendario().FormatarDataHora(convite.CriadoEmUtc));
    }
}
