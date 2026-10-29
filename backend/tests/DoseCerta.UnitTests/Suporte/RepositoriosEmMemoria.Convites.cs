// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
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

    /// <inheritdoc/>
    Task<Convite?> IConviteRepositorio.ObterAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Convites.Find(c => c.Id == id));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConviteResumo>> PendentesPorEmailAsync(string email, CancellationToken cancelamento)
    {
        IReadOnlyList<ConviteResumo> resumos = this.Resumos(c => MesmoEmail(c.EmailConvidado, email) && c.Estado == EstadoConvite.Pendente)
            .OrderByDescending(r => r.CriadoEmUtc)
            .ThenBy(r => r.Id)
            .ToList();
        return Task.FromResult(resumos);
    }

    /// <inheritdoc/>
    public Task<ConviteResumo?> ObterResumoAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Resumos(c => c.Id == id).FirstOrDefault());
    }

    private IEnumerable<ConviteResumo> Resumos(Func<Convite, bool> filtro)
    {
        return this.Convites
            .Where(filtro)
            .Join(this.Fichas, c => c.FichaId, f => f.Id, (c, f) => (Convite: c, Ficha: f))
            .Join(this.Utilizadores, p => p.Convite.ConvidadoPorId, u => u.Id, (p, u) => new ConviteResumo(
                p.Convite.Id,
                p.Convite.FichaId,
                p.Ficha.NomeCompleto,
                p.Convite.PapelOferecido,
                p.Convite.ParaPrincipal,
                u.Nome,
                p.Convite.Estado,
                p.Convite.CriadoEmUtc));
    }
}
