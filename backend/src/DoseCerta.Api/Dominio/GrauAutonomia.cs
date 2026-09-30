// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Entrada da categoria fechada de grau de autonomia e mobilidade.
/// </summary>
public class GrauAutonomia
{
    /// <summary>
    /// Gets or sets o identificador do grau.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets o nome do grau.
    /// </summary>
    public string Nome { get; set; } = string.Empty;
}
