// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Medicação prescrita a uma ficha, com horários, data de início e data de fim opcional (RF-050). Calcula as tomas a partir
/// dos horários; sem data de fim é contínuo e as tomas geram-se numa janela móvel de 30 dias (RF-060).
/// </summary>
public class Tratamento
{
    private readonly List<HorarioToma> horarios = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Tratamento"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="medicamento">Nome do medicamento.</param>
    /// <param name="dose">Dose.</param>
    /// <param name="grauImportancia">Grau de importância.</param>
    /// <param name="dataInicio">Data de início.</param>
    /// <param name="dataFim">Data de fim, se existir.</param>
    /// <param name="horas">Horas fixas de toma por dia, em hora de Lisboa (uma ou mais).</param>
    /// <param name="criadoPorId">Utilizador que regista.</param>
    /// <param name="criadoEmUtc">Instante do registo, em UTC.</param>
    /// <exception cref="ArgumentException">Sem horas, ou com a data de fim anterior à data de início.</exception>
    public Tratamento(
        Guid fichaId,
        string medicamento,
        string dose,
        GrauImportancia grauImportancia,
        DateOnly dataInicio,
        DateOnly? dataFim,
        IEnumerable<TimeOnly> horas,
        Guid criadoPorId,
        DateTime criadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.DataInicio = dataInicio;
        this.Estado = EstadoTratamento.EmCurso;
        this.CriadoPorId = criadoPorId;
        this.CriadoEmUtc = criadoEmUtc;
        this.DefinirDados(medicamento, dose, grauImportancia, dataFim, horas);
    }

    private Tratamento()
    {
    }

    /// <summary>
    /// Gets o identificador do tratamento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o nome do medicamento.
    /// </summary>
    public string Medicamento { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a dose.
    /// </summary>
    public string Dose { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o grau de importância.
    /// </summary>
    public GrauImportancia GrauImportancia { get; private set; }

    /// <summary>
    /// Gets a data de início.
    /// </summary>
    public DateOnly DataInicio { get; private set; }

    /// <summary>
    /// Gets a data de fim, se existir.
    /// </summary>
    public DateOnly? DataFim { get; private set; }

    /// <summary>
    /// Gets o estado do tratamento.
    /// </summary>
    public EstadoTratamento Estado { get; private set; }

    /// <summary>
    /// Gets o instante do término, em UTC.
    /// </summary>
    public DateTime? TerminadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que registou o tratamento.
    /// </summary>
    public Guid CriadoPorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }

    /// <summary>
    /// Gets as horas fixas de toma por dia.
    /// </summary>
    public IReadOnlyCollection<HorarioToma> Horarios => this.horarios;

    /// <summary>
    /// Recusa a alteração de um tratamento terminado (RF-055).
    /// </summary>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.TratamentoTerminado"/>, se o tratamento estiver terminado.</exception>
    public void ExigirEmCurso()
    {
        if (this.Estado == EstadoTratamento.Terminado)
        {
            throw new DominioException(CodigosErro.TratamentoTerminado, "Um tratamento terminado não pode ser alterado.");
        }
    }

    /// <summary>
    /// Altera os dados de um tratamento em curso; a data de início não muda (RF-055).
    /// </summary>
    /// <param name="medicamento">Nome do medicamento.</param>
    /// <param name="dose">Dose.</param>
    /// <param name="grau">Grau de importância.</param>
    /// <param name="dataFim">Data de fim, ou <see langword="null"/> para um tratamento contínuo.</param>
    /// <param name="horas">Horas fixas de toma por dia (uma ou mais).</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.TratamentoTerminado"/>, se o tratamento estiver terminado.</exception>
    /// <exception cref="ArgumentException">Sem horas, ou com a data de fim anterior à data de início.</exception>
    public void Atualizar(string medicamento, string dose, GrauImportancia grau, DateOnly? dataFim, IEnumerable<TimeOnly> horas)
    {
        this.ExigirEmCurso();
        this.DefinirDados(medicamento, dose, grau, dataFim, horas);
    }

    /// <summary>
    /// Termina o tratamento; deixa de gerar tomas a partir deste instante (RF-056).
    /// </summary>
    /// <param name="agoraUtc">Instante do término, em UTC.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.TratamentoJaTerminado"/>, se já estiver terminado.</exception>
    public void Terminar(DateTime agoraUtc)
    {
        if (this.Estado == EstadoTratamento.Terminado)
        {
            throw new DominioException(CodigosErro.TratamentoJaTerminado, "O tratamento já está terminado.");
        }

        this.Estado = EstadoTratamento.Terminado;
        this.TerminadoEmUtc = agoraUtc;
    }

    /// <summary>
    /// Indica se o tratamento em curso já passou a data de fim prevista (RF-056).
    /// </summary>
    /// <param name="hojeLocal">Dia atual, em hora de Lisboa.</param>
    /// <returns><see langword="true"/> se estiver em curso e a data de fim for anterior a hoje.</returns>
    public bool DataFimUltrapassada(DateOnly hojeLocal)
    {
        return this.Estado == EstadoTratamento.EmCurso && this.DataFim < hojeLocal;
    }

    /// <summary>
    /// Calcula as tomas previstas num intervalo: uma por horário e por dia entre a data de início e a data de fim, em hora de
    /// Lisboa, com o instante em UTC. Cada toma copia o medicamento, a dose e o grau (RF-060, RNF-047, RNF-049).
    /// </summary>
    /// <param name="deUtc">Início do intervalo, inclusive.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive.</param>
    /// <param name="calendario">Calendário de Lisboa.</param>
    /// <returns>As tomas previstas, por ordem cronológica; nenhuma se o tratamento estiver terminado.</returns>
    public IReadOnlyList<Toma> GerarTomas(DateTime deUtc, DateTime ateUtc, CalendarioLisboa calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);

        var tomas = new List<Toma>();
        if (this.Estado == EstadoTratamento.Terminado || deUtc >= ateUtc)
        {
            return tomas;
        }

        var primeiroDia = calendario.DiaLocal(deUtc) > this.DataInicio ? calendario.DiaLocal(deUtc) : this.DataInicio;
        var ultimoDia = calendario.DiaLocal(ateUtc);
        if (this.DataFim < ultimoDia)
        {
            ultimoDia = this.DataFim.Value;
        }

        // Numa mudança de hora, duas horas fixas podem dar o mesmo instante; só se gera uma toma por instante (UQ_Toma_TratamentoInstante).
        var instantes = new HashSet<DateTime>();
        var horas = this.horarios.Select(h => h.HoraLocal).Order().ToList();
        for (var dia = primeiroDia; dia <= ultimoDia; dia = dia.AddDays(1))
        {
            foreach (var hora in horas)
            {
                var instante = calendario.ParaUtc(dia, hora);
                if (instante >= deUtc && instante < ateUtc && instantes.Add(instante))
                {
                    tomas.Add(new Toma(this.Id, instante, this.Medicamento, this.Dose, this.GrauImportancia));
                }
            }
        }

        return tomas;
    }

    private void DefinirDados(string medicamento, string dose, GrauImportancia grau, DateOnly? dataFim, IEnumerable<TimeOnly> horas)
    {
        ArgumentNullException.ThrowIfNull(horas);

        var novas = horas.Distinct().ToList();
        if (novas.Count == 0)
        {
            throw new ArgumentException("O tratamento tem de ter pelo menos uma hora de toma.", nameof(horas));
        }

        if (dataFim < this.DataInicio)
        {
            throw new ArgumentException("A data de fim não pode ser anterior à data de início.", nameof(dataFim));
        }

        this.Medicamento = medicamento;
        this.Dose = dose;
        this.GrauImportancia = grau;
        this.DataFim = dataFim;

        // Mantém os horários que continuam, para não apagar e voltar a inserir a mesma hora (UQ_HorarioToma_TratamentoHora).
        this.horarios.RemoveAll(h => !novas.Contains(h.HoraLocal));
        foreach (var hora in novas.Where(h => !this.horarios.Exists(x => x.HoraLocal == h)))
        {
            this.horarios.Add(new HorarioToma(this.Id, hora));
        }
    }
}
