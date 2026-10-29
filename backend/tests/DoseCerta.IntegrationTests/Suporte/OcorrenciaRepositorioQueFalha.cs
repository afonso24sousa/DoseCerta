// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Ocorrencias;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.Infrastructure.Persistence.Repositorios;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Repositório das ocorrências cuja gravação é recusada pela base de dados: o autor passa a um Utilizador inexistente, o que
/// viola <c>FK_Ocorrencia_AutorId</c> ao guardar e faz reverter toda a transação, com a API completa (RNF-046).
/// </summary>
public sealed class OcorrenciaRepositorioQueFalha : IOcorrenciaRepositorio
{
    private readonly DoseCertaDbContext contexto;
    private readonly OcorrenciaRepositorio real;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcorrenciaRepositorioQueFalha"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public OcorrenciaRepositorioQueFalha(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
        this.real = new OcorrenciaRepositorio(contexto);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RegistoPartilhado>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento)
    {
        return this.real.DaFichaAsync(fichaId, deUtc, ateUtc, cancelamento);
    }

    /// <inheritdoc/>
    public void Adicionar(Ocorrencia ocorrencia)
    {
        this.real.Adicionar(ocorrencia);
        this.contexto.Entry(ocorrencia).Property(o => o.AutorId).CurrentValue = Guid.NewGuid();
    }
}
