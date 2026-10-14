// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Hora fixa do dia, em hora de Lisboa, de uma execução de uma tarefa recorrente (RF-070).
/// </summary>
public class HorarioTarefa
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HorarioTarefa"/> class.
    /// </summary>
    /// <param name="tarefaId">Identificador da tarefa.</param>
    /// <param name="horaLocal">Hora fixa, em hora de Lisboa.</param>
    public HorarioTarefa(
        Guid tarefaId,
        TimeOnly horaLocal)
    {
        this.Id = Guid.NewGuid();
        this.TarefaId = tarefaId;
        this.HoraLocal = horaLocal;
    }

    private HorarioTarefa()
    {
    }

    /// <summary>
    /// Gets o identificador do horário.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da tarefa.
    /// </summary>
    public Guid TarefaId { get; private set; }

    /// <summary>
    /// Gets a hora fixa, em hora de Lisboa.
    /// </summary>
    public TimeOnly HoraLocal { get; private set; }
}
