// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Ligação de um Utilizador a uma ficha com um papel. O Cuidador principal é um Cuidador marcado e é provisório enquanto o convite para Cuidador principal está pendente (RF-025, DM-02, DM-03).
/// </summary>
public class Associacao
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Associacao"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="papel">Papel na ficha.</param>
    /// <param name="criadaEmUtc">Instante da associação, em UTC.</param>
    public Associacao(
        Guid fichaId,
        Guid utilizadorId,
        Papel papel,
        DateTime criadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.UtilizadorId = utilizadorId;
        this.Papel = papel;
        this.CriadaEmUtc = criadaEmUtc;
    }

    private Associacao()
    {
    }

    /// <summary>
    /// Gets o identificador da associação.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador associado.
    /// </summary>
    public Guid UtilizadorId { get; private set; }

    /// <summary>
    /// Gets o papel do Utilizador na ficha.
    /// </summary>
    public Papel Papel { get; private set; }

    /// <summary>
    /// Gets a value indicating whether esta associação é a do Cuidador principal.
    /// </summary>
    public bool EPrincipal { get; private set; }

    /// <summary>
    /// Gets a value indicating whether o Cuidador principal é provisório (convite para Cuidador principal pendente).
    /// </summary>
    public bool EProvisorio { get; private set; }

    /// <summary>
    /// Gets o instante da associação, em UTC.
    /// </summary>
    public DateTime CriadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o Utilizador associado, quando carregado.
    /// </summary>
    public Utilizador? Utilizador { get; private set; }

    /// <summary>
    /// Indica se o papel é Cuidador (incluindo o Cuidador principal).
    /// </summary>
    /// <returns><see langword="true"/> se o papel for Cuidador.</returns>
    public bool EhCuidador()
    {
        return this.Papel == Papel.Cuidador;
    }

    /// <summary>
    /// Indica se o papel permite escrever na ficha: só o Cuidador (RF-047, RF-048).
    /// </summary>
    /// <returns><see langword="true"/> se o Utilizador puder escrever na ficha.</returns>
    public bool PodeEscrever()
    {
        return this.EhCuidador();
    }

    /// <summary>
    /// Marca esta associação como a do Cuidador principal, provisório ou não (RF-024, RF-025).
    /// Só a ficha a usa, para garantir um só Cuidador principal.
    /// </summary>
    /// <param name="provisorio">Indica se o Cuidador principal é provisório até à resposta ao convite.</param>
    internal void MarcarPrincipal(bool provisorio)
    {
        this.EPrincipal = true;
        this.EProvisorio = provisorio;
    }
}
