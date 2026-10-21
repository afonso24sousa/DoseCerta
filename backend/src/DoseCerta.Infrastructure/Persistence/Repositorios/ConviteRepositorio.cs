// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
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

    /// <inheritdoc/>
    public Task<bool> ExistePendenteAsync(Guid fichaId, string email, CancellationToken cancelamento)
    {
        return this.contexto.Convites.AnyAsync(
            c => c.FichaId == fichaId && c.EmailConvidado == email && c.Estado == EstadoConvite.Pendente,
            cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConviteResumo>> PendentesPorEmailAsync(string email, CancellationToken cancelamento)
    {
        return await this.contexto.Convites
            .AsNoTracking()
            .Where(c => c.EmailConvidado == email && c.Estado == EstadoConvite.Pendente)
            .Join(this.contexto.Fichas, c => c.FichaId, f => f.Id, (c, f) => new { Convite = c, Ficha = f })
            .Join(this.contexto.Utilizadores, p => p.Convite.ConvidadoPorId, u => u.Id, (p, u) => new { p.Convite, p.Ficha, QuemConvidou = u })
            .OrderByDescending(p => p.Convite.CriadoEmUtc)
            .ThenBy(p => p.Convite.Id)
            .Select(p => new ConviteResumo(
                p.Convite.Id,
                p.Convite.FichaId,
                p.Ficha.NomeCompleto,
                p.Convite.PapelOferecido,
                p.Convite.ParaPrincipal,
                p.QuemConvidou.Nome,
                p.Convite.Estado,
                p.Convite.CriadoEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<Convite?> ObterAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Convites.FirstOrDefaultAsync(c => c.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public void Adicionar(Convite convite)
    {
        this.contexto.Convites.Add(convite);
    }
}
