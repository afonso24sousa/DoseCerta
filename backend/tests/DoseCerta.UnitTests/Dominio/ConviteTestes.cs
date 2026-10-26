// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários da resposta a um <see cref="Convite"/> e do destinatário do convite (F004, RF-042 a RF-046).
/// </summary>
public class ConviteTestes
{
    private static readonly DateTime Criado = new(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Respondido = Criado.AddHours(2);
    private static readonly Guid Convidado = Guid.NewGuid();

    /// <summary>
    /// RF-045 CA1: aceitar deixa o convite aceite, com quem respondeu e quando; o papel oferecido não muda.
    /// </summary>
    [Fact]
    public void AceitarDeixaOConviteAceite()
    {
        var convite = NovoConvite();

        convite.Aceitar(Convidado, Respondido);

        Assert.Equal(EstadoConvite.Aceite, convite.Estado);
        Assert.Equal(Convidado, convite.UtilizadorConvidadoId);
        Assert.Equal(Respondido, convite.RespondidoEmUtc);
        Assert.Equal(Papel.Familiar, convite.PapelOferecido);
    }

    /// <summary>
    /// RF-046 CA1: recusar deixa o convite recusado, com quem respondeu e quando.
    /// </summary>
    [Fact]
    public void RecusarDeixaOConviteRecusado()
    {
        var convite = NovoConvite();

        convite.Recusar(Convidado, Respondido);

        Assert.Equal(EstadoConvite.Recusado, convite.Estado);
        Assert.Equal(Convidado, convite.UtilizadorConvidadoId);
        Assert.Equal(Respondido, convite.RespondidoEmUtc);
    }

    /// <summary>
    /// UC-14 E1: um convite já aceite ou recusado não pode ser respondido outra vez, e a primeira resposta mantém-se.
    /// </summary>
    /// <param name="primeiraAceita">Indica se a primeira resposta é aceitar.</param>
    /// <param name="segundaAceita">Indica se a segunda resposta é aceitar.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void SegundaRespostaERecusada(bool primeiraAceita, bool segundaAceita)
    {
        var convite = NovoConvite();
        Responder(convite, primeiraAceita, Respondido);
        var estadoDepoisDaPrimeira = convite.Estado;

        var erro = Assert.Throws<DominioException>(() => Responder(convite, segundaAceita, Respondido.AddMinutes(5)));

        Assert.Equal(CodigosErro.ConviteJaRespondido, erro.Codigo);
        Assert.False(string.IsNullOrWhiteSpace(erro.Message));
        Assert.Equal(estadoDepoisDaPrimeira, convite.Estado);
        Assert.Equal(Respondido, convite.RespondidoEmUtc);
    }

    /// <summary>
    /// RF-042, RF-044: o convite é dirigido ao e-mail convidado, sem distinguir maiúsculas nem espaços nas pontas, como a collation da coluna.
    /// </summary>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="esperado">Resultado esperado.</param>
    [Theory]
    [InlineData("rui@exemplo.pt", true)]
    [InlineData("Rui@Exemplo.PT", true)]
    [InlineData(" rui@exemplo.pt ", true)]
    [InlineData("ana@exemplo.pt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void DirigidoAComparaOEmailSemDistinguirMaiusculas(string? email, bool esperado)
    {
        var convite = NovoConvite();

        Assert.Equal(esperado, convite.DirigidoA(email));
    }

    private static Convite NovoConvite()
    {
        return new Convite(Guid.NewGuid(), "rui@exemplo.pt", Papel.Familiar, paraPrincipal: false, Guid.NewGuid(), Criado);
    }

    private static void Responder(Convite convite, bool aceitar, DateTime instante)
    {
        if (aceitar)
        {
            convite.Aceitar(Convidado, instante);
        }
        else
        {
            convite.Recusar(Convidado, instante);
        }
    }
}
