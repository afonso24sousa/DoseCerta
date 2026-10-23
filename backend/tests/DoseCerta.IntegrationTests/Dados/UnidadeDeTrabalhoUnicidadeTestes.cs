// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da conversão das violações de unicidade pela unidade de trabalho, sobre o SQL Server (RF-031, RNF-046).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class UnidadeDeTrabalhoUnicidadeTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoUnicidadeTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public UnidadeDeTrabalhoUnicidadeTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// Uma segunda ficha própria recusada pelo índice <c>UQ_Ficha_UtilizadorProprio</c> dá <c>FICHA_PROPRIA_JA_EXISTE</c>, não grava nada
    /// e executa as compensações, quer a gravação seja feita dentro da operação, quer na confirmação.
    /// </summary>
    /// <param name="gravarDentroDaOperacao">Indica se a operação grava antes da confirmação.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ViolacaoDaFichaPropriaDaErroDeNegocio(bool gravarDentroDaOperacao)
    {
        var titular = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        await this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, titular.Id, propria: true));
        var compensada = false;

        DominioException erro;
        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            erro = await Assert.ThrowsAsync<DominioException>(() => unidade.ExecutarEmTransacaoAsync(
                async () =>
                {
                    unidade.AoFalhar(() => compensada = true);
                    var agora = DadosTeste.Agora();
                    var ficha = new Ficha("Pessoa Fictícia", new DateOnly(1980, 2, 29), "+351 910 000 003", titular.Id, titular.Id, agora);
                    ficha.AssociarCuidadorPrincipal(titular.Id, provisorio: false, agora);
                    contexto.Fichas.Add(ficha);
                    if (gravarDentroDaOperacao)
                    {
                        await unidade.GravarAlteracoesAsync(CancellationToken.None);
                    }

                    return true;
                },
                CancellationToken.None));
        }

        Assert.Equal(CodigosErro.FichaPropriaJaExiste, erro.Codigo);
        Assert.Equal(FichaService.MensagemFichaPropriaJaExiste, erro.Message);
        Assert.True(compensada);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Fichas.CountAsync(f => f.UtilizadorProprioId == titular.Id)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.UtilizadorId == titular.Id)));
    }

    /// <summary>
    /// Outro índice único (e-mail repetido) continua a sair como <see cref="DbUpdateException"/>: só os índices conhecidos são convertidos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OutroIndiceUnicoNaoEConvertido()
    {
        var existente = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));

        await using var ambito = this.fabrica.Services.CreateAsyncScope();
        var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        await Assert.ThrowsAsync<DbUpdateException>(() => unidade.ExecutarEmTransacaoAsync(
            () =>
            {
                contexto.Utilizadores.Add(new Utilizador("Pessoa Fictícia", existente.Email, "hash-ficticio", DadosTeste.Agora()));
                return Task.FromResult(true);
            },
            CancellationToken.None));
    }

    /// <summary>
    /// Uma falha de integridade referencial continua a sair como <see cref="DbUpdateException"/> (sem regressão).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDeChaveEstrangeiraNaoEConvertida()
    {
        var criador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var ficha = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, criador.Id));

        await using var ambito = this.fabrica.Services.CreateAsyncScope();
        var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        await Assert.ThrowsAsync<DbUpdateException>(() => unidade.ExecutarEmTransacaoAsync(
            () =>
            {
                contexto.Associacoes.Add(new Associacao(ficha.Id, Guid.NewGuid(), Papel.Familiar, DadosTeste.Agora()));
                return Task.FromResult(true);
            },
            CancellationToken.None));
    }
}
