// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Consulta ou compromisso de saúde de uma ficha, apenas para consulta: sem estado de realização e não confirmável (RF-080, RF-087).
/// Tem a antecedência do aviso definida por compromisso (RF-116); o aviso é enviado pelo processo periódico (F011).
/// </summary>
public class Compromisso
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Compromisso"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="dataHoraUtc">Data e hora, em UTC.</param>
    /// <param name="local">Local.</param>
    /// <param name="motivo">Motivo.</param>
    /// <param name="antecedenciaAvisoMin">Antecedência do aviso, em minutos.</param>
    /// <param name="criadoPorId">Utilizador que regista.</param>
    /// <param name="criadoEmUtc">Instante do registo, em UTC.</param>
    public Compromisso(
        Guid fichaId,
        DateTime dataHoraUtc,
        string local,
        string motivo,
        int antecedenciaAvisoMin,
        Guid criadoPorId,
        DateTime criadoEmUtc)
    {
        ExigirAntecedenciaValida(antecedenciaAvisoMin);
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.DataHoraUtc = dataHoraUtc;
        this.Local = local;
        this.Motivo = motivo;
        this.AntecedenciaAvisoMin = antecedenciaAvisoMin;
        this.CriadoPorId = criadoPorId;
        this.CriadoEmUtc = criadoEmUtc;
    }

    private Compromisso()
    {
    }

    /// <summary>
    /// Gets o identificador do compromisso.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets a data e hora, em UTC.
    /// </summary>
    public DateTime DataHoraUtc { get; private set; }

    /// <summary>
    /// Gets o local.
    /// </summary>
    public string Local { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o motivo.
    /// </summary>
    public string Motivo { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a antecedência do aviso, em minutos (RF-116).
    /// </summary>
    public int AntecedenciaAvisoMin { get; private set; }

    /// <summary>
    /// Gets o instante do aviso, em UTC.
    /// </summary>
    public DateTime? AvisoEnviadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que registou.
    /// </summary>
    public Guid CriadoPorId { get; private set; }

    /// <summary>
    /// Gets o instante do registo, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }

    /// <summary>
    /// Altera os dados do compromisso. Se a data ou a antecedência mudarem, o aviso volta a ficar por enviar e passa a ser
    /// calculado com os valores novos (RF-081, RF-117).
    /// </summary>
    /// <param name="dataHoraUtc">Data e hora, em UTC.</param>
    /// <param name="local">Local.</param>
    /// <param name="motivo">Motivo.</param>
    /// <param name="antecedenciaAvisoMin">Antecedência do aviso, em minutos.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a antecedência não for um dos valores permitidos.</exception>
    public void Atualizar(DateTime dataHoraUtc, string local, string motivo, int antecedenciaAvisoMin)
    {
        ExigirAntecedenciaValida(antecedenciaAvisoMin);
        if (dataHoraUtc != this.DataHoraUtc || antecedenciaAvisoMin != this.AntecedenciaAvisoMin)
        {
            this.AvisoEnviadoEmUtc = null;
        }

        this.DataHoraUtc = dataHoraUtc;
        this.Local = local;
        this.Motivo = motivo;
        this.AntecedenciaAvisoMin = antecedenciaAvisoMin;
    }

    /// <summary>
    /// Obtém o instante do aviso: a data e hora menos a antecedência (RF-117).
    /// </summary>
    /// <returns>O instante do aviso, em UTC.</returns>
    public DateTime InstanteAviso()
    {
        return this.DataHoraUtc.AddMinutes(-this.AntecedenciaAvisoMin);
    }

    /// <summary>
    /// Indica se o aviso já devia ser enviado: o instante do aviso chegou e ainda não foi enviado. Um compromisso registado
    /// depois desse instante tem o aviso devido de imediato (RF-117).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <returns><see langword="true"/> se o aviso estiver por enviar e o instante do aviso tiver chegado.</returns>
    public bool AvisoDevido(DateTime agoraUtc)
    {
        return this.AvisoEnviadoEmUtc is null && agoraUtc >= this.InstanteAviso();
    }

    /// <summary>
    /// Regista o envio do aviso, para não ser enviado de novo (RNF-043).
    /// </summary>
    /// <param name="agoraUtc">Instante do envio, em UTC.</param>
    public void MarcarAvisoEnviado(DateTime agoraUtc)
    {
        this.AvisoEnviadoEmUtc = agoraUtc;
    }

    private static void ExigirAntecedenciaValida(int antecedenciaAvisoMin)
    {
        if (!ParametrosDeNegocio.AntecedenciaValida(antecedenciaAvisoMin))
        {
            throw new ArgumentOutOfRangeException(
                nameof(antecedenciaAvisoMin),
                antecedenciaAvisoMin,
                "A antecedência do aviso tem de ser 30, 60, 120, 1440 ou 2880 minutos.");
        }
    }
}
