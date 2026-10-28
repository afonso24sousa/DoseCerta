// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.ListaDiaria;

/// <summary>
/// Tipo de um item da lista diária (OP-50).
/// </summary>
public enum TipoItemListaDiaria
{
    /// <summary>
    /// Toma prevista de uma medicação.
    /// </summary>
    Toma,

    /// <summary>
    /// Compromisso de saúde, só de consulta (RF-087).
    /// </summary>
    Compromisso,
}
