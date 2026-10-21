// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários do ciclo de vida de um <see cref="EnvioEmail"/>: tentativas, reenvios e não entregue (RF-118, RF-119, RF-136, RNF-018).
/// </summary>
public class EnvioEmailTestes
{
    private static readonly DateTime Criado = new(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// RNF-018: o e-mail novo fica pendente e devido de imediato, sem tentativas nem reenvios.
    /// </summary>
    [Fact]
    public void EmailNovoFicaPendenteEDevidoDeImediato()
    {
        var envio = NovoEnvio();

        Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
        Assert.Equal(0, envio.Reenvios);
        Assert.Equal(Criado, envio.ProximaTentativaEmUtc);
        Assert.Null(envio.PrimeiraTentativaEmUtc);
        Assert.False(envio.Devido(Criado.AddSeconds(-1)));
        Assert.True(envio.Devido(Criado));
    }

    /// <summary>
    /// RNF-018: a primeira tentativa bem-sucedida deixa o e-mail enviado, com o instante da primeira tentativa, sem reenvios.
    /// </summary>
    [Fact]
    public void PrimeiraTentativaComSucessoDeixaEnviado()
    {
        var envio = NovoEnvio();
        var tentativa = Criado.AddSeconds(20);

        envio.IniciarTentativa(tentativa);
        envio.MarcarEnviado(tentativa);

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Equal(tentativa, envio.PrimeiraTentativaEmUtc);
        Assert.Equal(tentativa, envio.UltimaTentativaEmUtc);
        Assert.Null(envio.ProximaTentativaEmUtc);
        Assert.Equal(0, envio.Reenvios);
        Assert.False(envio.Devido(tentativa.AddDays(1)));
    }

    /// <summary>
    /// RF-136 CA1 e CA2: depois da primeira falha há no máximo três reenvios, com 5 minutos de intervalo; se o terceiro reenvio
    /// também falhar, o e-mail fica não entregue e não há mais tentativas.
    /// </summary>
    [Fact]
    public void TresReenviosFalhadosDeixamNaoEntregue()
    {
        var envio = NovoEnvio();
        var instante = Criado;

        for (var tentativa = 0; tentativa <= ParametrosDeNegocio.ReenviosMaximos; tentativa++)
        {
            Assert.True(envio.Devido(instante));
            Assert.False(envio.Devido(instante.AddSeconds(-1)) && tentativa > 0);
            envio.IniciarTentativa(instante);
            Assert.Equal(tentativa, envio.Reenvios);
            envio.RegistarFalha(instante);
            instante += ParametrosDeNegocio.IntervaloReenvio;
        }

        Assert.Equal(EstadoEnvioEmail.NaoEntregue, envio.Estado);
        Assert.Equal(3, envio.Reenvios);
        Assert.Null(envio.ProximaTentativaEmUtc);
        Assert.Equal(Criado, envio.PrimeiraTentativaEmUtc);
        Assert.Equal(Criado.AddMinutes(15), envio.UltimaTentativaEmUtc);
        Assert.False(envio.Devido(instante.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => envio.IniciarTentativa(instante.AddDays(1)));
    }

    /// <summary>
    /// RF-136: uma falha agenda o reenvio para 5 minutos depois; antes disso o e-mail não está devido.
    /// </summary>
    [Fact]
    public void FalhaAgendaOReenvioCincoMinutosDepois()
    {
        var envio = NovoEnvio();
        var tentativa = Criado.AddSeconds(30);
        envio.IniciarTentativa(tentativa);

        envio.RegistarFalha(tentativa);

        Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
        Assert.Equal(tentativa.AddMinutes(5), envio.ProximaTentativaEmUtc);
        Assert.False(envio.Devido(tentativa.AddMinutes(5).AddSeconds(-1)));
        Assert.True(envio.Devido(tentativa.AddMinutes(5)));
        Assert.True(envio.PodeReenviar());
    }

    /// <summary>
    /// RF-118: se um reenvio for bem-sucedido, não há mais tentativas; o reenvio conta.
    /// </summary>
    [Fact]
    public void ReenvioComSucessoTerminaOEnvio()
    {
        var envio = NovoEnvio();
        envio.IniciarTentativa(Criado);
        envio.RegistarFalha(Criado);
        var reenvio = Criado.AddMinutes(5);

        envio.IniciarTentativa(reenvio);
        envio.MarcarEnviado(reenvio);

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Equal(1, envio.Reenvios);
        Assert.Equal(reenvio, envio.UltimaTentativaEmUtc);
        Assert.Null(envio.ProximaTentativaEmUtc);
    }

    /// <summary>
    /// Uma tentativa antes do momento devido, ou depois de o envio terminar, é um defeito do chamador.
    /// </summary>
    [Fact]
    public void TentativaForaDeTempoOuDepoisDeTerminarEUmDefeito()
    {
        var envio = NovoEnvio();

        Assert.Throws<InvalidOperationException>(() => envio.IniciarTentativa(Criado.AddSeconds(-1)));
        envio.IniciarTentativa(Criado);
        envio.MarcarEnviado(Criado);
        Assert.Throws<InvalidOperationException>(() => envio.MarcarEnviado(Criado));
        Assert.Throws<InvalidOperationException>(() => envio.RegistarFalha(Criado));
    }

    private static EnvioEmail NovoEnvio()
    {
        return new EnvioEmail(null, Guid.NewGuid(), "rui@exemplo.pt", "DoseCerta: recebeu um convite para uma ficha", Criado);
    }
}
