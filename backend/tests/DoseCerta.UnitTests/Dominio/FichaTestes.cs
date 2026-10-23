// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários das regras do agregado <see cref="Ficha"/> (F002, F003).
/// </summary>
public class FichaTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Criador = Guid.NewGuid();

    /// <summary>
    /// RF-024: o criador fica associado como Cuidador principal, não provisório.
    /// </summary>
    [Fact]
    public void AssociarCuidadorPrincipalMarcaOPrincipal()
    {
        var ficha = NovaFicha();

        var associacao = ficha.AssociarCuidadorPrincipal(Criador, provisorio: false, Agora);

        Assert.Same(associacao, ficha.ObterAssociacao(Criador));
        Assert.Equal(Papel.Cuidador, associacao.Papel);
        Assert.True(associacao.EPrincipal);
        Assert.False(associacao.EProvisorio);
        Assert.True(associacao.EhCuidador());
        Assert.True(associacao.PodeEscrever());
        Assert.Equal(ficha.Id, associacao.FichaId);
    }

    /// <summary>
    /// RF-025: com convite para Cuidador principal, o criador fica Cuidador principal provisório.
    /// </summary>
    [Fact]
    public void AssociarCuidadorPrincipalProvisorio()
    {
        var ficha = NovaFicha();

        var associacao = ficha.AssociarCuidadorPrincipal(Criador, provisorio: true, Agora);

        Assert.True(associacao.EPrincipal);
        Assert.True(associacao.EProvisorio);
    }

    /// <summary>
    /// RF-132: a ficha nunca tem dois Cuidadores principais e um Utilizador só tem uma associação.
    /// </summary>
    [Fact]
    public void NaoAceitaSegundoPrincipalNemAssociacaoRepetida()
    {
        var ficha = NovaFicha();
        ficha.AssociarCuidadorPrincipal(Criador, provisorio: false, Agora);

        Assert.Throws<InvalidOperationException>(() => ficha.AssociarCuidadorPrincipal(Guid.NewGuid(), provisorio: false, Agora));
        Assert.Throws<InvalidOperationException>(() => ficha.Associar(Criador, Papel.Familiar, Agora));
    }

    /// <summary>
    /// RF-034: na ficha própria o titular nunca é Cuidador principal provisório.
    /// </summary>
    [Fact]
    public void FichaPropriaNaoTemPrincipalProvisorio()
    {
        var ficha = NovaFicha(propria: true);

        Assert.True(ficha.EProprio());
        Assert.Throws<InvalidOperationException>(() => ficha.AssociarCuidadorPrincipal(Criador, provisorio: true, Agora));
    }

    /// <summary>
    /// RF-048: o Familiar está associado mas não pode escrever.
    /// </summary>
    [Fact]
    public void FamiliarAssociadoNaoPodeEscrever()
    {
        var ficha = NovaFicha();
        var familiar = Guid.NewGuid();

        var associacao = ficha.Associar(familiar, Papel.Familiar, Agora);

        Assert.Same(associacao, ficha.ObterAssociacao(familiar));
        Assert.False(associacao.EPrincipal);
        Assert.False(associacao.EhCuidador());
        Assert.False(associacao.PodeEscrever());
        Assert.Null(ficha.ObterAssociacao(Guid.NewGuid()));
    }

    /// <summary>
    /// RF-120 e RF-121: arquivar e reativar mudam o estado e o instante a partir do qual a ficha está ativa (DM-19).
    /// </summary>
    [Fact]
    public void ArquivarEReativarAtualizamOEstado()
    {
        var ficha = NovaFicha();
        var arquivo = Agora.AddDays(1);
        var reativacao = Agora.AddDays(2);

        ficha.Arquivar(arquivo);

        Assert.False(ficha.EstaAtiva());
        Assert.Equal(EstadoFicha.Arquivada, ficha.Estado);
        Assert.Equal(arquivo, ficha.ArquivadaEmUtc);
        var erro = Assert.Throws<DominioException>(ficha.ExigirAtiva);
        Assert.Equal(CodigosErro.FichaArquivada, erro.Codigo);

        ficha.Reativar(reativacao);

        Assert.True(ficha.EstaAtiva());
        Assert.Null(ficha.ArquivadaEmUtc);
        Assert.Equal(reativacao, ficha.AtivaDesdeUtc);
        ficha.ExigirAtiva();
    }

    /// <summary>
    /// OP-12 e OP-13: arquivar uma ficha arquivada e reativar uma ficha ativa são recusados.
    /// </summary>
    [Fact]
    public void ArquivoEReativacaoRepetidosSaoRecusados()
    {
        var ficha = NovaFicha();

        var naoArquivada = Assert.Throws<DominioException>(() => ficha.Reativar(Agora));
        ficha.Arquivar(Agora);
        var jaArquivada = Assert.Throws<DominioException>(() => ficha.Arquivar(Agora));

        Assert.Equal(CodigosErro.FichaNaoArquivada, naoArquivada.Codigo);
        Assert.Equal(CodigosErro.FichaJaArquivada, jaArquivada.Codigo);
    }

    /// <summary>
    /// RF-021 e RF-029: os dados e as necessidades são substituídos, sem valores repetidos.
    /// </summary>
    [Fact]
    public void AtualizarDadosEDefinirNecessidades()
    {
        var ficha = NovaFicha();
        var diabetes = new CondicaoSaude(1, "Diabetes");
        var grau = new GrauAutonomia(2, "Autonomia parcial", 2);

        ficha.AtualizarDados("Maria Fictícia", new DateOnly(1950, 1, 2), "+351 910 000 002");
        ficha.DefinirNecessidades(grau, new[] { diabetes, diabetes }, new[] { new Alergia(1, "Medicamentos") });

        Assert.Equal("Maria Fictícia", ficha.NomeCompleto);
        Assert.Equal(new DateOnly(1950, 1, 2), ficha.DataNascimento);
        Assert.Equal("+351 910 000 002", ficha.ContactoEmergencia);
        Assert.Equal((short)2, ficha.GrauAutonomiaId);
        Assert.Same(grau, ficha.GrauAutonomia);
        Assert.Single(ficha.CondicoesSaude);
        Assert.Single(ficha.Alergias);

        ficha.DefinirNecessidades(null, Array.Empty<CondicaoSaude>(), Array.Empty<Alergia>());

        Assert.Null(ficha.GrauAutonomiaId);
        Assert.Empty(ficha.CondicoesSaude);
        Assert.Empty(ficha.Alergias);
    }

    /// <summary>
    /// RF-123: a ficha só pode ser eliminada sem medicação nem histórico.
    /// </summary>
    /// <param name="medicacao">Tem medicação.</param>
    /// <param name="historico">Tem histórico.</param>
    /// <param name="esperado">Resultado esperado.</param>
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void PodeSerEliminadaSoSemRegistos(bool medicacao, bool historico, bool esperado)
    {
        Assert.Equal(esperado, Ficha.PodeSerEliminada(medicacao, historico));
    }

    private static Ficha NovaFicha(bool propria = false)
    {
        return new Ficha("Utente Fictício", new DateOnly(1941, 5, 17), "+351 910 000 001", propria ? Criador : null, Criador, Agora);
    }
}
