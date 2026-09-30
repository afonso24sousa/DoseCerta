// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Entrada da categoria fechada de condições de saúde relevantes.
/// </summary>
public class CondicaoSaude
{
    /// <summary>
    /// Gets or sets o identificador da condição.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets o nome da condição.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a entrada representa a ausência de condições
    /// relevantes; nesse caso não pode ser combinada com outras.
    /// </summary>
    public bool EhAusencia { get; set; }
}
