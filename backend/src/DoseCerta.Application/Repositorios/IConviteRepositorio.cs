// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
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
    /// Lista os convites pendentes dirigidos a um e-mail, sem distinguir maiúsculas, com o nome da ficha e de quem convidou,
    /// do mais recente para o mais antigo (RF-042, RF-044, RF-135).
    /// </summary>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os convites pendentes.</returns>
    Task<IReadOnlyList<ConviteResumo>> PendentesPorEmailAsync(string email, CancellationToken cancelamento);

    /// <summary>
    /// Obtém um convite pelo identificador, para responder a ele.
    /// </summary>
    /// <param name="id">Identificador do convite.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O convite, ou <see langword="null"/> se não existir.</returns>
    Task<Convite?> ObterAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Regista um convite novo.
    /// </summary>
    /// <param name="convite">Convite a registar.</param>
    void Adicionar(Convite convite);
}
