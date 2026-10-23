// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Linq.Expressions;
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
    private static readonly Expression<Func<LinhaConvite, ConviteResumo>> ParaResumo = l => new ConviteResumo(
        l.Convite.Id,
        l.Convite.FichaId,
        l.Ficha.NomeCompleto,
        l.Convite.PapelOferecido,
        l.Convite.ParaPrincipal,
        l.QuemConvidou.Nome,
        l.Convite.Estado,
        l.Convite.CriadoEmUtc);

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
        return await this.ComFichaEQuemConvidou(this.contexto.Convites.Where(c => c.EmailConvidado == email && c.Estado == EstadoConvite.Pendente))
            .OrderByDescending(l => l.Convite.CriadoEmUtc)
            .ThenBy(l => l.Convite.Id)
            .Select(ParaResumo)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<ConviteResumo?> ObterResumoAsync(Guid id, CancellationToken cancelamento)
    {
        return this.ComFichaEQuemConvidou(this.contexto.Convites.Where(c => c.Id == id)).Select(ParaResumo).FirstOrDefaultAsync(cancelamento);
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

    private IQueryable<LinhaConvite> ComFichaEQuemConvidou(IQueryable<Convite> convites)
    {
        return convites
            .AsNoTracking()
            .Join(this.contexto.Fichas, c => c.FichaId, f => f.Id, (c, f) => new { Convite = c, Ficha = f })
            .Join(
                this.contexto.Utilizadores,
                p => p.Convite.ConvidadoPorId,
                u => u.Id,
                (p, u) => new LinhaConvite { Convite = p.Convite, Ficha = p.Ficha, QuemConvidou = u });
    }

    /// <summary>
    /// Convite com a ficha e a conta de quem convidou, antes da projeção para <see cref="ConviteResumo"/>; as propriedades permitem
    /// ordenar no SQL.
    /// </summary>
    private sealed class LinhaConvite
    {
        public Convite Convite { get; init; } = null!;

        public Ficha Ficha { get; init; } = null!;

        public Utilizador QuemConvidou { get; init; } = null!;
    }
}
