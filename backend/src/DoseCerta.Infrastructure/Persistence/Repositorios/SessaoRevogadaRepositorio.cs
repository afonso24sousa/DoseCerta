// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das credenciais terminadas.
/// </summary>
public sealed class SessaoRevogadaRepositorio : ISessaoRevogadaRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessaoRevogadaRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public SessaoRevogadaRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<bool> EstaRevogadaAsync(Guid jti, CancellationToken cancelamento)
    {
        return this.contexto.SessoesRevogadas.AnyAsync(s => s.Jti == jti, cancelamento);
    }

    /// <inheritdoc/>
    public void Adicionar(SessaoRevogada sessao)
    {
        this.contexto.SessoesRevogadas.Add(sessao);
    }

    /// <inheritdoc/>
    public Task<int> EliminarExpiradasAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        return this.contexto.SessoesRevogadas.Where(s => s.ExpiraEmUtc <= agoraUtc).ExecuteDeleteAsync(cancelamento);
    }
}
