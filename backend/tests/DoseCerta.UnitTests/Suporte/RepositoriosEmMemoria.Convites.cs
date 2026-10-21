// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Consultas em memória dos convites usadas pela associação de pessoas (F004).
/// </summary>
public partial class RepositoriosEmMemoria
{
    /// <inheritdoc/>
    public Task<bool> ExistePendenteAsync(Guid fichaId, string email, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Convites.Exists(c => c.FichaId == fichaId && MesmoEmail(c.EmailConvidado, email) && c.Estado == EstadoConvite.Pendente));
    }
}
