// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos convites. A igualdade de e-mail usa a collation CI_AI da coluna.
/// </summary>
public sealed class ConviteRepositorio : IConviteRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConviteRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public ConviteRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<int> ContarPendentesPorEmailAsync(string email, CancellationToken cancelamento)
    {
        return this.contexto.Convites.CountAsync(c => c.EmailConvidado == email && c.Estado == EstadoConvite.Pendente, cancelamento);
    }
}
