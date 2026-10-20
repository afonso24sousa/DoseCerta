// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às notificações (NotificacaoRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface INotificacaoRepositorio
{
    /// <summary>
    /// Apaga as notificações de uma ficha, antes de a eliminar (não existe cascata de Ficha para Notificacao, RF-123).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de notificações apagadas.</returns>
    Task<int> EliminarDaFichaAsync(Guid fichaId, CancellationToken cancelamento);
}
