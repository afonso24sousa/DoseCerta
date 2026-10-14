// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DoseCerta.Api.Saude;

/// <summary>
/// Verificação do estado da aplicação: confirma que a API consegue ligar ao SQL Server.
/// </summary>
public sealed class VerificacaoBaseDados : IHealthCheck
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificacaoBaseDados"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public VerificacaoBaseDados(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var liga = await this.contexto.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return liga
            ? HealthCheckResult.Healthy("Base de dados acessível.")
            : HealthCheckResult.Unhealthy("Sem ligação à base de dados.");
    }
}
