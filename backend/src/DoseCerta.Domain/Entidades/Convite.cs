// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Convite para uma ficha dirigido a um e-mail, com o papel oferecido e, se for o caso, para Cuidador principal. Não cria associação até ser aceite (DM-04, DM-05).
/// </summary>
public class Convite
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Convite"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="emailConvidado">E-mail da pessoa convidada.</param>
    /// <param name="papelOferecido">Papel oferecido.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal.</param>
    /// <param name="convidadoPorId">Utilizador que convida.</param>
    /// <param name="criadoEmUtc">Instante de criação, em UTC.</param>
    public Convite(
        Guid fichaId,
        string emailConvidado,
        Papel papelOferecido,
        bool paraPrincipal,
        Guid convidadoPorId,
        DateTime criadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.EmailConvidado = emailConvidado;
        this.PapelOferecido = papelOferecido;
        this.ParaPrincipal = paraPrincipal;
        this.ConvidadoPorId = convidadoPorId;
        this.Estado = EstadoConvite.Pendente;
        this.CriadoEmUtc = criadoEmUtc;
    }

    private Convite()
    {
    }

    /// <summary>
    /// Gets o identificador do convite.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets o e-mail da pessoa convidada.
    /// </summary>
    public string EmailConvidado { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o papel oferecido.
    /// </summary>
    public Papel PapelOferecido { get; private set; }

    /// <summary>
    /// Gets a value indicating whether o convite oferece o papel de Cuidador principal.
    /// </summary>
    public bool ParaPrincipal { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que convidou.
    /// </summary>
    public Guid ConvidadoPorId { get; private set; }

    /// <summary>
    /// Gets o estado do convite.
    /// </summary>
    public EstadoConvite Estado { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que respondeu ao convite.
    /// </summary>
    public Guid? UtilizadorConvidadoId { get; private set; }

    /// <summary>
    /// Gets o instante de criação, em UTC.
    /// </summary>
    public DateTime CriadoEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da resposta, em UTC.
    /// </summary>
    public DateTime? RespondidoEmUtc { get; private set; }

    /// <summary>
    /// Indica se o convite é dirigido a um e-mail, sem distinguir maiúsculas, como a collation da coluna (RF-042, RF-044).
    /// </summary>
    /// <param name="email">E-mail da conta do Utilizador.</param>
    /// <returns><see langword="true"/> se o convite for para esse e-mail.</returns>
    public bool DirigidoA(string? email)
    {
        return string.Equals(this.EmailConvidado, email?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Aceita o convite: deixa de estar pendente e regista quem respondeu e quando (RF-045, <c>CK_Convite_Resposta</c>).
    /// A associação à ficha é criada pela ficha.
    /// </summary>
    /// <param name="utilizadorId">Utilizador que aceita.</param>
    /// <param name="agoraUtc">Instante da resposta, em UTC.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.ConviteJaRespondido"/>, se o convite já não estiver pendente.</exception>
    public void Aceitar(Guid utilizadorId, DateTime agoraUtc)
    {
        this.Responder(EstadoConvite.Aceite, utilizadorId, agoraUtc);
    }

    /// <summary>
    /// Recusa o convite: deixa de estar pendente, sem associação à ficha, e o mesmo e-mail pode voltar a ser convidado (RF-043, RF-046).
    /// </summary>
    /// <param name="utilizadorId">Utilizador que recusa.</param>
    /// <param name="agoraUtc">Instante da resposta, em UTC.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.ConviteJaRespondido"/>, se o convite já não estiver pendente.</exception>
    public void Recusar(Guid utilizadorId, DateTime agoraUtc)
    {
        this.Responder(EstadoConvite.Recusado, utilizadorId, agoraUtc);
    }

    private void Responder(EstadoConvite resposta, Guid utilizadorId, DateTime agoraUtc)
    {
        if (this.Estado != EstadoConvite.Pendente)
        {
            throw new DominioException(CodigosErro.ConviteJaRespondido, "O convite já foi aceite ou recusado.");
        }

        this.Estado = resposta;
        this.UtilizadorConvidadoId = utilizadorId;
        this.RespondidoEmUtc = agoraUtc;
    }
}
