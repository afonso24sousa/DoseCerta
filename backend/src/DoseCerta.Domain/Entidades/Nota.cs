// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Nota dirigida aos Cuidadores da ficha; depois de registada não se altera nem se elimina (RF-091, RF-095).
/// </summary>
public class Nota
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Nota"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="texto">Texto.</param>
    /// <param name="autorId">Autor.</param>
    /// <param name="registadaEmUtc">Instante do registo, em UTC.</param>
    public Nota(
        Guid fichaId,
        string texto,
        Guid autorId,
        DateTime registadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.Texto = texto;
        this.AutorId = autorId;
        this.RegistadaEmUtc = registadaEmUtc;
    }

    private Nota()
    {
    }

    /// <summary>
    /// Gets o identificador da nota.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o texto.
    /// </summary>
    public string Texto { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o identificador do autor.
    /// </summary>
    public Guid AutorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime RegistadaEmUtc { get; private set; }
}
