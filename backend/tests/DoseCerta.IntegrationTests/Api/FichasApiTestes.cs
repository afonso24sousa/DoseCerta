// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Json;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração dos endpoints de fichas de F002 e F003 (OP-07 a OP-14), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class FichasApiTestes
{
    private static readonly int[] CondicaoInexistente = { 999 };
    private static readonly int[] UmaCondicao = { 1 };
    private static readonly string[] CamposInvalidos = { "nomeCompleto", "dataNascimento", "contactoEmergencia", "condicoesSaudeIds", "cuidadorPrincipal" };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichasApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public FichasApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-08: devolve as listas fechadas dos dados predefinidos (RF-021).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DominiosDevolveAsListasFechadas()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;

        using var resposta = await cliente.GetAsync("api/v1/dominios/necessidades");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal(9, corpo.GetProperty("condicoesSaude").GetArrayLength());
        Assert.Equal(6, corpo.GetProperty("alergias").GetArrayLength());
        var graus = corpo.GetProperty("grausAutonomia");
        Assert.Equal(4, graus.GetArrayLength());
        Assert.Equal("Autónomo", graus[0].GetProperty("designacao").GetString());
    }

    /// <summary>
    /// OP-09 e OP-10: a ficha de terceiro é criada ativa, com o criador como Cuidador principal, e consultada com o contrato da API.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarEConsultarFichaDeTerceiro()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var id = contaCliente.Id;

        using var criada = await cliente.PostAsJsonAsync("api/v1/fichas", ClienteFichas.PedidoTerceiro());
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(criada);
        var fichaId = corpo.GetProperty("id").GetGuid();
        using var consulta = await cliente.GetAsync($"api/v1/fichas/{fichaId}");
        var consultada = await ClienteApi.JsonAsync(consulta);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal("Maria Fictícia Santos", consultada.GetProperty("nomeCompleto").GetString());
        Assert.Equal("1941-05-17", consultada.GetProperty("dataNascimento").GetString());
        Assert.False(consultada.GetProperty("eProprio").GetBoolean());
        Assert.Equal("Ativa", consultada.GetProperty("estado").GetString());
        Assert.Equal("Autonomia parcial", consultada.GetProperty("grauAutonomia").GetProperty("designacao").GetString());
        Assert.Equal("Diabetes", consultada.GetProperty("condicoesSaude")[0].GetProperty("designacao").GetString());
        Assert.Equal("Medicamentos", consultada.GetProperty("alergias")[0].GetProperty("designacao").GetString());
        Assert.Equal("Cuidador", consultada.GetProperty("meuPapel").GetString());
        Assert.True(consultada.GetProperty("souPrincipal").GetBoolean());
        var associacao = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId));
        Assert.Equal(id, associacao.UtilizadorId);
        Assert.True(associacao.EPrincipal);
        Assert.False(associacao.EProvisorio);
    }

    /// <summary>
    /// RF-025: com outra pessoa para Cuidador principal, o criador fica provisório e o convite fica pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarComConviteParaPrincipalDeixaOCriadorProvisorio()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var convidado = DadosTeste.EmailUnico("rui");

        using var resposta = await cliente.PostAsJsonAsync("api/v1/fichas", new
        {
            nomeCompleto = "Utente Fictício",
            dataNascimento = "1950-01-01",
            contactoEmergencia = "910000001",
            cuidadorPrincipal = new { eu = false, email = convidado },
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var fichaId = (await ClienteApi.JsonAsync(resposta)).GetProperty("id").GetGuid();
        var associacao = await this.fabrica.ComContextoAsync(c => c.Associacoes.SingleAsync(a => a.FichaId == fichaId));
        var convite = await this.fabrica.ComContextoAsync(c => c.Convites.SingleAsync(v => v.FichaId == fichaId));
        Assert.True(associacao.EProvisorio);
        Assert.Equal(convidado, convite.EmailConvidado);
        Assert.True(convite.ParaPrincipal);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
    }

    /// <summary>
    /// RF-030 e RF-031: a ficha própria é criada uma só vez por Utilizador.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaPropriaSoUmaVez()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var id = contaCliente.Id;
        var pedido = new { propria = true, nomeCompleto = "Pessoa Fictícia", dataNascimento = "1980-02-29", contactoEmergencia = "+351 910 000 003" };

        using var primeira = await cliente.PostAsJsonAsync("api/v1/fichas", pedido);
        using var segunda = await cliente.PostAsJsonAsync("api/v1/fichas", pedido);

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.True((await ClienteApi.JsonAsync(primeira)).GetProperty("eProprio").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal("FICHA_PROPRIA_JA_EXISTE", await ClienteApi.CodigoErroAsync(segunda));
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Fichas.CountAsync(f => f.UtilizadorProprioId == id)));
    }

    /// <summary>
    /// OP-09: dados inválidos dão 400 com os campos em erro e um pedido sem sessão dá 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarComDadosInvalidosOuSemSessaoERecusado()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        using var anonimo = this.fabrica.CreateClient();

        using var invalido = await cliente.PostAsJsonAsync("api/v1/fichas", new
        {
            nomeCompleto = string.Empty,
            dataNascimento = "2999-01-01",
            contactoEmergencia = "abc",
            condicoesSaudeIds = CondicaoInexistente,
        });
        using var formatoData = await cliente.PostAsJsonAsync("api/v1/fichas", new { nomeCompleto = "X", dataNascimento = "17/05/1941" });
        using var semSessao = await anonimo.PostAsJsonAsync("api/v1/fichas", ClienteFichas.PedidoTerceiro());

        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        var campos = (await ClienteApi.JsonAsync(invalido)).GetProperty("campos").EnumerateArray().Select(c => c.GetProperty("campo").GetString()).ToList();
        Assert.Equal(CamposInvalidos, campos);
        Assert.Equal(HttpStatusCode.BadRequest, formatoData.StatusCode);
        Assert.Equal("VALIDACAO_INVALIDA", await ClienteApi.CodigoErroAsync(formatoData));
        Assert.Equal(HttpStatusCode.Unauthorized, semSessao.StatusCode);
    }

    /// <summary>
    /// OP-07: a lista só tem as fichas do Utilizador, com o filtro de estado (RNF-005).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarFichasDoUtilizador()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var contaOutro = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var outro = contaOutro.Cliente;
        var ativa = await ClienteFichas.CriarFichaAsync(cliente, "Bruno Fictício");
        var arquivada = await ClienteFichas.CriarFichaAsync(cliente, "Ana Fictícia");
        (await cliente.PostAsync($"api/v1/fichas/{arquivada}/arquivo", null)).Dispose();
        await ClienteFichas.CriarFichaAsync(outro);

        var todas = await ClienteApi.JsonAsync(await cliente.GetAsync("api/v1/fichas"));
        var ativas = await ClienteApi.JsonAsync(await cliente.GetAsync("api/v1/fichas?estado=Ativa"));
        using var invalido = await cliente.GetAsync("api/v1/fichas?estado=Outro");

        Assert.Equal(new[] { arquivada, ativa }, todas.EnumerateArray().Select(f => f.GetProperty("id").GetGuid()).ToArray());
        Assert.Equal("Arquivada", todas[0].GetProperty("estado").GetString());
        Assert.Equal("Cuidador", todas[0].GetProperty("meuPapel").GetString());
        Assert.True(todas[0].GetProperty("souPrincipal").GetBoolean());
        Assert.Equal(ativa, Assert.Single(ativas.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    /// <summary>
    /// RF-028 e RNF-005: o Familiar consulta a ficha; quem não está associado, ou só tem convite pendente, recebe 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConsultaRespeitaAAssociacao()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var familiarId = contaFamiliar.Id;
        var contaEstranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var estranho = contaEstranho.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiarId, Papel.Familiar);

        using var doFamiliar = await familiar.GetAsync($"api/v1/fichas/{fichaId}");
        using var doEstranho = await estranho.GetAsync($"api/v1/fichas/{fichaId}");
        using var inexistente = await estranho.GetAsync($"api/v1/fichas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, doFamiliar.StatusCode);
        var corpo = await ClienteApi.JsonAsync(doFamiliar);
        Assert.Equal("Familiar", corpo.GetProperty("meuPapel").GetString());
        Assert.False(corpo.GetProperty("souPrincipal").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(inexistente));
    }

    /// <summary>
    /// OP-11: o Cuidador edita; o Familiar recebe 403 e uma ficha arquivada 409 (RF-029, RF-120).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarFicha()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var familiarId = contaFamiliar.Id;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiarId, Papel.Familiar);
        var pedido = new
        {
            nomeCompleto = "Maria Santos",
            dataNascimento = "1941-05-17",
            contactoEmergencia = "+351 910 000 002",
            grauAutonomiaId = 3,
            condicoesSaudeIds = new[] { 2, 3 },
            alergiasIds = Array.Empty<int>(),
        };

        using var editada = await cliente.PutAsJsonAsync($"api/v1/fichas/{fichaId}", pedido);
        using var doFamiliar = await familiar.PutAsJsonAsync($"api/v1/fichas/{fichaId}", pedido);
        (await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null)).Dispose();
        using var arquivada = await cliente.PutAsJsonAsync($"api/v1/fichas/{fichaId}", pedido);

        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        var corpo = await ClienteApi.JsonAsync(editada);
        Assert.Equal("Maria Santos", corpo.GetProperty("nomeCompleto").GetString());
        Assert.Equal("+351 910 000 002", corpo.GetProperty("contactoEmergencia").GetString());
        Assert.Equal(3, corpo.GetProperty("grauAutonomia").GetProperty("id").GetInt16());
        Assert.Equal(2, corpo.GetProperty("condicoesSaude").GetArrayLength());
        Assert.Equal(0, corpo.GetProperty("alergias").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
        var ficha = await this.fabrica.ComContextoAsync(c => c.Fichas.Include(f => f.CondicoesSaude).SingleAsync(f => f.Id == fichaId));
        Assert.Equal("Maria Santos", ficha.NomeCompleto);
        Assert.Equal(2, ficha.CondicoesSaude.Count);
    }

    /// <summary>
    /// OP-12 e OP-13: arquivar apaga só as tomas futuras por confirmar; reativar volta a ativa; repetir dá 409 (RF-120 a RF-122).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ArquivarEReativar()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var id = contaCliente.Id;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var familiarId = contaFamiliar.Id;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiarId, Papel.Familiar);
        var agora = DadosTeste.Agora();
        var tomaPassada = await this.fabrica.ComContextoAsync(async c =>
        {
            var tratamento = new Tratamento(fichaId, "Metformina", "500 mg", GrauImportancia.Moderado, DateOnly.FromDateTime(agora), null, id, agora);
            c.Tratamentos.Add(tratamento);
            var passada = new Toma(tratamento.Id, agora.AddHours(-2), "Metformina", "500 mg", GrauImportancia.Moderado);
            c.Tomas.Add(passada);
            c.Tomas.Add(new Toma(tratamento.Id, agora.AddDays(1), "Metformina", "500 mg", GrauImportancia.Moderado));
            await c.SaveChangesAsync();
            return passada.Id;
        });

        using var doFamiliar = await familiar.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        using var arquivo = await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        using var repetido = await cliente.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        var tomas = await this.fabrica.ComContextoAsync(c => c.Tomas.Where(t => c.Tratamentos.Any(tr => tr.Id == t.TratamentoId && tr.FichaId == fichaId)).Select(t => t.Id).ToListAsync());
        using var consultaArquivada = await familiar.GetAsync($"api/v1/fichas/{fichaId}");
        using var reativacao = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}/arquivo");
        using var reativacaoRepetida = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}/arquivo");

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, arquivo.StatusCode);
        Assert.Equal("FICHA_JA_ARQUIVADA", await ClienteApi.CodigoErroAsync(repetido));
        Assert.Equal(tomaPassada, Assert.Single(tomas));
        Assert.Equal("Arquivada", (await ClienteApi.JsonAsync(consultaArquivada)).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.NoContent, reativacao.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reativacaoRepetida.StatusCode);
        Assert.Equal("FICHA_NAO_ARQUIVADA", await ClienteApi.CodigoErroAsync(reativacaoRepetida));
        var ficha = await this.fabrica.ComContextoAsync(c => c.Fichas.SingleAsync(f => f.Id == fichaId));
        Assert.Equal(EstadoFicha.Ativa, ficha.Estado);
        Assert.Null(ficha.ArquivadaEmUtc);
    }

    /// <summary>
    /// OP-14: a ficha sem registos é eliminada com as associações, os convites e as notificações (RF-123 CA1).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaSemRegistos()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var id = contaCliente.Id;
        using var criada = await cliente.PostAsJsonAsync("api/v1/fichas", new
        {
            nomeCompleto = "Utente Fictício",
            dataNascimento = "1950-01-01",
            contactoEmergencia = "910000001",
            condicoesSaudeIds = UmaCondicao,
            cuidadorPrincipal = new { eu = false, email = DadosTeste.EmailUnico() },
        });
        var fichaId = (await ClienteApi.JsonAsync(criada)).GetProperty("id").GetGuid();
        await this.fabrica.ComContextoAsync(c =>
        {
            c.Notificacoes.Add(new Notificacao(id, fichaId, TipoNotificacao.NovaOcorrencia, "Fictícia.", TipoReferencia.Ocorrencia, Guid.NewGuid(), DadosTeste.Agora(), DadosTeste.Agora()));
            return c.SaveChangesAsync();
        });

        using var resposta = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Fichas.AnyAsync(f => f.Id == fichaId)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Associacoes.AnyAsync(a => a.FichaId == fichaId)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Notificacoes.AnyAsync(n => n.FichaId == fichaId)));
        using var consulta = await cliente.GetAsync($"api/v1/fichas/{fichaId}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    /// <summary>
    /// OP-14: com medicação, a eliminação é recusada com 409 e nada é removido; o Familiar recebe 403 (RF-123 CA2).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaComMedicacaoERecusado()
    {
        var contaCliente = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = contaCliente.Cliente;
        var id = contaCliente.Id;
        var contaFamiliar = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var familiar = contaFamiliar.Cliente;
        var familiarId = contaFamiliar.Id;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        await ClienteFichas.AssociarAsync(this.fabrica, fichaId, familiarId, Papel.Familiar);
        await this.fabrica.ComContextoAsync(c =>
        {
            var agora = DadosTeste.Agora();
            c.Tratamentos.Add(new Tratamento(fichaId, "Metformina", "500 mg", GrauImportancia.Leve, DateOnly.FromDateTime(agora), null, id, agora));
            return c.SaveChangesAsync();
        });

        using var doFamiliar = await familiar.DeleteAsync($"api/v1/fichas/{fichaId}");
        using var resposta = await cliente.DeleteAsync($"api/v1/fichas/{fichaId}");

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("FICHA_COM_HISTORICO", await ClienteApi.CodigoErroAsync(resposta));
        Assert.True(await this.fabrica.ComContextoAsync(c => c.Fichas.AnyAsync(f => f.Id == fichaId)));
    }
}
