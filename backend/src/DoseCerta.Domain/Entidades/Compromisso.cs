// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Consulta ou compromisso de saúde, apenas para consulta e sem estado de realização (RF-087). O comportamento é implementado com a F008.
/// </summary>
public class Compromisso
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Compromisso"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="dataHoraUtc">Data e hora, em UTC.</param>
    /// <param name="local">Local.</param>
    /// <param name="motivo">Motivo.</param>
    /// <param name="antecedenciaAvisoMin">Antecedência do aviso, em minutos.</param>
    /// <param name="criadoPorId">Utilizador que regista.</param>
    /// <param name="criadoEmUtc">Instante do registo, em UTC.</param>
    public Compromisso(
        Guid fichaId,
        DateTime dataHoraUtc,
        string local,
        string motivo,
        int antecedenciaAvisoMin,
        Guid criadoPorId,
        DateTime criadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.DataHoraUtc = dataHoraUtc;
        this.Local = local;
        this.Motivo = motivo;
        this.AntecedenciaAvisoMin = antecedenciaAvisoMin;
        this.CriadoPorId = criadoPorId;
        this.CriadoEmUtc = criadoEmUtc;
    }

    private Compromisso()
    {
    }

    /// <summary>
    /// Gets o identificador do compromisso.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets a data e hora, em UTC.
    /// </summary>
    public DateTime DataHoraUtc { get; private set; }

    /// <summary>
    /// Gets o local.
    /// </summary>
    public string Local { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o motivo.
    /// </summary>
    public string Motivo { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a antecedência do aviso, em minutos (RF-116).
    /// </summary>
    public int AntecedenciaAvisoMin { get; private set; }

    /// <summary>
    /// Gets o instante do aviso, em UTC.
    /// </summary>
    public DateTime? AvisoEnviadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que registou.
    /// </summary>
    public Guid CriadoPorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }
}
