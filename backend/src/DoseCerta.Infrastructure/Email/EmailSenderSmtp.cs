// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Mail;
using DoseCerta.Application.Abstracoes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoseCerta.Infrastructure.Email;

/// <summary>
/// Implementação de <see cref="IEmailSender"/> sobre o SMTP do serviço de e-mail transacional
/// (classe <c>EmailSenderTransacional</c> do Modelo de Classes do Backend). Uma falha nunca é propagada:
/// fica registada no log e o método devolve <see langword="false"/>, deixando a decisão ao chamador.
/// </summary>
public sealed partial class EmailSenderSmtp : IEmailSender
{
    private readonly OpcoesEmail opcoes;
    private readonly ILogger<EmailSenderSmtp> registo;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailSenderSmtp"/> class.
    /// </summary>
    /// <param name="opcoes">Configuração do serviço de e-mail.</param>
    /// <param name="registo">Registo de eventos.</param>
    public EmailSenderSmtp(IOptions<OpcoesEmail> opcoes, ILogger<EmailSenderSmtp> registo)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        this.opcoes = opcoes.Value;
        this.registo = registo;
    }

    /// <inheritdoc/>
    public async Task<bool> EnviarAsync(string destino, string assunto, string corpo, CancellationToken cancelamento)
    {
        if (!this.opcoes.Configurado)
        {
            this.RegistarSemConfiguracao();
            return false;
        }

        try
        {
            using var mensagem = new MailMessage
            {
                From = new MailAddress(this.opcoes.Remetente, this.opcoes.NomeRemetente),
                Subject = assunto,
                Body = corpo,
                IsBodyHtml = false,
            };
            mensagem.To.Add(new MailAddress(destino));

            using var cliente = new SmtpClient(this.opcoes.Servidor, this.opcoes.Porta)
            {
                EnableSsl = this.opcoes.UsarTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            if (!string.IsNullOrEmpty(this.opcoes.Utilizador))
            {
                cliente.Credentials = new NetworkCredential(this.opcoes.Utilizador, this.opcoes.PalavraPasse);
            }

            await cliente.SendMailAsync(mensagem, cancelamento).ConfigureAwait(false);
            this.RegistarEnvio(assunto);
            return true;
        }
        catch (Exception erro) when (erro is SmtpException or InvalidOperationException or FormatException or IOException)
        {
            this.RegistarFalha(assunto, erro);
            return false;
        }
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Envio de e-mail sem configuração SMTP: o e-mail fica por enviar.")]
    private partial void RegistarSemConfiguracao();

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "E-mail aceite pelo serviço de envio: {Assunto}.")]
    private partial void RegistarEnvio(string assunto);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Falha no envio de e-mail: {Assunto}.")]
    private partial void RegistarFalha(string assunto, Exception erro);
}
