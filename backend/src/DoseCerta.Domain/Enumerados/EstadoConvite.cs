// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Estado de um convite (RF-043, RF-045, RF-046).
/// </summary>
public enum EstadoConvite
{
    /// <summary>
    /// Convite ainda sem resposta.
    /// </summary>
    Pendente,

    /// <summary>
    /// Convite aceite.
    /// </summary>
    Aceite,

    /// <summary>
    /// Convite recusado.
    /// </summary>
    Recusado,
}
