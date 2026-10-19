// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Tarefa de cuidados recorrente de uma ficha (RF-070). Uma tarefa eliminada fica inativa (DM-14). O comportamento é implementado com a F007.
/// </summary>
public class TarefaRecorrente
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TarefaRecorrente"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="designacao">Designação da tarefa.</param>
    /// <param name="periodicidadeTipo">Tipo de periodicidade.</param>
    /// <param name="intervaloDias">Intervalo em dias, se aplicável.</param>
    /// <param name="dataInicio">Data de início.</param>
    /// <param name="toleranciaMinutos">Tolerância em minutos.</param>
    /// <param name="criadaPorId">Utilizador que regista.</param>
    /// <param name="criadaEmUtc">Instante do registo, em UTC.</param>
    public TarefaRecorrente(
        Guid fichaId,
        string designacao,
        PeriodicidadeTipo periodicidadeTipo,
        byte? intervaloDias,
        DateOnly dataInicio,
        short toleranciaMinutos,
        Guid criadaPorId,
        DateTime criadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.Designacao = designacao;
        this.PeriodicidadeTipo = periodicidadeTipo;
        this.IntervaloDias = intervaloDias;
        this.DataInicio = dataInicio;
        this.ToleranciaMinutos = toleranciaMinutos;
        this.Ativa = true;
        this.CriadaPorId = criadaPorId;
        this.CriadaEmUtc = criadaEmUtc;
    }

    private TarefaRecorrente()
    {
    }

    /// <summary>
    /// Gets o identificador da tarefa.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets a designação da tarefa.
    /// </summary>
    public string Designacao { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o tipo de periodicidade.
    /// </summary>
    public PeriodicidadeTipo PeriodicidadeTipo { get; private set; }

    /// <summary>
    /// Gets o intervalo em dias, só na periodicidade de N em N dias.
    /// </summary>
    public byte? IntervaloDias { get; private set; }

    /// <summary>
    /// Gets a data de início.
    /// </summary>
    public DateOnly DataInicio { get; private set; }

    /// <summary>
    /// Gets a tolerância, em minutos.
    /// </summary>
    public short ToleranciaMinutos { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a tarefa está ativa (não eliminada).
    /// </summary>
    public bool Ativa { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que registou a tarefa.
    /// </summary>
    public Guid CriadaPorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime CriadaEmUtc { get; private set; }
}
