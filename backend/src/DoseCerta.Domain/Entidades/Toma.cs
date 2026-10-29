// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Toma prevista num instante, gerada antecipadamente; copia o medicamento, a dose e o grau para preservar o histórico (DM-07, DM-17).
/// Segue o diagrama de estados da toma: confirmação dentro ou fora da tolerância e passagem a não confirmada (F006).
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

    /// <summary>
    /// Indica se a toma já foi confirmada, dentro ou fora da tolerância.
    /// </summary>
    /// <returns><see langword="true"/> se estiver <c>Confirmada</c> ou <c>ConfirmadaComAtraso</c>.</returns>
    public bool EstaConfirmada()
    {
        return this.Estado is EstadoToma.Confirmada or EstadoToma.ConfirmadaComAtraso;
    }

    /// <summary>
    /// Obtém o fim da tolerância do grau da toma, inclusive (RF-065, RF-066).
    /// </summary>
    /// <returns>O instante previsto mais a tolerância.</returns>
    public DateTime FimTolerancia()
    {
        return PoliticaTolerancia.FimTolerancia(this.PrevistaEmUtc, this.GrauImportancia);
    }

    /// <summary>
    /// Obtém o instante a partir do qual a toma pendente passa a não confirmada (RF-068).
    /// </summary>
    /// <returns>O instante previsto mais 24 horas.</returns>
    public DateTime InstanteNaoConfirmada()
    {
        return PoliticaTolerancia.InstanteNaoConfirmada(this.PrevistaEmUtc);
    }

    /// <summary>
    /// Indica se a janela de confirmação já abriu: abre às 00:00 de Lisboa do dia previsto (RF-064, TR-13).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="calendario">Calendário de Lisboa.</param>
    /// <returns><see langword="true"/> a partir das 00:00 de Lisboa do dia da toma.</returns>
    public bool JanelaDeConfirmacaoAberta(DateTime agoraUtc, CalendarioLisboa calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        return agoraUtc >= calendario.InicioDoDiaUtc(calendario.DiaLocal(this.PrevistaEmUtc));
    }

    /// <summary>
    /// Indica se a toma pendente já devia ter passado a não confirmada (RF-068, TR-06).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <returns><see langword="true"/> se estiver pendente e tiverem passado 24 horas da hora prevista.</returns>
    public bool NaoConfirmadaDevida(DateTime agoraUtc)
    {
        return this.Estado == EstadoToma.Pendente && agoraUtc >= this.InstanteNaoConfirmada();
    }

    /// <summary>
    /// Passa a toma pendente a não confirmada, se já tiverem passado 24 horas da hora prevista; estado final (RF-068, TR-06).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    public void MarcarNaoConfirmada(DateTime agoraUtc)
    {
        if (!this.NaoConfirmadaDevida(agoraUtc))
        {
            return;
        }

        this.Estado = EstadoToma.NaoConfirmada;
        this.NaoConfirmadaDetetadaEmUtc = agoraUtc;
    }

    /// <summary>
    /// Confirma a toma, registando o Cuidador e o instante: até ao fim da tolerância fica <c>Confirmada</c> e depois fica
    /// <c>ConfirmadaComAtraso</c> (RF-064 a RF-067, TR-03, TR-04).
    /// </summary>
    /// <param name="cuidadorId">Cuidador que confirma.</param>
    /// <param name="agoraUtc">Instante da confirmação, em UTC.</param>
    /// <param name="calendario">Calendário de Lisboa.</param>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.TomaJaConfirmada"/> (TR-15), <see cref="CodigosErro.TomaNaoConfirmavel"/> depois das 24 horas
    /// (TR-14) ou <see cref="CodigosErro.TomaForaDaJanela"/> antes das 00:00 de Lisboa do dia da toma (TR-13).
    /// </exception>
    public void Confirmar(Guid cuidadorId, DateTime agoraUtc, CalendarioLisboa calendario)
    {
        // A primeira confirmação mantém-se; o serviço trata este caso antes, porque o corpo do 409 leva o nome de quem confirmou.
        if (this.EstaConfirmada())
        {
            throw new DominioException(CodigosErro.TomaJaConfirmada, "A toma já foi confirmada.");
        }

        if (this.Estado == EstadoToma.NaoConfirmada || this.NaoConfirmadaDevida(agoraUtc))
        {
            this.MarcarNaoConfirmada(agoraUtc);
            throw new DominioException(
                CodigosErro.TomaNaoConfirmavel,
                "A toma já não pode ser confirmada: passaram 24 horas da hora prevista.");
        }

        if (!this.JanelaDeConfirmacaoAberta(agoraUtc, calendario))
        {
            throw new DominioException(
                CodigosErro.TomaForaDaJanela,
                "A toma só pode ser confirmada a partir das 00:00 do dia previsto.");
        }

        this.Estado = agoraUtc <= this.FimTolerancia() ? EstadoToma.Confirmada : EstadoToma.ConfirmadaComAtraso;
        this.ConfirmadaPorId = cuidadorId;
        this.ConfirmadaEmUtc = agoraUtc;
    }
}
