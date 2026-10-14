// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração do modelo de dados (I030): criação da base de dados pelas migrações, dados
/// predefinidos, unicidade, integridade referencial, cascatas e gatilhos (modelo de dados, secções 7 e 13).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ModeloDadosTestes
{
    private static readonly string[] TabelasEsperadas =
    {
        "Alergia", "Associacao", "Compromisso", "CondicaoSaude", "Convite", "Documento", "EnvioEmail",
        "ExecucaoTarefa", "Ficha", "FichaAlergia", "FichaCondicaoSaude", "GrauAutonomia", "HorarioTarefa",
        "HorarioToma", "Nota", "Notificacao", "Ocorrencia", "SessaoRevogada", "TarefaDiaSemana",
        "TarefaRecorrente", "Toma", "Tratamento", "Utilizador",
    };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeloDadosTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ModeloDadosTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// A migração inicial cria as 23 tabelas do modelo de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MigracaoCriaAsVinteETresTabelas()
    {
        var tabelas = await this.fabrica.ComContextoAsync(c => c.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'")
            .ToListAsync());

        Assert.Equal(TabelasEsperadas.OrderBy(t => t, StringComparer.Ordinal), tabelas.OrderBy(t => t, StringComparer.Ordinal));
    }

    /// <summary>
    /// A migração inicial cria os quatro gatilhos de imutabilidade do histórico.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MigracaoCriaOsQuatroGatilhos()
    {
        var gatilhos = await this.fabrica.ComContextoAsync(c => c.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.triggers WHERE parent_class = 1")
            .ToListAsync());

        foreach (var nome in GatilhosSql.Nomes())
        {
            Assert.Contains(nome, gatilhos);
        }
    }

    /// <summary>
    /// As listas fechadas de necessidades têm os valores predefinidos da secção 8 do modelo de dados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListasFechadasTemOsValoresPredefinidos()
    {
        var (condicoes, alergias, graus) = await this.fabrica.ComContextoAsync(async c =>
            (await c.CondicoesSaude.CountAsync(), await c.Alergias.CountAsync(), await c.GrausAutonomia.CountAsync()));

        Assert.Equal(9, condicoes);
        Assert.Equal(6, alergias);
        Assert.Equal(4, graus);
    }

    /// <summary>
    /// O e-mail é único sem distinção de maiúsculas (UQ_Utilizador_Email com collation CI_AI).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailDuplicadoComMaiusculasDiferentesERecusado()
    {
        var email = DadosTeste.EmailUnico("ana");
        await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c, email));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c, email.ToUpperInvariant())));
    }

    /// <summary>
    /// Um Utilizador tem no máximo uma ficha própria (UQ_Ficha_UtilizadorProprio).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SegundaFichaPropriaERecusada()
    {
        var utilizador = await this.fabrica.ComContextoAsync(c => DadosTeste.CriarUtilizadorAsync(c));
        await this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, utilizador.Id, propria: true));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            this.fabrica.ComContextoAsync(c => DadosTeste.CriarFichaAsync(c, utilizador.Id, propria: true)));
    }

    /// <summary>
    /// Uma ficha tem no máximo um Cuidador principal (UQ_Associacao_Principal) e o principal é Cuidador (CK_Associacao_Principal).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SegundoCuidadorPrincipalERecusado()
    {
        var (fichaId, primeiro, segundo) = await this.CriarFichaComDoisUtilizadoresAsync();
        await this.InserirAssociacaoAsync(fichaId, primeiro, "Cuidador", principal: true);

        await Assert.ThrowsAsync<SqlException>(() => this.InserirAssociacaoAsync(fichaId, segundo, "Cuidador", principal: true));
        await Assert.ThrowsAsync<SqlException>(() => this.InserirAssociacaoAsync(fichaId, segundo, "Familiar", principal: false, provisorio: true));
    }

    /// <summary>
    /// Só existe um convite pendente por e-mail e ficha; depois de recusado pode ser criado outro (UQ_Convite_Pendente).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitePendenteDuplicadoERecusadoMasPermitidoDepoisDeRecusado()
    {
        var (fichaId, criador, convidado) = await this.CriarFichaComDoisUtilizadoresAsync();
        var email = DadosTeste.EmailUnico("convidado");
        await this.InserirConviteAsync(fichaId, criador, email);

        await Assert.ThrowsAsync<SqlException>(() => this.InserirConviteAsync(fichaId, criador, email.ToUpperInvariant()));

        await this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Convite SET Estado = 'Recusado', RespondidoEmUtc = SYSUTCDATETIME(), UtilizadorConvidadoId = {convidado} WHERE FichaId = {fichaId}"));
        await this.InserirConviteAsync(fichaId, criador, email);

        var pendentes = await this.fabrica.ComContextoAsync(c =>
            c.Convites.CountAsync(x => x.FichaId == fichaId && x.Estado == EstadoConvite.Pendente));
        Assert.Equal(1, pendentes);
    }

    /// <summary>
    /// Uma associação a um Utilizador inexistente é recusada (FK_Associacao_UtilizadorId).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AssociacaoAUtilizadorInexistenteERecusada()
    {
        var (fichaId, _, _) = await this.CriarFichaComDoisUtilizadoresAsync();

        await Assert.ThrowsAsync<SqlException>(() => this.InserirAssociacaoAsync(fichaId, Guid.NewGuid(), "Cuidador", principal: false));
    }

    /// <summary>
    /// Não é possível eliminar um Utilizador que criou uma ficha (FK_Ficha_CriadaPorId, NO ACTION).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarUtilizadorReferenciadoERecusado()
    {
        var (_, criador, _) = await this.CriarFichaComDoisUtilizadoresAsync();

        await Assert.ThrowsAsync<SqlException>(() => this.fabrica.ComContextoAsync(c =>
            c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Utilizador WHERE Id = {criador}")));
    }

    /// <summary>
    /// Eliminar uma ficha remove as associações e os convites por cascata (RF-123).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaRemoveAssociacoesEConvitesPorCascata()
    {
        var (fichaId, criador, _) = await this.CriarFichaComDoisUtilizadoresAsync();
        await this.InserirAssociacaoAsync(fichaId, criador, "Cuidador", principal: true);
        await this.InserirConviteAsync(fichaId, criador, DadosTeste.EmailUnico());

        await this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Ficha WHERE Id = {fichaId}"));

        var restantes = await this.fabrica.ComContextoAsync(async c =>
            await c.Associacoes.CountAsync(a => a.FichaId == fichaId) + await c.Convites.CountAsync(x => x.FichaId == fichaId));
        Assert.Equal(0, restantes);
    }

    /// <summary>
    /// Uma ocorrência registada não pode ser alterada (TR_Ocorrencia_Imutavel).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OcorrenciaNaoPodeSerAlterada()
    {
        var (fichaId, autor, _) = await this.CriarFichaComDoisUtilizadoresAsync();
        var ocorrencia = new Ocorrencia(fichaId, "Ocorrência fictícia.", DadosTeste.Agora(), autor, DadosTeste.Agora());
        await this.fabrica.ComContextoAsync(async c =>
        {
            c.Ocorrencias.Add(ocorrencia);
            return await c.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<SqlException>(() => this.fabrica.ComContextoAsync(c =>
            c.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Ocorrencia SET Descricao = 'Alterada' WHERE Id = {ocorrencia.Id}")));
    }

    private async Task<(Guid FichaId, Guid Primeiro, Guid Segundo)> CriarFichaComDoisUtilizadoresAsync()
    {
        return await this.fabrica.ComContextoAsync(async c =>
        {
            var primeiro = await DadosTeste.CriarUtilizadorAsync(c);
            var segundo = await DadosTeste.CriarUtilizadorAsync(c);
            var ficha = await DadosTeste.CriarFichaAsync(c, primeiro.Id);
            return (ficha.Id, primeiro.Id, segundo.Id);
        });
    }

    private Task<int> InserirAssociacaoAsync(Guid fichaId, Guid utilizadorId, string papel, bool principal, bool provisorio = false)
    {
        return this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.Associacao (Id, FichaId, UtilizadorId, Papel, EPrincipal, EProvisorio) VALUES ({Guid.NewGuid()}, {fichaId}, {utilizadorId}, {papel}, {principal}, {provisorio})"));
    }

    private Task<int> InserirConviteAsync(Guid fichaId, Guid convidadoPorId, string email)
    {
        return this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.Convite (Id, FichaId, EmailConvidado, PapelOferecido, ConvidadoPorId) VALUES ({Guid.NewGuid()}, {fichaId}, {email}, 'Familiar', {convidadoPorId})"));
    }
}
