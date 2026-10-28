// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Net;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração de OP-50 (F010, I046): a API completa com o SQL Server. As tomas e os compromissos são criados
/// diretamente na base de dados, e as tomas confirmadas com um <c>UPDATE</c>, porque a confirmação (F006) e a agenda (F008)
/// são de outras tarefas. O dia usado é de verão, para verificar o desvio <c>+01:00</c> de Lisboa.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ListaDiariaApiTestes
{
    private const string Dia = "2027-06-15";

    private static readonly CalendarioLisboa Calendario = new();

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListaDiariaApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ListaDiariaApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-100 (CA1, CA2 e CA5), RF-101 e RF-104: cada Cuidador vê as tomas e os compromissos do dia das fichas em que é Cuidador,
    /// com o nome do utente, ordenados por hora; a toma confirmada por outro Cuidador aparece com quem a confirmou e quando; as
    /// fichas em que é Familiar não aparecem e os compromissos não são confirmáveis.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CadaCuidadorVeOsItensDoDiaDasSuasFichas()
    {
        var (ana, _, anaId) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Ana Fictícia");
        var (rui, _, ruiId) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Rui Fictício");
        using (ana)
        using (rui)
        {
            var fichaX = await ClienteFichas.CriarFichaAsync(ana, "Maria Fictícia Santos");
            var fichaY = await ClienteFichas.CriarFichaAsync(ana, "João Fictício Silva");
            var fichaZ = await ClienteFichas.CriarFichaAsync(rui, "Rita Fictícia Costa");
            await ClienteFichas.AssociarAsync(this.fabrica, fichaX, ruiId, Papel.Cuidador);
            await ClienteFichas.AssociarAsync(this.fabrica, fichaZ, anaId, Papel.Familiar);
            var tomaX = await this.TomaAsync(fichaX, anaId, Hora(8, 0));
            await this.ConfirmarAsync(tomaX, anaId, Hora(8, 5));
            var tomaY = await this.TomaAsync(fichaY, anaId, Hora(8, 0));
            var compromissoX = await this.CompromissoAsync(fichaX, anaId, Hora(10, 30));
            var compromissoY = await this.CompromissoAsync(fichaY, anaId, Hora(15, 0));
            var tomaZ = await this.TomaAsync(fichaZ, ruiId, Hora(9, 0));

            using var daAna = await ana.GetAsync($"api/v1/lista-diaria?data={Dia}");
            using var doRui = await rui.GetAsync($"api/v1/lista-diaria?data={Dia}");

            Assert.Equal(HttpStatusCode.OK, daAna.StatusCode);
            var itens = (await ClienteApi.JsonAsync(daAna)).EnumerateArray().ToList();
            Assert.Equal(new[] { tomaY, tomaX, compromissoX, compromissoY }, itens.Select(i => i.GetProperty("id").GetGuid()));

            var pendente = itens[0];
            Assert.Equal("Toma", pendente.GetProperty("tipo").GetString());
            Assert.Equal(fichaY, pendente.GetProperty("fichaId").GetGuid());
            Assert.Equal("João Fictício Silva", pendente.GetProperty("nomeFicha").GetString());
            Assert.Equal("Metformina 500 mg", pendente.GetProperty("titulo").GetString());
            Assert.Equal("2027-06-15T08:00:00+01:00", pendente.GetProperty("previstaEm").GetString());
            Assert.Equal("Pendente", pendente.GetProperty("estado").GetString());
            Assert.False(pendente.TryGetProperty("confirmadaPor", out _));
            Assert.False(pendente.TryGetProperty("confirmadaEm", out _));

            var confirmada = itens[1];
            Assert.Equal("Maria Fictícia Santos", confirmada.GetProperty("nomeFicha").GetString());
            Assert.Equal("Confirmada", confirmada.GetProperty("estado").GetString());
            Assert.Equal("Ana Fictícia", confirmada.GetProperty("confirmadaPor").GetString());
            Assert.Equal("2027-06-15T08:05:00+01:00", confirmada.GetProperty("confirmadaEm").GetString());
            Assert.False(confirmada.GetProperty("confirmavel").GetBoolean());

            var compromisso = itens[2];
            Assert.Equal("Compromisso", compromisso.GetProperty("tipo").GetString());
            Assert.Equal("Consulta de rotina", compromisso.GetProperty("titulo").GetString());
            Assert.Equal("2027-06-15T10:30:00+01:00", compromisso.GetProperty("previstaEm").GetString());
            Assert.False(compromisso.TryGetProperty("estado", out _));
            Assert.False(compromisso.GetProperty("confirmavel").GetBoolean());

            var doRuiItens = (await ClienteApi.JsonAsync(doRui)).EnumerateArray().ToList();
            Assert.Equal(new[] { tomaX, tomaZ, compromissoX }, doRuiItens.Select(i => i.GetProperty("id").GetGuid()));
            Assert.Equal("Ana Fictícia", doRuiItens[0].GetProperty("confirmadaPor").GetString());
        }
    }

    /// <summary>
    /// RF-100, CA3: depois de arquivada, os itens da ficha deixam de aparecer, incluindo os compromissos e as tomas confirmadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaDeixaDeAparecer()
    {
        var (cliente, _, id) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using (cliente)
        {
            var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
            var toma = await this.TomaAsync(fichaId, id, Hora(8, 0));
            await this.ConfirmarAsync(toma, id, Hora(8, 5));
            await this.CompromissoAsync(fichaId, id, Hora(10, 30));

            using var antes = await cliente.GetAsync($"api/v1/lista-diaria?data={Dia}");
            (await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null)).Dispose();
            using var depois = await cliente.GetAsync($"api/v1/lista-diaria?data={Dia}");

            Assert.Equal(2, (await ClienteApi.JsonAsync(antes)).GetArrayLength());
            Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
            Assert.Equal(0, (await ClienteApi.JsonAsync(depois)).GetArrayLength());
        }
    }

    /// <summary>
    /// RNF-047: sem a data, a lista é a do dia de hoje na hora de Lisboa, e a toma pendente de hoje é confirmável (RF-102).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemDataUsaODiaDeHojeEmLisboa()
    {
        var (cliente, _, id) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using (cliente)
        {
            var hoje = Calendario.DiaLocal(DateTime.UtcNow);
            var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
            var toma = await this.TomaAsync(fichaId, id, Calendario.ParaUtc(hoje, new TimeOnly(23, 59)));

            using var resposta = await cliente.GetAsync("api/v1/lista-diaria");

            var item = Assert.Single((await ClienteApi.JsonAsync(resposta)).EnumerateArray());
            Assert.Equal(toma, item.GetProperty("id").GetGuid());
            Assert.True(item.GetProperty("confirmavel").GetBoolean());
        }
    }

    /// <summary>
    /// RP-15: quem não é Cuidador de nenhuma ficha ativa recebe 200 com uma lista vazia.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemFichasDeCuidadorRecebeListaVazia()
    {
        var (cliente, _, _) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using (cliente)
        {
            using var resposta = await cliente.GetAsync($"api/v1/lista-diaria?data={Dia}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(0, (await ClienteApi.JsonAsync(resposta)).GetArrayLength());
        }
    }

    /// <summary>
    /// RNF-028: uma data com outro formato ou inexistente é recusada com 400 e o campo <c>data</c>.
    /// </summary>
    /// <param name="data">Valor do parâmetro.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("15-06-2027")]
    [InlineData("2027-02-30")]
    [InlineData("2027-06-15T08:00:00")]
    public async Task DataInvalidaERecusada(string data)
    {
        var (cliente, _, _) = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using (cliente)
        {
            using var resposta = await cliente.GetAsync($"api/v1/lista-diaria?data={Uri.EscapeDataString(data)}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var corpo = await ClienteApi.JsonAsync(resposta);
            Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
            Assert.Equal("data", corpo.GetProperty("campos")[0].GetProperty("campo").GetString());
        }
    }

    /// <summary>
    /// RF-017: sem sessão, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoERecusado()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await cliente.GetAsync("api/v1/lista-diaria");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("NAO_AUTENTICADO", await ClienteApi.CodigoErroAsync(resposta));
    }

    private static DateTime Hora(int hora, int minuto)
    {
        return Calendario.ParaUtc(DateOnly.Parse(Dia, CultureInfo.InvariantCulture), new TimeOnly(hora, minuto));
    }

    private Task<Guid> TomaAsync(Guid fichaId, Guid autorId, DateTime previstaEmUtc)
    {
        return this.fabrica.ComContextoAsync(async c =>
        {
            var agora = DadosTeste.Agora();
            var tratamento = new Tratamento(
                fichaId,
                "Metformina",
                "500 mg",
                GrauImportancia.Moderado,
                Calendario.DiaLocal(previstaEmUtc),
                null,
                new[] { new TimeOnly(8, 0) },
                autorId,
                agora);
            var toma = new Toma(tratamento.Id, previstaEmUtc, "Metformina", "500 mg", GrauImportancia.Moderado);
            c.Tratamentos.Add(tratamento);
            c.Tomas.Add(toma);
            await c.SaveChangesAsync();
            return toma.Id;
        });
    }

    private Task<int> ConfirmarAsync(Guid tomaId, Guid cuidadorId, DateTime confirmadaEmUtc)
    {
        return this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Toma SET Estado = 'Confirmada', ConfirmadaPorId = {cuidadorId}, ConfirmadaEmUtc = {confirmadaEmUtc} WHERE Id = {tomaId}"));
    }

    private Task<Guid> CompromissoAsync(Guid fichaId, Guid autorId, DateTime dataHoraUtc)
    {
        return this.fabrica.ComContextoAsync(async c =>
        {
            var compromisso = new Compromisso(fichaId, dataHoraUtc, "Centro de Saúde Fictício", "Consulta de rotina", 1440, autorId, DadosTeste.Agora());
            c.Compromissos.Add(compromisso);
            await c.SaveChangesAsync();
            return compromisso.Id;
        });
    }
}
