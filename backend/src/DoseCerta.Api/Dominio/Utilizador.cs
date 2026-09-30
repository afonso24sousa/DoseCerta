// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Utilizador registado no sistema.
/// </summary>
/// <remarks>
/// Contrato mínimo (Id, Nome, Email) acordado com a I001 (F001), que é dona desta entidade
/// e lhe acrescentará os dados de autenticação e de conta.
/// </remarks>
public class Utilizador
{
    /// <summary>
    /// Gets or sets o identificador do Utilizador.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets o nome do Utilizador.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o e-mail do Utilizador (único).
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
