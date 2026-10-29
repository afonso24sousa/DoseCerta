// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Tipo de um registo partilhado da ficha (F009): ocorrência ou nota (OP-48, OP-49).
/// </summary>
public enum TipoRegisto
{
    /// <summary>
    /// Ocorrência do dia a dia, visível a Cuidadores e Familiares (RF-094).
    /// </summary>
    Ocorrencia,

    /// <summary>
    /// Nota dirigida aos Cuidadores, visível só a eles (RF-095).
    /// </summary>
    Nota,
}
