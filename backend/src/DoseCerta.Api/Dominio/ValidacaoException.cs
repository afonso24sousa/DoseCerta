// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Dados de entrada inválidos, com os erros agrupados por campo.
/// </summary>
public class ValidacaoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    public ValidacaoException()
        : this(new Dictionary<string, string[]>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem geral.</param>
    public ValidacaoException(string message)
        : base(message)
    {
        this.Erros = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem geral.</param>
    /// <param name="innerException">Exceção que originou esta.</param>
    public ValidacaoException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.Erros = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidacaoException"/> class.
    /// </summary>
    /// <param name="erros">Erros agrupados pelo nome do campo.</param>
    public ValidacaoException(IReadOnlyDictionary<string, string[]> erros)
        : base("Um ou mais campos são inválidos.")
    {
        this.Erros = erros;
    }

    /// <summary>
    /// Gets os erros agrupados pelo nome do campo.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
