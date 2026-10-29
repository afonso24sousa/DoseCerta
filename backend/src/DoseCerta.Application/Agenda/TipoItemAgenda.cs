// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Agenda;

/// <summary>
/// Tipo de um evento da agenda (OP-45). A F007 foi retirada do âmbito, por isso não há eventos de tarefas recorrentes.
/// </summary>
public enum TipoItemAgenda
{
    /// <summary>
    /// Toma prevista de medicação.
    /// </summary>
    Toma,

    /// <summary>
    /// Compromisso de saúde.
    /// </summary>
    Compromisso,
}
