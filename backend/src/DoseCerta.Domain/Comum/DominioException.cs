// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Erro de regra de negócio com o código de erro da API (Documentação da API, secção 3.3).
/// É traduzido numa resposta HTTP pelo filtro de erros da API. Corresponde à classe <c>ExcecaoDominio</c> do
/// Modelo de Classes do Backend; o sufixo <c>Exception</c> é exigido pela regra CA1710 da análise estática (QG-05).
/// </summary>
public class DominioException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DominioException"/> class.
    /// Com o código genérico de erro interno.
    /// </summary>
    public DominioException()
        : this(CodigosErro.ErroInterno, "Ocorreu um erro inesperado.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DominioException"/> class.
    /// Com o código genérico de erro interno.
    /// </summary>
    /// <param name="message">Mensagem em português europeu.</param>
    public DominioException(string message)
        : this(CodigosErro.ErroInterno, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DominioException"/> class.
    /// Com o código genérico de erro interno.
    /// </summary>
    /// <param name="message">Mensagem em português europeu.</param>
    /// <param name="innerException">Exceção que originou o erro.</param>
    public DominioException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.Codigo = CodigosErro.ErroInterno;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DominioException"/> class.
    /// </summary>
    /// <param name="codigo">Código de erro da API, por exemplo <c>EMAIL_JA_REGISTADO</c>.</param>
    /// <param name="mensagem">Mensagem em português europeu (RNF-020).</param>
    public DominioException(string codigo, string mensagem)
        : base(mensagem)
    {
        this.Codigo = codigo;
    }

    /// <summary>
    /// Gets o código de erro da API.
    /// </summary>
    public string Codigo { get; }
}
