// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Tratamentos;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração do passo 4 do processo periódico sobre o SQL Server: término dos tratamentos cuja data de fim
/// foi ultrapassada (RF-056; Modelo de Classes do Backend, secção 8.1).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class TerminoTratamentosTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminoTratamentosTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public TerminoTratamentosTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// Só o tratamento em curso com data de fim anterior a hoje (Lisboa) é terminado e gravado; o que acaba hoje e o
    /// contínuo continuam em curso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarConcluidosGravaSoOsTratamentosComDataDeFimUltrapassada()
    {
        var agora = DadosTeste.Agora();
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(agora, "Europe/Lisbon"));
        var cuidador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        var ficha = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, cuidador.Id));
        var ontem = Novo(ficha.Id, cuidador.Id, hoje.AddDays(-1), agora);
        var acabaHoje = Novo(ficha.Id, cuidador.Id, hoje, agora);
        var continuo = Novo(ficha.Id, cuidador.Id, null, agora);
        await this.fabrica.ComContextoAsync(c =>
        {
            c.Tratamentos.AddRange(ontem, acabaHoje, continuo);
            return c.SaveChangesAsync();
        });

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var servico = ambito.ServiceProvider.GetRequiredService<TratamentoService>();
            Assert.True(await servico.TerminarConcluidosAsync(agora, CancellationToken.None) >= 1);
        }

        var estados = await this.fabrica.ComContextoAsync(c => c.Tratamentos
            .Where(t => t.FichaId == ficha.Id)
            .ToDictionaryAsync(t => t.Id, t => (t.Estado, t.TerminadoEmUtc)));
        Assert.Equal((EstadoTratamento.Terminado, (DateTime?)agora), estados[ontem.Id]);
        Assert.Equal(EstadoTratamento.EmCurso, estados[acabaHoje.Id].Estado);
        Assert.Equal(EstadoTratamento.EmCurso, estados[continuo.Id].Estado);
    }

    private static Tratamento Novo(Guid fichaId, Guid cuidadorId, DateOnly? dataFim, DateTime agora)
    {
        var inicio = (dataFim ?? DateOnly.FromDateTime(agora)).AddDays(-3);
        return new Tratamento(fichaId, "Metformina", "500 mg", GrauImportancia.Leve, inicio, dataFim, new[] { new TimeOnly(8, 0) }, cuidadorId, agora);
    }
}
