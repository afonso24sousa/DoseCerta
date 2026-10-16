// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Raiz do agregado da ficha: dados do utente, necessidades específicas, associações e estado.
/// As associações só se alteram através da ficha (Modelo de Classes do Backend, secção 4.1).
/// </summary>
public class Ficha
{
    private readonly List<Associacao> associacoes = new();
    private readonly List<CondicaoSaude> condicoesSaude = new();
    private readonly List<Alergia> alergias = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Ficha"/> class.
    /// </summary>
    /// <param name="nomeCompleto">Nome completo do utente.</param>
    /// <param name="dataNascimento">Data de nascimento do utente.</param>
    /// <param name="contactoEmergencia">Contacto de emergência.</param>
    /// <param name="utilizadorProprioId">Titular da ficha própria, ou <see langword="null"/> numa ficha de terceiro.</param>
    /// <param name="criadaPorId">Utilizador que cria a ficha.</param>
    /// <param name="criadaEmUtc">Instante de criação, em UTC.</param>
    public Ficha(
        string nomeCompleto,
        DateOnly dataNascimento,
        string contactoEmergencia,
        Guid? utilizadorProprioId,
        Guid criadaPorId,
        DateTime criadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.NomeCompleto = nomeCompleto;
        this.DataNascimento = dataNascimento;
        this.ContactoEmergencia = contactoEmergencia;
        this.UtilizadorProprioId = utilizadorProprioId;
        this.Tipo = utilizadorProprioId.HasValue ? TipoFicha.Propria : TipoFicha.Terceira;
        this.Estado = EstadoFicha.Ativa;
        this.CriadaPorId = criadaPorId;
        this.CriadaEmUtc = criadaEmUtc;
        this.AtivaDesdeUtc = criadaEmUtc;
    }

    private Ficha()
    {
    }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o nome completo do utente.
    /// </summary>
    public string NomeCompleto { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a data de nascimento do utente.
    /// </summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>
    /// Gets o contacto de emergência.
    /// </summary>
    public string ContactoEmergencia { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o identificador do grau de autonomia ou mobilidade, se definido.
    /// </summary>
    public short? GrauAutonomiaId { get; private set; }

    /// <summary>
    /// Gets o grau de autonomia ou mobilidade, quando carregado.
    /// </summary>
    public GrauAutonomia? GrauAutonomia { get; private set; }

    /// <summary>
    /// Gets o tipo da ficha (própria ou de terceiro).
    /// </summary>
    public TipoFicha Tipo { get; private set; }

    /// <summary>
    /// Gets o identificador do titular, na ficha própria.
    /// </summary>
    public Guid? UtilizadorProprioId { get; private set; }

    /// <summary>
    /// Gets o estado da ficha.
    /// </summary>
    public EstadoFicha Estado { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que criou a ficha.
    /// </summary>
    public Guid CriadaPorId { get; private set; }

    /// <summary>
    /// Gets o instante de criação, em UTC.
    /// </summary>
    public DateTime CriadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante a partir do qual a ficha está ativa, em UTC (DM-19).
    /// </summary>
    public DateTime AtivaDesdeUtc { get; private set; }

    /// <summary>
    /// Gets o instante do arquivo, em UTC, quando arquivada.
    /// </summary>
    public DateTime? ArquivadaEmUtc { get; private set; }

    /// <summary>
    /// Gets as pessoas associadas à ficha.
    /// </summary>
    public IReadOnlyCollection<Associacao> Associacoes => this.associacoes;

    /// <summary>
    /// Gets as condições de saúde registadas.
    /// </summary>
    public IReadOnlyCollection<CondicaoSaude> CondicoesSaude => this.condicoesSaude;

    /// <summary>
    /// Gets as alergias registadas.
    /// </summary>
    public IReadOnlyCollection<Alergia> Alergias => this.alergias;
}
