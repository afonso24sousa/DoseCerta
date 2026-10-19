// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Tipo de periodicidade de uma tarefa recorrente (RF-070).
/// </summary>
public enum PeriodicidadeTipo
{
    /// <summary>
    /// Todos os dias.
    /// </summary>
    Diaria,

    /// <summary>
    /// Em dias da semana escolhidos.
    /// </summary>
    DiasSemana,

    /// <summary>
    /// De N em N dias, com N de 2 a 30.
    /// </summary>
    CadaNDias,
}
