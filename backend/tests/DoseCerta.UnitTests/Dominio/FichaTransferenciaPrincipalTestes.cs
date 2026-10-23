// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários da transferência do papel de Cuidador principal no agregado <see cref="Ficha"/> (F004, RF-034, RF-132, RF-133; OP-26).
/// </summary>
public class FichaTransferenciaPrincipalTestes
{
    private static readonly Guid Principal = Guid.NewGuid();

    /// <summary>
    /// RF-132 CA1: numa ficha de terceiro com dois Cuidadores, o segundo passa a Cuidador principal, o anterior continua Cuidador
    /// e a ficha tem um único Cuidador principal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferenciaDeixaUmUnicoPrincipal()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var novo = await ficha.TransferirPrincipalAsync(cuidador, SemGravacao);

        Assert.Equal(cuidador, novo.UtilizadorId);
        Assert.True(novo.EPrincipal);
        Assert.False(novo.EProvisorio);
        var anterior = ficha.ObterAssociacao(Principal)!;
        Assert.False(anterior.EPrincipal);
        Assert.Equal(Papel.Cuidador, anterior.Papel);
        Assert.Single(ficha.Associacoes, a => a.EPrincipal);
    }

    /// <summary>
    /// <c>UQ_Associacao_Principal</c>: a gravação a meio é chamada uma só vez, quando o anterior já não tem a marca e o novo ainda
    /// não a tem, para que nenhuma escrita deixe dois Cuidadores principais na base de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GravacaoAMeioAconteceSemNenhumPrincipalMarcado()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);
        var principaisNaGravacao = new List<int>();

        await ficha.TransferirPrincipalAsync(
            cuidador,
            () =>
            {
                principaisNaGravacao.Add(ficha.Associacoes.Count(a => a.EPrincipal));
                return Task.CompletedTask;
            });

        Assert.Equal(0, Assert.Single(principaisNaGravacao));
        Assert.Single(ficha.Associacoes, a => a.EPrincipal);
    }

    /// <summary>
    /// RF-034, UC-16 E1: na ficha própria o papel não se transfere, nem para outro Cuidador nem a pedido do titular; nada é gravado.
    /// </summary>
    /// <param name="paraOTitular">Indica se o destinatário é o próprio titular.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NaFichaPropriaOPapelNaoSeTransfere(bool paraOTitular)
    {
        var ficha = FichasDeTeste.Propria(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);
        var gravacoes = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => ficha.TransferirPrincipalAsync(paraOTitular ? Principal : cuidador, () => Contar(ref gravacoes)));

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.Equal(0, gravacoes);
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
        Assert.False(ficha.ObterAssociacao(cuidador)!.EPrincipal);
    }

    /// <summary>
    /// UC-16 E2: o destinatário tem de ser Cuidador da ficha; um Familiar ou uma pessoa não associada é recusado e nada muda.
    /// </summary>
    /// <param name="associado">Indica se o destinatário está associado à ficha, como Familiar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DestinatarioTemDeSerCuidador(bool associado)
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var destinatario = associado ? FichasDeTeste.Associar(ficha, Papel.Familiar) : Guid.NewGuid();
        var gravacoes = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(() => ficha.TransferirPrincipalAsync(destinatario, () => Contar(ref gravacoes)));

        Assert.Equal(CodigosErro.DestinatarioNaoECuidador, erro.Codigo);
        Assert.Contains("promova-a ou convide-a como Cuidador", erro.Message, StringComparison.Ordinal);
        Assert.Equal(0, gravacoes);
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
    }

    /// <summary>
    /// D7: transferir o papel para quem já é Cuidador principal não altera nada nem grava.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferirParaOPrincipalAtualNaoAlteraNada()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        FichasDeTeste.Associar(ficha, Papel.Cuidador);
        var gravacoes = 0;

        var principal = await ficha.TransferirPrincipalAsync(Principal, () => Contar(ref gravacoes));

        Assert.Equal(Principal, principal.UtilizadorId);
        Assert.True(principal.EPrincipal);
        Assert.Equal(0, gravacoes);
        Assert.Single(ficha.Associacoes, a => a.EPrincipal);
    }

    /// <summary>
    /// RF-133 CA2, R-02: depois da transferência, o anterior Cuidador principal pode ser despromovido ou removido.
    /// </summary>
    /// <param name="remover">Indica se o anterior principal é removido em vez de despromovido.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DepoisDaTransferenciaOAnteriorPodeSair(bool remover)
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);
        await ficha.TransferirPrincipalAsync(cuidador, SemGravacao);

        if (remover)
        {
            ficha.RemoverAcesso(Principal);
            Assert.Null(ficha.ObterAssociacao(Principal));
        }
        else
        {
            Assert.Equal(Papel.Familiar, ficha.AlterarPapel(Principal, Papel.Familiar).Papel);
        }

        Assert.True(ficha.ObterAssociacao(cuidador)!.EPrincipal);
    }

    /// <summary>
    /// D3: com um convite para Cuidador principal pendente, a transferência leva a marca de provisório para o novo Cuidador principal,
    /// para que a aceitação do convite continue a substituir o principal provisório.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TransferenciaComPrincipalProvisorioPassaAMarca()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal, provisorio: true);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var novo = await ficha.TransferirPrincipalAsync(cuidador, SemGravacao);

        Assert.True(novo.EPrincipal);
        Assert.True(novo.EProvisorio);
        var anterior = ficha.ObterAssociacao(Principal)!;
        Assert.False(anterior.EPrincipal);
        Assert.False(anterior.EProvisorio);
    }

    private static Task SemGravacao()
    {
        return Task.CompletedTask;
    }

    private static Task Contar(ref int gravacoes)
    {
        gravacoes++;
        return Task.CompletedTask;
    }
}
