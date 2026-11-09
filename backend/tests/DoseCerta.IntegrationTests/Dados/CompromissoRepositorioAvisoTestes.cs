// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração, sobre o SQL Server, das consultas do <c>CompromissoRepositorio</c> usadas pelo aviso antes de um
/// compromisso (I056: RF-117, RF-142, RNF-043).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class CompromissoRepositorioAvisoTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissoRepositorioAvisoTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public CompromissoRepositorioAvisoTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-117 e RF-142: só são devolvidos os compromissos de fichas ativas com o aviso devido, por enviar e cuja data ainda
    /// não chegou; o aviso que ficou devido antes da reativação da ficha e os da ficha arquivada ficam de fora.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PorAvisarDevolveSoOsAvisosDevidos()
    {
        var agora = DadosTeste.Agora();
        var cuidador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var ativa = await this.FichaAtivaDesdeAsync(cuidador.Id, agora.AddDays(-3));
        var reativada = await this.FichaAtivaDesdeAsync(cuidador.Id, agora.AddMinutes(-10));
        var arquivada = await this.FichaAtivaDesdeAsync(cuidador.Id, agora.AddDays(-3));
        var devido = Novo(ativa, cuidador.Id, agora.AddMinutes(30), 60, agora.AddDays(-2));
        var registadoDepoisDoAviso = Novo(reativada, cuidador.Id, agora.AddMinutes(30), 1440, agora.AddMinutes(-1));
        var aindaNao = Novo(ativa, cuidador.Id, agora.AddHours(3), 60, agora.AddDays(-2));
        var jaPassou = Novo(ativa, cuidador.Id, agora.AddMinutes(-1), 60, agora.AddDays(-2));
        var jaAvisado = Novo(ativa, cuidador.Id, agora.AddMinutes(20), 60, agora.AddDays(-2));
        jaAvisado.MarcarAvisoEnviado(agora.AddMinutes(-40));
        var doPeriodoArquivado = Novo(reativada, cuidador.Id, agora.AddMinutes(50), 120, agora.AddDays(-2));
        var depoisDaReativacao = Novo(reativada, cuidador.Id, agora.AddMinutes(50), 60, agora.AddDays(-2));
        var deArquivada = Novo(arquivada, cuidador.Id, agora.AddMinutes(30), 60, agora.AddDays(-2));
        await this.fabrica.ComContextoAsync(async c =>
        {
            c.Compromissos.AddRange(devido, registadoDepoisDoAviso, aindaNao, jaPassou, jaAvisado, doPeriodoArquivado, depoisDaReativacao, deArquivada);
            var ficha = await c.Fichas.SingleAsync(f => f.Id == arquivada);
            ficha.Arquivar(agora.AddMinutes(-5));
            return await c.SaveChangesAsync();
        });

        var ids = await this.ComRepositorioAsync(r => r.PorAvisarAsync(agora, CancellationToken.None));

        var meus = new[] { devido, registadoDepoisDoAviso, aindaNao, jaPassou, jaAvisado, doPeriodoArquivado, depoisDaReativacao, deArquivada }
            .Select(c => c.Id)
            .ToHashSet();
        var devolvidos = ids.Where(meus.Contains).ToList();
        Assert.Equal(new[] { registadoDepoisDoAviso.Id, devido.Id, depoisDaReativacao.Id }.ToHashSet(), devolvidos.ToHashSet());
        Assert.Equal(depoisDaReativacao.Id, devolvidos[^1]);
    }

    /// <summary>
    /// RNF-043: a marcação condicional só marca uma vez e não marca um compromisso cuja data mudou depois da leitura.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MarcarAvisoEnviadoSoMarcaUmaVezESemAlteracoes()
    {
        var agora = DadosTeste.Agora();
        var cuidador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var fichaId = await this.FichaAtivaDesdeAsync(cuidador.Id, agora.AddDays(-3));
        var compromisso = Novo(fichaId, cuidador.Id, agora.AddMinutes(30), 60, agora.AddDays(-2));
        var editado = Novo(fichaId, cuidador.Id, agora.AddMinutes(30), 60, agora.AddDays(-2));
        await this.fabrica.ComContextoAsync(c =>
        {
            c.Compromissos.AddRange(compromisso, editado);
            return c.SaveChangesAsync();
        });
        await this.fabrica.ComContextoAsync(c => c.Compromissos
            .Where(x => x.Id == editado.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DataHoraUtc, agora.AddMinutes(45))));

        var primeira = await this.ComRepositorioAsync(r => r.MarcarAvisoEnviadoAsync(compromisso, agora, CancellationToken.None));
        var segunda = await this.ComRepositorioAsync(r => r.MarcarAvisoEnviadoAsync(compromisso, agora.AddMinutes(1), CancellationToken.None));
        var comValoresAntigos = await this.ComRepositorioAsync(r => r.MarcarAvisoEnviadoAsync(editado, agora, CancellationToken.None));

        Assert.True(primeira);
        Assert.False(segunda);
        Assert.False(comValoresAntigos);
        var avisos = await this.fabrica.ComContextoAsync(c => c.Compromissos
            .Where(x => x.Id == compromisso.Id || x.Id == editado.Id)
            .ToDictionaryAsync(x => x.Id, x => x.AvisoEnviadoEmUtc));
        Assert.Equal(agora, avisos[compromisso.Id]);
        Assert.Null(avisos[editado.Id]);
    }

    /// <summary>
    /// RNF-043: se o aviso for marcado depois de o compromisso ser lido para edição, a gravação da data nova repõe-no.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GravarAvisoRepostoEscreveOAvisoPorEnviar()
    {
        var agora = DadosTeste.Agora();
        var cuidador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var fichaId = await this.FichaAtivaDesdeAsync(cuidador.Id, agora.AddDays(-3));
        var compromisso = Novo(fichaId, cuidador.Id, agora.AddMinutes(30), 60, agora.AddDays(-2));
        await this.fabrica.ComContextoAsync(c =>
        {
            c.Compromissos.Add(compromisso);
            return c.SaveChangesAsync();
        });

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var repositorio = ambito.ServiceProvider.GetRequiredService<ICompromissoRepositorio>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            var lido = await repositorio.ObterPorIdAsync(compromisso.Id, CancellationToken.None);

            // O processo periódico marca o aviso entre a leitura e a gravação da edição.
            await this.fabrica.ComContextoAsync(c => c.Compromissos
                .Where(x => x.Id == compromisso.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.AvisoEnviadoEmUtc, agora)));

            Assert.True(lido!.Atualizar(agora.AddDays(1), lido.Local, lido.Motivo, lido.AntecedenciaAvisoMin));
            repositorio.GravarAvisoReposto(lido);
            await contexto.SaveChangesAsync();
        }

        var guardado = await this.fabrica.ComContextoAsync(c => c.Compromissos.AsNoTracking().SingleAsync(x => x.Id == compromisso.Id));
        Assert.Null(guardado.AvisoEnviadoEmUtc);
        Assert.Equal(agora.AddDays(1), guardado.DataHoraUtc);
    }

    private static Compromisso Novo(Guid fichaId, Guid criadoPorId, DateTime dataHoraUtc, int antecedencia, DateTime criadoEmUtc)
    {
        return new Compromisso(fichaId, dataHoraUtc, "Centro de Saúde Fictício", "Consulta de rotina", antecedencia, criadoPorId, criadoEmUtc);
    }

    private async Task<Guid> FichaAtivaDesdeAsync(Guid cuidadorId, DateTime ativaDesdeUtc)
    {
        var ficha = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, cuidadorId));
        await this.fabrica.ComContextoAsync(c => c.Fichas
            .Where(f => f.Id == ficha.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.CriadaEmUtc, ativaDesdeUtc.AddDays(-1))
                .SetProperty(f => f.AtivaDesdeUtc, ativaDesdeUtc)));
        return ficha.Id;
    }

    private async Task<T> ComRepositorioAsync<T>(Func<ICompromissoRepositorio, Task<T>> acao)
    {
        await using var ambito = this.fabrica.Services.CreateAsyncScope();
        return await acao(ambito.ServiceProvider.GetRequiredService<ICompromissoRepositorio>());
    }
}
