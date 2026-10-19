// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Papel de um Utilizador numa ficha (RF-047, RF-048). O Cuidador principal é um Cuidador marcado (DM-02).
/// </summary>
public enum Papel
{
    /// <summary>
    /// Permissão de escrita completa sobre a ficha (RF-047).
    /// </summary>
    Cuidador,

    /// <summary>
    /// Apenas consulta (RF-048).
    /// </summary>
    Familiar,
}
