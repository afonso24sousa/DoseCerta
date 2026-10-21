// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

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
    /// Lista os e-mails de convite pendentes cuja próxima tentativa já chegou, do mais antigo para o mais recente
    /// (<c>IX_EnvioEmail_PendenteProxima</c>). Os e-mails das restantes notificações são da F011.
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os e-mails devidos, para atualizar.</returns>
    Task<IReadOnlyList<EnvioEmail>> PendentesDevidosAsync(DateTime agoraUtc, CancellationToken cancelamento);
}
