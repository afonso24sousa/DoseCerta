// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Credencial JWT terminada antes de expirar, que deixa de ser aceite (RF-016, RF-017).
/// </summary>
public class SessaoRevogada
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessaoRevogada"/> class.
    /// </summary>
    /// <param name="jti">Identificador da credencial.</param>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="revogadaEmUtc">Instante do término, em UTC.</param>
    /// <param name="expiraEmUtc">Instante de expiração do token, em UTC.</param>
    public SessaoRevogada(
        Guid jti,
        Guid utilizadorId,
        DateTime revogadaEmUtc,
        DateTime expiraEmUtc)
    {
        this.Jti = jti;
        this.UtilizadorId = utilizadorId;
        this.RevogadaEmUtc = revogadaEmUtc;
        this.ExpiraEmUtc = expiraEmUtc;
    }

    private SessaoRevogada()
    {
    }

    /// <summary>
    /// Gets o identificador da credencial (claim jti).
    /// </summary>
    public Guid Jti { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador.
    /// </summary>
    public Guid UtilizadorId { get; private set; }

    /// <summary>
    /// Gets o instante do término da sessão, em UTC.
    /// </summary>
    public DateTime RevogadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante de expiração do token, em UTC.
    /// </summary>
    public DateTime ExpiraEmUtc { get; private set; }
}
