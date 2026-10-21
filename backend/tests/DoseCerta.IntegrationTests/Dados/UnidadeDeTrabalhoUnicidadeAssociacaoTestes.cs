// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Convites;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da conversão das violações dos índices únicos da associação a uma ficha pela unidade de trabalho,
/// sobre o SQL Server (RF-043, RNF-046; D8). Cada teste força a violação, sem depender do momento em que chegam pedidos simultâneos.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class UnidadeDeTrabalhoUnicidadeAssociacaoTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoUnicidadeAssociacaoTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public UnidadeDeTrabalhoUnicidadeAssociacaoTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-043: um segundo convite pendente para o mesmo e-mail e a mesma ficha, recusado pelo índice <c>UQ_Convite_Pendente</c>,
    /// dá <c>CONVITE_PENDENTE_EXISTE</c> e não grava nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitePendenteRepetidoDaErroDeNegocio()
    {
        var (cuidador, ficha) = await this.FichaAsync();
        var email = DadosTeste.EmailUnico("rui");
        await this.fabrica.ComContextoAsync(c =>
        {
            c.Convites.Add(new Convite(ficha.Id, email, Papel.Familiar, false, cuidador.Id, DadosTeste.Agora()));
            return c.SaveChangesAsync();
        });

        var erro = await this.GravarAsync(c => c.Convites.Add(new Convite(ficha.Id, email.ToUpperInvariant(), Papel.Cuidador, false, cuidador.Id, DadosTeste.Agora())));

        Assert.Equal(CodigosErro.ConvitePendenteExiste, erro.Codigo);
        Assert.Equal(ConviteService.MensagemConvitePendenteExiste, erro.Message);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Convites.CountAsync(v => v.FichaId == ficha.Id)));
    }

    /// <summary>
    /// D8: uma segunda associação do mesmo Utilizador à mesma ficha, recusada pelo índice <c>UQ_Associacao_FichaUtilizador</c>,
    /// dá <c>PESSOA_JA_ASSOCIADA</c> e não grava nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AssociacaoRepetidaDaErroDeNegocio()
    {
        var (cuidador, ficha) = await this.FichaAsync();

        var erro = await this.GravarAsync(c => c.Associacoes.Add(new Associacao(ficha.Id, cuidador.Id, Papel.Familiar, DadosTeste.Agora())));

        Assert.Equal(CodigosErro.PessoaJaAssociada, erro.Codigo);
        Assert.Equal(Ficha.MensagemPessoaJaAssociada, erro.Message);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Associacoes.CountAsync(a => a.FichaId == ficha.Id)));
    }

    private async Task<(Utilizador Cuidador, Ficha Ficha)> FichaAsync()
    {
        var cuidador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var ficha = await this.fabrica.ComContextoAsync(async c =>
        {
            var nova = new Ficha("Utente Fictício", new DateOnly(1941, 5, 17), "+351 910 000 001", null, cuidador.Id, DadosTeste.Agora());
            nova.AssociarCuidadorPrincipal(cuidador.Id, provisorio: false, DadosTeste.Agora());
            c.Fichas.Add(nova);
            await c.SaveChangesAsync();
            return nova;
        });
        return (cuidador, ficha);
    }

    private async Task<DominioException> GravarAsync(Action<DoseCertaDbContext> alteracao)
    {
        await using var ambito = this.fabrica.Services.CreateAsyncScope();
        var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        return await Assert.ThrowsAsync<DominioException>(() => unidade.ExecutarEmTransacaoAsync(
            () =>
            {
                alteracao(contexto);
                return Task.FromResult(true);
            },
            CancellationToken.None));
    }
}
