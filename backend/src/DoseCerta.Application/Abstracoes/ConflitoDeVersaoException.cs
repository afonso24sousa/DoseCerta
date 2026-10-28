// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Outra operação alterou o mesmo registo entre a leitura e a gravação (concorrência otimista, AD-11, RNF-044). A unidade de
/// trabalho lança-a depois de desfazer a transação, para a camada de aplicação não depender do EF Core.
/// </summary>
public sealed class ConflitoDeVersaoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflitoDeVersaoException"/> class.
    /// </summary>
    public ConflitoDeVersaoException()
        : base("O registo foi alterado por outra operação.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflitoDeVersaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem do erro.</param>
    public ConflitoDeVersaoException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflitoDeVersaoException"/> class.
    /// </summary>
    /// <param name="message">Mensagem do erro.</param>
    /// <param name="innerException">Exceção de concorrência original.</param>
    public ConflitoDeVersaoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
