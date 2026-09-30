// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Violação de uma regra de negócio, identificada por um <see cref="CodigoErro"/>.
/// </summary>
public class RegraNegocioException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegraNegocioException"/> class.
    /// </summary>
    public RegraNegocioException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegraNegocioException"/> class.
    /// </summary>
    /// <param name="message">Mensagem para o Utilizador.</param>
    public RegraNegocioException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegraNegocioException"/> class.
    /// </summary>
    /// <param name="message">Mensagem para o Utilizador.</param>
    /// <param name="innerException">Exceção que originou esta.</param>
    public RegraNegocioException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegraNegocioException"/> class.
    /// </summary>
    /// <param name="codigo">Código da regra violada.</param>
    /// <param name="message">Mensagem para o Utilizador.</param>
    /// <param name="innerException">Exceção que originou esta, se existir.</param>
    public RegraNegocioException(CodigoErro codigo, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        this.Codigo = codigo;
    }

    /// <summary>
    /// Gets o código da regra violada.
    /// </summary>
    public CodigoErro? Codigo { get; }
}
