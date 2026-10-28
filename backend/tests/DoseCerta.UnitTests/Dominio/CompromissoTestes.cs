// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários de <see cref="Compromisso"/> e da antecedência do aviso (RF-080, RF-081, RF-116, RF-117).
/// </summary>
public class CompromissoTestes
{
    private static readonly DateTime DataHora = new(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime Agora = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// RF-116: só são aceites as antecedências de 30 minutos, 1, 2, 24 e 48 horas.
    /// </summary>
    /// <param name="minutos">Antecedência, em minutos.</param>
    /// <param name="valida">Se deve ser aceite.</param>
    [Theory]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(120, true)]
    [InlineData(1440, true)]
    [InlineData(2880, true)]
    [InlineData(0, false)]
    [InlineData(45, false)]
    [InlineData(-30, false)]
    public void AntecedenciaValida(int minutos, bool valida)
    {
        Assert.Equal(valida, ParametrosDeNegocio.AntecedenciaValida(minutos));
        if (valida)
        {
            Assert.Equal(minutos, Novo(minutos).AntecedenciaAvisoMin);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Novo(minutos));
        }
    }

    /// <summary>
    /// RF-117: o aviso é devido a partir da data e hora menos a antecedência, e uma só vez.
    /// </summary>
    [Fact]
    public void AvisoDevidoDesdeOInstanteDoAvisoAteSerEnviado()
    {
        var compromisso = Novo(60);

        Assert.Equal(DataHora.AddHours(-1), compromisso.InstanteAviso());
        Assert.False(compromisso.AvisoDevido(DataHora.AddHours(-1).AddSeconds(-1)));
        Assert.True(compromisso.AvisoDevido(DataHora.AddHours(-1)));

        compromisso.MarcarAvisoEnviado(DataHora.AddHours(-1));

        Assert.False(compromisso.AvisoDevido(DataHora));
        Assert.Equal(DataHora.AddHours(-1), compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RF-117: registado depois do instante do aviso, o aviso fica devido de imediato.
    /// </summary>
    [Fact]
    public void AvisoJaPassadoFicaDevidoDeImediato()
    {
        var compromisso = Novo(2880);

        Assert.True(compromisso.AvisoDevido(DataHora.AddHours(-2)));
    }

    /// <summary>
    /// RF-081 e RF-117: ao alterar a data ou a antecedência, o aviso volta a ficar por enviar, com os valores novos.
    /// </summary>
    /// <param name="mudaData">Se a data muda.</param>
    /// <param name="mudaAntecedencia">Se a antecedência muda.</param>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AtualizarDataOuAntecedenciaRepoeOAviso(bool mudaData, bool mudaAntecedencia)
    {
        var compromisso = Novo(60);
        compromisso.MarcarAvisoEnviado(DataHora.AddHours(-1));
        var novaData = mudaData ? DataHora.AddDays(1) : DataHora;
        var novaAntecedencia = mudaAntecedencia ? 120 : 60;

        compromisso.Atualizar(novaData, "Hospital Fictício", "Análises", novaAntecedencia);

        Assert.Null(compromisso.AvisoEnviadoEmUtc);
        Assert.Equal(novaData.AddMinutes(-novaAntecedencia), compromisso.InstanteAviso());
        Assert.Equal("Hospital Fictício", compromisso.Local);
        Assert.Equal("Análises", compromisso.Motivo);
    }

    /// <summary>
    /// Alterar só o local ou o motivo mantém o aviso já enviado.
    /// </summary>
    [Fact]
    public void AtualizarSoLocalEMotivoMantemOAviso()
    {
        var compromisso = Novo(60);
        compromisso.MarcarAvisoEnviado(DataHora.AddHours(-1));

        compromisso.Atualizar(DataHora, "Hospital Fictício", "Análises", 60);

        Assert.Equal(DataHora.AddHours(-1), compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// A antecedência inválida também é recusada na edição, sem alterar o compromisso.
    /// </summary>
    [Fact]
    public void AtualizarComAntecedenciaInvalidaERecusado()
    {
        var compromisso = Novo(60);

        Assert.Throws<ArgumentOutOfRangeException>(() => compromisso.Atualizar(DataHora.AddDays(1), "Outro", "Outro", 45));

        Assert.Equal(DataHora, compromisso.DataHoraUtc);
        Assert.Equal(60, compromisso.AntecedenciaAvisoMin);
    }

    private static Compromisso Novo(int antecedencia)
    {
        return new Compromisso(Guid.NewGuid(), DataHora, "Centro de Saúde Fictício", "Consulta de rotina", antecedencia, Guid.NewGuid(), Agora);
    }
}
