// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Associação de um Utilizador a uma ficha, com um papel.
/// </summary>
/// <remarks>
/// Só é alterada através dos métodos de <see cref="Ficha"/>, que aplicam as regras da F003 e da F004.
/// </remarks>
public class AssociacaoFicha
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssociacaoFicha"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="papel">Papel do Utilizador na ficha.</param>
    /// <param name="ehCuidadorPrincipal">Indica se é o Cuidador principal.</param>
    /// <param name="dataAssociacao">Data da associação (UTC).</param>
    internal AssociacaoFicha(Guid fichaId, Guid utilizadorId, PapelFicha papel, bool ehCuidadorPrincipal, DateTime dataAssociacao)
    {
        this.FichaId = fichaId;
        this.UtilizadorId = utilizadorId;
        this.Papel = papel;
        this.EhCuidadorPrincipal = ehCuidadorPrincipal;
        this.DataAssociacao = dataAssociacao;
    }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador.
    /// </summary>
    public Guid UtilizadorId { get; private set; }

    /// <summary>
    /// Gets o Utilizador associado.
    /// </summary>
    public Utilizador? Utilizador { get; private set; }

    /// <summary>
    /// Gets o papel do Utilizador na ficha.
    /// </summary>
    public PapelFicha Papel { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether o Utilizador é o Cuidador principal da ficha.
    /// </summary>
    public bool EhCuidadorPrincipal { get; internal set; }

    /// <summary>
    /// Gets a data da associação (UTC).
    /// </summary>
    public DateTime DataAssociacao { get; private set; }
}
