// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Entidades;

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

    /// <summary>
    /// Regista uma notificação nova, na transação em curso.
    /// </summary>
    /// <param name="notificacao">Notificação a registar.</param>
    void Adicionar(Notificacao notificacao);

    /// <summary>
    /// Lista as notificações recebidas por um Utilizador, da mais recente para a mais antiga (OP-51, RF-140,
    /// <c>IX_Notificacao_DestinatarioData</c>), com a ficha, a toma de origem e o estado do e-mail (RF-119).
    /// </summary>
    /// <param name="utilizadorId">Destinatário.</param>
    /// <param name="deUtc">Início do intervalo de criação, inclusive, em UTC; opcional.</param>
    /// <param name="ateUtc">Fim do intervalo de criação, exclusive, em UTC; opcional.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notificações do Utilizador.</returns>
    Task<IReadOnlyList<ItemNotificacao>> DoUtilizadorAsync(Guid utilizadorId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento);
}
