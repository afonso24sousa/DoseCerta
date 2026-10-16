// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Relógio fixo para os testes (RNF-047): o instante só muda quando o teste o avança.
/// </summary>
public sealed class RelogioFixo : IRelogio
{
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RelogioFixo"/> class.
    /// </summary>
    /// <param name="agoraUtc">Instante inicial, em UTC.</param>
    public RelogioFixo(DateTime agoraUtc)
    {
        this.Agora = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets or sets o instante atual do relógio, em UTC.
    /// </summary>
    public DateTime Agora { get; set; }

    /// <inheritdoc/>
    public DateTime AgoraUtc()
    {
        return this.Agora;
    }

    /// <inheritdoc/>
    public CalendarioLisboa Calendario()
    {
        return this.calendario;
    }
}
