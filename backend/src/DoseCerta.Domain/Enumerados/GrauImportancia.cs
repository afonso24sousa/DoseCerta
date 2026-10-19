// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Grau de importância de um tratamento (RF-051).
/// </summary>
public enum GrauImportancia
{
    /// <summary>
    /// Tolerância de 2 horas.
    /// </summary>
    Leve,

    /// <summary>
    /// Tolerância de 1 hora.
    /// </summary>
    Moderado,

    /// <summary>
    /// Tolerância de 30 minutos.
    /// </summary>
    Grave,
}
