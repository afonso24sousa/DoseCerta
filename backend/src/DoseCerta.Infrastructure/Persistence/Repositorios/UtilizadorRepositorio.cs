// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das contas. A igualdade de e-mail usa a collation CI_AI da coluna (RF-011).
/// </summary>
public sealed class UtilizadorRepositorio : IUtilizadorRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="UtilizadorRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public UtilizadorRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<Utilizador?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Utilizadores.FirstOrDefaultAsync(u => u.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken cancelamento)
    {
        return this.contexto.Utilizadores.FirstOrDefaultAsync(u => u.Email == email, cancelamento);
    }

    /// <inheritdoc/>
    public Task<bool> ExisteEmailAsync(string email, Guid? excetoId, CancellationToken cancelamento)
    {
        return this.contexto.Utilizadores.AnyAsync(u => u.Email == email && (excetoId == null || u.Id != excetoId), cancelamento);
    }

    /// <inheritdoc/>
    public void Adicionar(Utilizador utilizador)
    {
        this.contexto.Utilizadores.Add(utilizador);
    }
}
