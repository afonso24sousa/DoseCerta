// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.RegularExpressions;
using DoseCerta.Domain.Comum;

namespace DoseCerta.Application.Comum;

/// <summary>
/// Acumula os erros de validação dos dados submetidos e lança uma única <see cref="ValidacaoException"/>
/// com todos os campos em erro (RNF-028).
/// </summary>
public sealed partial class ValidadorDados
{
    /// <summary>
    /// Comprimento máximo de um e-mail (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoEmail = 254;

    private readonly List<ErroCampo> erros = new();

    /// <summary>
    /// Gets a value indicating whether existem erros registados.
    /// </summary>
    public bool TemErros => this.erros.Count > 0;

    /// <summary>
    /// Normaliza um texto: remove os espaços nas extremidades e converte texto vazio em <see langword="null"/>.
    /// </summary>
    /// <param name="valor">Valor submetido.</param>
    /// <returns>O valor normalizado.</returns>
    public static string? Normalizar(string? valor)
    {
        var limpo = valor?.Trim();
        return string.IsNullOrEmpty(limpo) ? null : limpo;
    }

    /// <summary>
    /// Indica se um e-mail tem formato válido.
    /// </summary>
    /// <param name="email">E-mail a verificar.</param>
    /// <returns><see langword="true"/> se o formato for válido.</returns>
    public static bool EmailValido(string? email)
    {
        return email is not null && email.Length <= ComprimentoMaximoEmail && PadraoEmail().IsMatch(email);
    }

    /// <summary>
    /// Regista um erro num campo.
    /// </summary>
    /// <param name="campo">Nome do campo.</param>
    /// <param name="regra">Regra violada, com o valor ou formato esperado.</param>
    /// <returns>Este validador, para encadeamento.</returns>
    public ValidadorDados Erro(string campo, string regra)
    {
        this.erros.Add(new ErroCampo(campo, regra));
        return this;
    }

    /// <summary>
    /// Verifica que um texto obrigatório está preenchido e não excede o comprimento máximo.
    /// </summary>
    /// <param name="campo">Nome do campo.</param>
    /// <param name="valor">Valor normalizado.</param>
    /// <param name="comprimentoMaximo">Comprimento máximo.</param>
    /// <returns>Este validador, para encadeamento.</returns>
    public ValidadorDados Obrigatorio(string campo, string? valor, int comprimentoMaximo)
    {
        if (valor is null)
        {
            return this.Erro(campo, "É obrigatório.");
        }

        if (valor.Length > comprimentoMaximo)
        {
            return this.Erro(campo, $"Tem de ter no máximo {comprimentoMaximo} caracteres.");
        }

        return this;
    }

    /// <summary>
    /// Verifica que um e-mail obrigatório está preenchido e tem formato válido.
    /// </summary>
    /// <param name="campo">Nome do campo.</param>
    /// <param name="valor">Valor normalizado.</param>
    /// <returns>Este validador, para encadeamento.</returns>
    public ValidadorDados Email(string campo, string? valor)
    {
        if (valor is null)
        {
            return this.Erro(campo, "É obrigatório.");
        }

        return EmailValido(valor) ? this : this.Erro(campo, "Tem de ter um formato de e-mail válido, por exemplo nome@exemplo.pt.");
    }

    /// <summary>
    /// Verifica que uma palavra-passe cumpre o RNF-003.
    /// </summary>
    /// <param name="campo">Nome do campo.</param>
    /// <param name="valor">Palavra-passe submetida.</param>
    /// <returns>Este validador, para encadeamento.</returns>
    public ValidadorDados PalavraPasse(string campo, string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return this.Erro(campo, "É obrigatória.");
        }

        return ParametrosDeNegocio.PalavraPasseValida(valor)
            ? this
            : this.Erro(campo, "Tem de ter pelo menos 8 caracteres, com uma letra e um algarismo.");
    }

    /// <summary>
    /// Lança uma <see cref="ValidacaoException"/> se existirem erros registados.
    /// </summary>
    /// <exception cref="ValidacaoException">Existem campos em erro.</exception>
    public void LancarSeInvalido()
    {
        if (this.TemErros)
        {
            throw new ValidacaoException(this.erros);
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PadraoEmail();
}
