// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
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

    /// <summary>
    /// Indica se o e-mail está pendente e a próxima tentativa já chegou.
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <returns><see langword="true"/> se for altura de tentar o envio.</returns>
    public bool Devido(DateTime agoraUtc)
    {
        return this.Estado == EstadoEnvioEmail.Pendente && this.ProximaTentativaEmUtc <= agoraUtc;
    }

    /// <summary>
    /// Indica se, depois de uma falha, ainda há reenvios disponíveis (no máximo <see cref="ParametrosDeNegocio.ReenviosMaximos"/>).
    /// </summary>
    /// <returns><see langword="true"/> se ainda puder ser reenviado.</returns>
    public bool PodeReenviar()
    {
        return this.Reenvios < ParametrosDeNegocio.ReenviosMaximos;
    }

    /// <summary>
    /// Regista o início de uma tentativa de envio: a primeira guarda o instante da primeira tentativa (RNF-018); as seguintes
    /// contam como reenvios (RF-136).
    /// </summary>
    /// <param name="agoraUtc">Instante da tentativa, em UTC.</param>
    /// <exception cref="InvalidOperationException">Se o e-mail não estiver pendente ou a próxima tentativa ainda não tiver chegado.</exception>
    public void IniciarTentativa(DateTime agoraUtc)
    {
        if (!this.Devido(agoraUtc))
        {
            throw new InvalidOperationException("O e-mail não tem nenhuma tentativa de envio devida.");
        }

        if (this.PrimeiraTentativaEmUtc is null)
        {
            this.PrimeiraTentativaEmUtc = agoraUtc;
        }
        else
        {
            this.Reenvios++;
        }

        this.UltimaTentativaEmUtc = agoraUtc;
    }

    /// <summary>
    /// Marca o e-mail como enviado: o serviço de envio aceitou-o e não há mais tentativas.
    /// </summary>
    /// <param name="agoraUtc">Instante da tentativa bem-sucedida, em UTC.</param>
    /// <exception cref="InvalidOperationException">Se o e-mail não estiver pendente.</exception>
    public void MarcarEnviado(DateTime agoraUtc)
    {
        this.ExigirPendente();
        this.Estado = EstadoEnvioEmail.Enviado;
        this.UltimaTentativaEmUtc = agoraUtc;
        this.ProximaTentativaEmUtc = null;
    }

    /// <summary>
    /// Regista a falha de uma tentativa: agenda o reenvio <see cref="ParametrosDeNegocio.IntervaloReenvio"/> depois ou, se os
    /// reenvios já se esgotaram, marca o e-mail como não entregue (RF-119, RF-136). A falha nunca altera o convite nem a notificação.
    /// </summary>
    /// <param name="agoraUtc">Instante da tentativa falhada, em UTC.</param>
    /// <exception cref="InvalidOperationException">Se o e-mail não estiver pendente.</exception>
    public void RegistarFalha(DateTime agoraUtc)
    {
        this.ExigirPendente();
        this.UltimaTentativaEmUtc = agoraUtc;
        if (this.PodeReenviar())
        {
            this.ProximaTentativaEmUtc = agoraUtc + ParametrosDeNegocio.IntervaloReenvio;
        }
        else
        {
            this.Estado = EstadoEnvioEmail.NaoEntregue;
            this.ProximaTentativaEmUtc = null;
        }
    }

    private void ExigirPendente()
    {
        if (this.Estado != EstadoEnvioEmail.Pendente)
        {
            throw new InvalidOperationException("O envio do e-mail já terminou.");
        }
    }
}
