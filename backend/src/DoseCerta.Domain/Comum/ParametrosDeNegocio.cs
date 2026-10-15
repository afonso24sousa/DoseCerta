// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Valores fixos das regras de negócio, num só lugar (Modelo de Classes do Backend, secção 4.2).
/// </summary>
public static class ParametrosDeNegocio
{
    /// <summary>
    /// Número mínimo de caracteres da palavra-passe (RNF-003).
    /// </summary>
    public const int TamanhoMinimoPalavraPasse = 8;

    /// <summary>
    /// Validade do token de sessão: 8 horas (RNF-009).
    /// </summary>
    public static readonly TimeSpan ValidadeSessao = TimeSpan.FromHours(8);

    /// <summary>
    /// Verifica se a palavra-passe cumpre o RNF-003: pelo menos 8 caracteres, com uma letra e um algarismo.
    /// </summary>
    /// <param name="palavraPasse">Palavra-passe a verificar.</param>
    /// <returns><see langword="true"/> se a palavra-passe for válida.</returns>
    public static bool PalavraPasseValida(string? palavraPasse)
    {
        return palavraPasse is not null
            && palavraPasse.Length >= TamanhoMinimoPalavraPasse
            && palavraPasse.Any(char.IsLetter)
            && palavraPasse.Any(char.IsDigit);
    }
}
