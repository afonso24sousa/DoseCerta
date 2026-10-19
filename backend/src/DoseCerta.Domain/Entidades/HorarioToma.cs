// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Hora fixa do dia, em hora de Lisboa, de uma toma de um tratamento (RF-050).
/// </summary>
public class HorarioToma
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HorarioToma"/> class.
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="horaLocal">Hora fixa, em hora de Lisboa.</param>
    public HorarioToma(
        Guid tratamentoId,
        TimeOnly horaLocal)
    {
        this.Id = Guid.NewGuid();
        this.TratamentoId = tratamentoId;
        this.HoraLocal = horaLocal;
    }

    private HorarioToma()
    {
    }

    /// <summary>
    /// Gets o identificador do horário.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador do tratamento.
    /// </summary>
    public Guid TratamentoId { get; private set; }

    /// <summary>
    /// Gets a hora fixa, em hora de Lisboa.
    /// </summary>
    public TimeOnly HoraLocal { get; private set; }
}
