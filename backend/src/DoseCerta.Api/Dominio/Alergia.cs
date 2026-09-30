// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Entrada da categoria fechada de alergias.
/// </summary>
public class Alergia
{
    /// <summary>
    /// Gets or sets o identificador da alergia.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets o nome da alergia.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a entrada representa a ausência de alergias
    /// conhecidas; nesse caso não pode ser combinada com outras.
    /// </summary>
    public bool EhAusencia { get; set; }
}
