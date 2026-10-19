// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Notificação dentro da aplicação, uma por destinatário e por evento, criada sempre mesmo que o e-mail falhe (RF-141, DM-12).
/// </summary>
public class Notificacao
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Notificacao"/> class.
    /// </summary>
    /// <param name="destinatarioId">Destinatário.</param>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="tipo">Tipo de notificação.</param>
    /// <param name="mensagem">Mensagem.</param>
    /// <param name="referenciaTipo">Tipo do registo de origem.</param>
    /// <param name="referenciaId">Identificador do registo de origem.</param>
    /// <param name="instanteLimiteUtc">Instante-limite, em UTC.</param>
    /// <param name="criadaEmUtc">Instante de criação, em UTC.</param>
    public Notificacao(
        Guid destinatarioId,
        Guid fichaId,
        TipoNotificacao tipo,
        string mensagem,
        TipoReferencia referenciaTipo,
        Guid referenciaId,
        DateTime instanteLimiteUtc,
        DateTime criadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.DestinatarioId = destinatarioId;
        this.FichaId = fichaId;
        this.Tipo = tipo;
        this.Mensagem = mensagem;
        this.ReferenciaTipo = referenciaTipo;
        this.ReferenciaId = referenciaId;
        this.InstanteLimiteUtc = instanteLimiteUtc;
        this.CriadaEmUtc = criadaEmUtc;
    }

    private Notificacao()
    {
    }

    /// <summary>
    /// Gets o identificador da notificação.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador do destinatário.
    /// </summary>
    public Guid DestinatarioId { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o tipo de notificação.
    /// </summary>
    public TipoNotificacao Tipo { get; private set; }

    /// <summary>
    /// Gets a mensagem em português europeu.
    /// </summary>
    public string Mensagem { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o tipo do registo de origem.
    /// </summary>
    public TipoReferencia ReferenciaTipo { get; private set; }

    /// <summary>
    /// Gets o identificador do registo de origem.
    /// </summary>
    public Guid ReferenciaId { get; private set; }

    /// <summary>
    /// Gets o instante-limite que originou a notificação, em UTC (DM-20).
    /// </summary>
    public DateTime InstanteLimiteUtc { get; private set; }

    /// <summary>
    /// Gets o instante de criação, em UTC.
    /// </summary>
    public DateTime CriadaEmUtc { get; private set; }
}
