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
    /// Regista um convite novo.
    /// </summary>
    /// <param name="convite">Convite a registar.</param>
    void Adicionar(Convite convite);
}
