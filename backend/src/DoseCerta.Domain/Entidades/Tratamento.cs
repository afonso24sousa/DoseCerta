// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Medicação prescrita a uma ficha, com horários, data de início e data de fim opcional (RF-050). O comportamento é implementado com a F005.
/// </summary>
public class Tratamento
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tratamento"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="medicamento">Nome do medicamento.</param>
    /// <param name="dose">Dose.</param>
    /// <param name="grauImportancia">Grau de importância.</param>
    /// <param name="dataInicio">Data de início.</param>
    /// <param name="dataFim">Data de fim, se existir.</param>
    /// <param name="criadoPorId">Utilizador que regista.</param>
    /// <param name="criadoEmUtc">Instante do registo, em UTC.</param>
    public Tratamento(
        Guid fichaId,
        string medicamento,
        string dose,
        GrauImportancia grauImportancia,
        DateOnly dataInicio,
        DateOnly? dataFim,
        Guid criadoPorId,
        DateTime criadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.Medicamento = medicamento;
        this.Dose = dose;
        this.GrauImportancia = grauImportancia;
        this.DataInicio = dataInicio;
        this.DataFim = dataFim;
        this.Estado = EstadoTratamento.EmCurso;
        this.CriadoPorId = criadoPorId;
        this.CriadoEmUtc = criadoEmUtc;
    }

    private Tratamento()
    {
    }

    /// <summary>
    /// Gets o identificador do tratamento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o nome do medicamento.
    /// </summary>
    public string Medicamento { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a dose.
    /// </summary>
    public string Dose { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o grau de importância.
    /// </summary>
    public GrauImportancia GrauImportancia { get; private set; }

    /// <summary>
    /// Gets a data de início.
    /// </summary>
    public DateOnly DataInicio { get; private set; }

    /// <summary>
    /// Gets a data de fim, se existir.
    /// </summary>
    public DateOnly? DataFim { get; private set; }

    /// <summary>
    /// Gets o estado do tratamento.
    /// </summary>
    public EstadoTratamento Estado { get; private set; }

    /// <summary>
    /// Gets o instante do término, em UTC.
    /// </summary>
    public DateTime? TerminadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que registou o tratamento.
    /// </summary>
    public Guid CriadoPorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }
}
