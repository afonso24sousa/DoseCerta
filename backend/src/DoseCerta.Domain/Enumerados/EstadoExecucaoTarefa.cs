// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Estado de uma execução de tarefa recorrente (RF-074).
/// </summary>
public enum EstadoExecucaoTarefa
{
    /// <summary>
    /// Execução ainda não confirmada.
    /// </summary>
    PorConfirmar,

    /// <summary>
    /// Execução confirmada por um Cuidador.
    /// </summary>
    Realizada,
}
