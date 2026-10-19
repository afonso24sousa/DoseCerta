// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Conta de uma pessoa registada. Não guarda papéis: o papel é por ficha. A palavra-passe só existe em hash com salt e nunca é devolvida (RF-013, RNF-001).
/// </summary>
public class Utilizador
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Utilizador"/> class.
    /// </summary>
    /// <param name="nome">Nome da pessoa.</param>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="palavraPasseHash">Hash com salt da palavra-passe.</param>
    /// <param name="criadoEmUtc">Instante de criação, em UTC.</param>
    public Utilizador(
        string nome,
        string email,
        string palavraPasseHash,
        DateTime criadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.Nome = nome;
        this.Email = email;
        this.PalavraPasseHash = palavraPasseHash;
        this.CriadoEmUtc = criadoEmUtc;
    }

    private Utilizador()
    {
    }

    /// <summary>
    /// Gets o identificador do Utilizador.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o nome da pessoa.
    /// </summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o e-mail da conta, único sem distinção de maiúsculas (RF-011).
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o hash com salt da palavra-passe (RNF-001).
    /// </summary>
    public string PalavraPasseHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o instante de criação da conta, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }
}
