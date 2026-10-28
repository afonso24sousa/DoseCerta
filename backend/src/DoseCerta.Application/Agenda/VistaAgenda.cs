// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Agenda;

/// <summary>
/// Vista da agenda pedida em OP-45 (RF-084 a RF-086).
/// </summary>
public enum VistaAgenda
{
    /// <summary>
    /// O dia que contém a data.
    /// </summary>
    Dia,

    /// <summary>
    /// A semana, de segunda-feira a domingo, que contém a data.
    /// </summary>
    Semana,

    /// <summary>
    /// O mês que contém a data.
    /// </summary>
    Mes,
}
