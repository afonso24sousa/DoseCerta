// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Erro de validação dos dados submetidos (<c>VALIDACAO_INVALIDA</c>), com a lista dos campos em erro (RNF-028).
/// </summary>
public class ValidacaoException : DominioException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    public ValidacaoException()
        : this(Array.Empty<ErroCampo>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem em português europeu.</param>
    public ValidacaoException(string message)
        : base(CodigosErro.ValidacaoInvalida, message)
    {
        this.Campos = Array.Empty<ErroCampo>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem em português europeu.</param>
    /// <param name="innerException">Exceção que originou o erro.</param>
    public ValidacaoException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.Campos = Array.Empty<ErroCampo>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// Com os campos em erro.
    /// </summary>
    /// <param name="campos">Campos em erro e regras violadas.</param>
    public ValidacaoException(IEnumerable<ErroCampo> campos)
        : base(CodigosErro.ValidacaoInvalida, "Os dados submetidos são inválidos.")
    {
        ArgumentNullException.ThrowIfNull(campos);
        this.Campos = campos.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets os campos em erro e as regras violadas.
    /// </summary>
    public IReadOnlyList<ErroCampo> Campos { get; }

    /// <summary>
    /// Cria uma exceção de validação para um único campo.
    /// </summary>
    /// <param name="campo">Nome do campo em erro.</param>
    /// <param name="regra">Regra violada, com o valor ou formato esperado.</param>
    /// <returns>A exceção de validação.</returns>
    public static ValidacaoException DoCampo(string campo, string regra)
    {
        return new ValidacaoException(new[] { new ErroCampo(campo, regra) });
    }
}
