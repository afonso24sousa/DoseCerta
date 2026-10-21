// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos convites (ConviteRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface IConviteRepositorio
{
    /// <summary>
    /// Conta os convites pendentes dirigidos a um e-mail (RF-044).
    /// </summary>
    /// <param name="email">E-mail convidado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de convites pendentes.</returns>
    Task<int> ContarPendentesPorEmailAsync(string email, CancellationToken cancelamento);

    /// <summary>
    /// Indica se já existe um convite pendente para o e-mail na ficha, sem distinguir maiúsculas (RF-043, <c>UQ_Convite_Pendente</c>).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="email">E-mail convidado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se existir um convite pendente.</returns>
    Task<bool> ExistePendenteAsync(Guid fichaId, string email, CancellationToken cancelamento);

    /// <summary>
    /// Regista um convite novo.
    /// </summary>
    /// <param name="convite">Convite a registar.</param>
    void Adicionar(Convite convite);
}
