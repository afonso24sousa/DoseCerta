// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Toma prevista num instante, gerada antecipadamente; copia o medicamento, a dose e o grau para preservar o histórico (DM-07, DM-17). O comportamento é implementado com a F006.
/// </summary>
public class Toma
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Toma"/> class.
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="previstaEmUtc">Instante previsto, em UTC.</param>
    /// <param name="medicamento">Medicamento.</param>
    /// <param name="dose">Dose.</param>
    /// <param name="grauImportancia">Grau de importância.</param>
    public Toma(
        Guid tratamentoId,
        DateTime previstaEmUtc,
        string medicamento,
        string dose,
        GrauImportancia grauImportancia)
    {
        this.Id = Guid.NewGuid();
        this.TratamentoId = tratamentoId;
        this.PrevistaEmUtc = previstaEmUtc;
        this.Medicamento = medicamento;
        this.Dose = dose;
        this.GrauImportancia = grauImportancia;
        this.Estado = EstadoToma.Pendente;
    }

    private Toma()
    {
    }

    /// <summary>
    /// Gets o identificador da toma.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador do tratamento.
    /// </summary>
    public Guid TratamentoId { get; private set; }

    /// <summary>
    /// Gets o instante previsto, em UTC.
    /// </summary>
    public DateTime PrevistaEmUtc { get; private set; }

    /// <summary>
    /// Gets o medicamento copiado do tratamento.
    /// </summary>
    public string Medicamento { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a dose copiada do tratamento.
    /// </summary>
    public string Dose { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o grau copiado do tratamento.
    /// </summary>
    public GrauImportancia GrauImportancia { get; private set; }

    /// <summary>
    /// Gets o estado da toma.
    /// </summary>
    public EstadoToma Estado { get; private set; }

    /// <summary>
    /// Gets o identificador do Cuidador que confirmou.
    /// </summary>
    public Guid? ConfirmadaPorId { get; private set; }

    /// <summary>
    /// Gets o instante da confirmação, em UTC.
    /// </summary>
    public DateTime? ConfirmadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante em que a toma passou a não confirmada, em UTC.
    /// </summary>
    public DateTime? NaoConfirmadaDetetadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da notificação de atraso aos Cuidadores, em UTC.
    /// </summary>
    public DateTime? CuidadoresNotificadosEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da notificação aos Familiares, em UTC.
    /// </summary>
    public DateTime? FamiliaresNotificadosEmUtc { get; private set; }

    /// <summary>
    /// Gets o token de concorrência otimista (AD-11).
    /// </summary>
    public byte[] Versao { get; private set; } = Array.Empty<byte>();
}
