// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Ocorrencias;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das ocorrências. A imutabilidade é garantida também pelo gatilho <c>TR_Ocorrencia_Imutavel</c>.
/// </summary>
public sealed class OcorrenciaRepositorio : IOcorrenciaRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcorrenciaRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public OcorrenciaRepositorio(DoseCertaDbContext contexto)
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
        var consulta = this.contexto.Ocorrencias.AsNoTracking().Where(o => o.FichaId == fichaId);
        if (deUtc is not null)
        {
            consulta = consulta.Where(o => o.RegistadaEmUtc >= deUtc);
        }

        if (ateUtc is not null)
        {
            consulta = consulta.Where(o => o.RegistadaEmUtc < ateUtc);
        }

        return await consulta
            .Join(this.contexto.Utilizadores, o => o.AutorId, u => u.Id, (o, u) => new { Ocorrencia = o, Autor = u })
            .OrderByDescending(l => l.Ocorrencia.RegistadaEmUtc)
            .ThenBy(l => l.Ocorrencia.Id)
            .Select(l => new RegistoPartilhado(
                l.Ocorrencia.Id,
                l.Ocorrencia.FichaId,
                TipoRegisto.Ocorrencia,
                l.Ocorrencia.Descricao,
                l.Ocorrencia.AutorId,
                l.Autor.Nome,
                l.Ocorrencia.OcorreuEmUtc,
                l.Ocorrencia.RegistadaEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Adicionar(Ocorrencia ocorrencia)
    {
        this.contexto.Ocorrencias.Add(ocorrencia);
    }
}
