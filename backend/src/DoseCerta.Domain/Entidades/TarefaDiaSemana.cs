// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Dia da semana (1 segunda-feira a 7 domingo) de uma tarefa com periodicidade em dias da semana (RF-070).
/// </summary>
public class TarefaDiaSemana
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TarefaDiaSemana"/> class.
    /// </summary>
    /// <param name="tarefaId">Identificador da tarefa.</param>
    /// <param name="diaSemana">Dia da semana, de 1 a 7.</param>
    public TarefaDiaSemana(
        Guid tarefaId,
        byte diaSemana)
    {
        this.TarefaId = tarefaId;
        this.DiaSemana = diaSemana;
    }

    private TarefaDiaSemana()
    {
    }

    /// <summary>
    /// Gets o identificador da tarefa.
    /// </summary>
    public Guid TarefaId { get; private set; }

    /// <summary>
    /// Gets o dia da semana, de 1 (segunda-feira) a 7 (domingo).
    /// </summary>
    public byte DiaSemana { get; private set; }
}
