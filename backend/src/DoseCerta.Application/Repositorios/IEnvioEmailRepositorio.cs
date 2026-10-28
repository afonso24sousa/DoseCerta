// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Emails;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos e-mails a enviar (EnvioEmailRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface IEnvioEmailRepositorio
{
    /// <summary>
    /// Regista um e-mail a enviar, na transação em curso.
    /// </summary>
    /// <param name="envio">E-mail a enviar.</param>
    void Adicionar(EnvioEmail envio);

    /// <summary>
    /// Lista os e-mails pendentes, de convites e de notificações, cuja próxima tentativa já chegou, do mais antigo para o mais
    /// recente (<c>IX_EnvioEmail_PendenteProxima</c>).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os e-mails devidos, para atualizar.</returns>
    Task<IReadOnlyList<EnvioEmail>> PendentesDevidosAsync(DateTime agoraUtc, CancellationToken cancelamento);

    /// <summary>
    /// Lista as notificações que ainda não têm e-mail registado, da mais antiga para a mais recente, com o e-mail atual do
    /// destinatário; por exemplo, as criadas por outras funcionalidades só com a notificação na aplicação (RF-141).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notificações sem e-mail.</returns>
    Task<IReadOnlyList<NotificacaoSemEmail>> NotificacoesSemEmailAsync(CancellationToken cancelamento);

    /// <summary>
    /// Obtém a notificação de origem de um e-mail, para o corpo do e-mail.
    /// </summary>
    /// <param name="notificacaoId">Identificador da notificação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A notificação, ou <see langword="null"/> se não existir.</returns>
    Task<Notificacao?> ObterNotificacaoAsync(Guid notificacaoId, CancellationToken cancelamento);
}
