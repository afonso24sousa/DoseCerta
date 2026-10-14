// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.Infrastructure.Tempo;

/// <summary>
/// Relógio do servidor, com o fuso Europe/Lisbon.
/// </summary>
public sealed class RelogioSistema : IRelogio
{
    private readonly TimeProvider fornecedorTempo;
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RelogioSistema"/> class.
    /// </summary>
    /// <param name="fornecedorTempo">Fornecedor do tempo do sistema.</param>
    public RelogioSistema(TimeProvider fornecedorTempo)
    {
        this.fornecedorTempo = fornecedorTempo;
    }

    /// <inheritdoc/>
    public DateTime AgoraUtc()
    {
        var agora = this.fornecedorTempo.GetUtcNow().UtcDateTime;

        // Os instantes são guardados em datetime2(0): a precisão é de um segundo.
        return new DateTime(agora.Ticks - (agora.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public CalendarioLisboa Calendario()
    {
        return this.calendario;
    }
}
