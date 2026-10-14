// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da unidade de trabalho sobre o SQL Server (RNF-046, AD-10).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class UnidadeDeTrabalhoTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public UnidadeDeTrabalhoTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// Uma operação bem-sucedida fica gravada e as compensações não são executadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OperacaoBemSucedidaFicaGravada()
    {
        var email = DadosTeste.EmailUnico();
        var compensada = false;

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            await unidade.ExecutarEmTransacaoAsync(
                async () =>
                {
                    unidade.AoFalhar(() => compensada = true);
                    contexto.Utilizadores.Add(new Utilizador("Pessoa Fictícia", email, "hash-ficticio", DadosTeste.Agora()));
                    await unidade.GravarAlteracoesAsync(CancellationToken.None);
                    return true;
                },
                CancellationToken.None);
        }

        var existe = await this.fabrica.ComContextoAsync(c => c.Utilizadores.AnyAsync(u => u.Email == email));
        Assert.True(existe);
        Assert.False(compensada);
    }

    /// <summary>
    /// Uma operação que falha não grava nada, mesmo o que já tinha sido gravado dentro da transação, executa as
    /// compensações registadas (mesmo que uma delas falhe) e volta a lançar o erro original.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OperacaoFalhadaNaoGravaNadaEExecutaCompensacoes()
    {
        var email = DadosTeste.EmailUnico();
        var compensada = false;

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => unidade.ExecutarEmTransacaoAsync<bool>(
                async () =>
                {
                    unidade.AoFalhar(() => throw new IOException("Falha simulada da compensação."));
                    unidade.AoFalhar(() => compensada = true);
                    contexto.Utilizadores.Add(new Utilizador("Pessoa Fictícia", email, "hash-ficticio", DadosTeste.Agora()));
                    await unidade.GravarAlteracoesAsync(CancellationToken.None);
                    throw new InvalidOperationException("Falha simulada.");
                },
                CancellationToken.None));
        }

        var existe = await this.fabrica.ComContextoAsync(c => c.Utilizadores.AnyAsync(u => u.Email == email));
        Assert.False(existe);
        Assert.True(compensada);
    }
}
