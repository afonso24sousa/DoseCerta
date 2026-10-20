// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos metadados dos documentos; os ficheiros vivem no sistema de ficheiros do servidor (AD-06).
/// </summary>
public sealed class DocumentoRepositorio : IDocumentoRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentoRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public DocumentoRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Documento>> DaFichaAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return await this.contexto.Documentos
            .AsNoTracking()
            .Where(d => d.FichaId == fichaId)
            .OrderByDescending(d => d.CarregadoEmUtc)
            .ThenBy(d => d.NomeOriginal)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Adicionar(Documento documento)
    {
        this.contexto.Documentos.Add(documento);
    }
}
