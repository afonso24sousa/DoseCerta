// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Submete um e-mail ao serviço de e-mail transacional, a partir do endereço dedicado ao projeto (RNF-052).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Submete um e-mail.
    /// </summary>
    /// <param name="destino">Endereço de destino.</param>
    /// <param name="assunto">Assunto do e-mail.</param>
    /// <param name="corpo">Corpo do e-mail, em texto simples.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se o serviço aceitou o e-mail; <see langword="false"/> se o envio falhou.</returns>
    Task<bool> EnviarAsync(string destino, string assunto, string corpo, CancellationToken cancelamento);
}
