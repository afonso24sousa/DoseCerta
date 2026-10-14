// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// E-mail a enviar, de uma notificação ou de um convite, com o estado e os reenvios (RF-118, RF-136, DM-13).
/// </summary>
public class EnvioEmail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmail"/> class.
    /// </summary>
    /// <param name="notificacaoId">Notificação de origem, se existir.</param>
    /// <param name="conviteId">Convite de origem, se existir.</param>
    /// <param name="enderecoDestino">Endereço de destino.</param>
    /// <param name="assunto">Assunto do e-mail.</param>
    /// <param name="criadoEmUtc">Instante de criação, em UTC; é também o instante da primeira tentativa.</param>
    public EnvioEmail(
        Guid? notificacaoId,
        Guid? conviteId,
        string enderecoDestino,
        string assunto,
        DateTime criadoEmUtc)
    {
        if (notificacaoId is null && conviteId is null)
        {
            throw new ArgumentException("Um e-mail tem de ter origem numa notificação ou num convite.", nameof(conviteId));
        }

        this.Id = Guid.NewGuid();
        this.NotificacaoId = notificacaoId;
        this.ConviteId = conviteId;
        this.EnderecoDestino = enderecoDestino;
        this.Assunto = assunto;
        this.Estado = EstadoEnvioEmail.Pendente;
        this.CriadoEmUtc = criadoEmUtc;
        this.ProximaTentativaEmUtc = criadoEmUtc;
    }

    private EnvioEmail()
    {
    }

    /// <summary>
    /// Gets o identificador do envio.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da notificação de origem.
    /// </summary>
    public Guid? NotificacaoId { get; private set; }

    /// <summary>
    /// Gets o identificador do convite de origem.
    /// </summary>
    public Guid? ConviteId { get; private set; }

    /// <summary>
    /// Gets o endereço de destino.
    /// </summary>
    public string EnderecoDestino { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o assunto.
    /// </summary>
    public string Assunto { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o estado do envio.
    /// </summary>
    public EstadoEnvioEmail Estado { get; private set; }

    /// <summary>
    /// Gets o número de reenvios já feitos (0 a 3).
    /// </summary>
    public byte Reenvios { get; private set; }

    /// <summary>
    /// Gets o instante de criação, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da primeira tentativa, em UTC.
    /// </summary>
    public DateTime? PrimeiraTentativaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da última tentativa, em UTC.
    /// </summary>
    public DateTime? UltimaTentativaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da próxima tentativa, em UTC, enquanto o envio está pendente.
    /// </summary>
    public DateTime? ProximaTentativaEmUtc { get; private set; }
}
