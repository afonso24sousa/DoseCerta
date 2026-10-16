// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Estado de uma toma (RF-064 a RF-068).
/// </summary>
public enum EstadoToma
{
    /// <summary>
    /// Toma ainda não confirmada.
    /// </summary>
    Pendente,

    /// <summary>
    /// Confirmada até ao fim da tolerância.
    /// </summary>
    Confirmada,

    /// <summary>
    /// Confirmada depois da tolerância e antes das 24 horas.
    /// </summary>
    ConfirmadaComAtraso,

    /// <summary>
    /// Sem confirmação 24 horas depois da hora prevista; estado final.
    /// </summary>
    NaoConfirmada,
}
