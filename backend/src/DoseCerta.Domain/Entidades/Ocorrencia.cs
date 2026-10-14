// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Ocorrência do dia a dia da pessoa cuidada; depois de registada não se altera nem se elimina (RF-090, RF-094).
/// </summary>
public class Ocorrencia
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Ocorrencia"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="descricao">Descrição.</param>
    /// <param name="ocorreuEmUtc">Instante em que aconteceu, em UTC.</param>
    /// <param name="autorId">Autor.</param>
    /// <param name="registadaEmUtc">Instante do registo, em UTC.</param>
    public Ocorrencia(
        Guid fichaId,
        string descricao,
        DateTime ocorreuEmUtc,
        Guid autorId,
        DateTime registadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.Descricao = descricao;
        this.OcorreuEmUtc = ocorreuEmUtc;
        this.AutorId = autorId;
        this.RegistadaEmUtc = registadaEmUtc;
    }

    private Ocorrencia()
    {
    }

    /// <summary>
    /// Gets o identificador da ocorrência.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets a descrição.
    /// </summary>
    public string Descricao { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o instante em que aconteceu, em UTC.
    /// </summary>
    public DateTime OcorreuEmUtc { get; private set; }

    /// <summary>
    /// Gets o identificador do autor.
    /// </summary>
    public Guid AutorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime RegistadaEmUtc { get; private set; }
}
