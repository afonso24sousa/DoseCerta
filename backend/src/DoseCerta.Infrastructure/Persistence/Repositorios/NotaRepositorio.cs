// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Ocorrencias;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das notas partilhadas. A imutabilidade é garantida também pelo gatilho <c>TR_Nota_Imutavel</c>.
/// </summary>
public sealed class NotaRepositorio : INotaRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotaRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public NotaRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RegistoPartilhado>> DaFichaAsync(
        Guid fichaId,
        DateTime? deUtc,
        DateTime? ateUtc,
        CancellationToken cancelamento)
    {
        var consulta = this.contexto.Notas.AsNoTracking().Where(n => n.FichaId == fichaId);
        if (deUtc is not null)
        {
            consulta = consulta.Where(n => n.RegistadaEmUtc >= deUtc);
        }

        if (ateUtc is not null)
        {
            consulta = consulta.Where(n => n.RegistadaEmUtc < ateUtc);
        }

        return await consulta
            .Join(this.contexto.Utilizadores, n => n.AutorId, u => u.Id, (n, u) => new { Nota = n, Autor = u })
            .OrderByDescending(l => l.Nota.RegistadaEmUtc)
            .ThenBy(l => l.Nota.Id)
            .Select(l => new RegistoPartilhado(
                l.Nota.Id,
                l.Nota.FichaId,
                TipoRegisto.Nota,
                l.Nota.Texto,
                l.Nota.AutorId,
                l.Autor.Nome,
                null,
                l.Nota.RegistadaEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Adicionar(Nota nota)
    {
        this.contexto.Notas.Add(nota);
    }
}
