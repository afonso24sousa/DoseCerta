// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários dos estados iniciais e das invariantes de criação das entidades do modelo de dados (I030).
/// </summary>
public class EntidadesTestes
{
    private static readonly DateTime Agora = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Uma ficha de terceiro fica ativa desde a criação e não tem titular (DM-19, CK_Ficha_Tipo).
    /// </summary>
    [Fact]
    public void FichaDeTerceiroFicaAtivaSemTitular()
    {
        var criador = Guid.NewGuid();

        var ficha = new Ficha("Utente Fictício", new DateOnly(1941, 5, 17), "+351 910 000 001", null, criador, Agora);

        Assert.NotEqual(Guid.Empty, ficha.Id);
        Assert.Equal(TipoFicha.Terceira, ficha.Tipo);
        Assert.Null(ficha.UtilizadorProprioId);
        Assert.Equal(EstadoFicha.Ativa, ficha.Estado);
        Assert.Equal(criador, ficha.CriadaPorId);
        Assert.Equal(Agora, ficha.CriadaEmUtc);
        Assert.Equal(Agora, ficha.AtivaDesdeUtc);
        Assert.Null(ficha.ArquivadaEmUtc);
        Assert.Empty(ficha.Associacoes);
        Assert.Empty(ficha.CondicoesSaude);
        Assert.Empty(ficha.Alergias);
    }

    /// <summary>
    /// Uma ficha com titular é a ficha própria desse Utilizador (CK_Ficha_Tipo).
    /// </summary>
    [Fact]
    public void FichaComTitularEPropria()
    {
        var titular = Guid.NewGuid();

        var ficha = new Ficha("Pessoa Fictícia", new DateOnly(1985, 3, 2), "+351 910 000 002", titular, titular, Agora);

        Assert.Equal(TipoFicha.Propria, ficha.Tipo);
        Assert.Equal(titular, ficha.UtilizadorProprioId);
    }

    /// <summary>
    /// Uma associação nova não é a do Cuidador principal (valores predefinidos de EPrincipal e EProvisorio).
    /// </summary>
    [Fact]
    public void AssociacaoNovaNaoEPrincipal()
    {
        var associacao = new Associacao(Guid.NewGuid(), Guid.NewGuid(), Papel.Familiar, Agora);

        Assert.Equal(Papel.Familiar, associacao.Papel);
        Assert.False(associacao.EPrincipal);
        Assert.False(associacao.EProvisorio);
    }

    /// <summary>
    /// Um convite novo fica pendente, sem resposta (CK_Convite_Resposta).
    /// </summary>
    [Fact]
    public void ConviteNovoFicaPendente()
    {
        var convite = new Convite(Guid.NewGuid(), "convidado@exemplo.pt", Papel.Cuidador, true, Guid.NewGuid(), Agora);

        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.True(convite.ParaPrincipal);
        Assert.Null(convite.UtilizadorConvidadoId);
        Assert.Null(convite.RespondidoEmUtc);
    }

    /// <summary>
    /// Um e-mail tem de ter origem numa notificação ou num convite (CK_EnvioEmail_Origem).
    /// </summary>
    [Fact]
    public void EnvioEmailSemOrigemERecusado()
    {
        Assert.Throws<ArgumentException>(() => new EnvioEmail(null, null, "destino@exemplo.pt", "Assunto", Agora));
    }

    /// <summary>
    /// Um e-mail novo fica pendente, sem reenvios, com a primeira tentativa marcada para o instante da criação.
    /// </summary>
    [Fact]
    public void EnvioEmailNovoFicaPendente()
    {
        var conviteId = Guid.NewGuid();

        var envio = new EnvioEmail(null, conviteId, "destino@exemplo.pt", "Convite fictício", Agora);

        Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
        Assert.Equal(conviteId, envio.ConviteId);
        Assert.Equal(0, envio.Reenvios);
        Assert.Equal(Agora, envio.ProximaTentativaEmUtc);
        Assert.Null(envio.PrimeiraTentativaEmUtc);
    }

    /// <summary>
    /// Os registos gerados começam nos estados iniciais do modelo de dados (toma pendente e tratamento em curso).
    /// </summary>
    [Fact]
    public void RegistosNovosComecamNoEstadoInicial()
    {
        var fichaId = Guid.NewGuid();
        var autor = Guid.NewGuid();

        var tratamento = new Tratamento(fichaId, "Medicamento fictício", "1 comprimido", GrauImportancia.Grave, new DateOnly(2026, 10, 1), null, new[] { new TimeOnly(8, 0) }, autor, Agora);
        var toma = new Toma(tratamento.Id, Agora, tratamento.Medicamento, tratamento.Dose, tratamento.GrauImportancia);

        Assert.Equal(EstadoTratamento.EmCurso, tratamento.Estado);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal("Medicamento fictício", toma.Medicamento);
    }
}
