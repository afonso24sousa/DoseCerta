// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração dos endpoints de F001 (OP-01 a OP-06), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ContaApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContaApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ContaApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// OP-01: o registo devolve 201 com a conta, sem palavra-passe, e guarda apenas o hash (RNF-001).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistoDevolve201EGuardaSoOHash()
    {
        using var cliente = this.fabrica.CreateClient();
        var email = DadosTeste.EmailUnico("ana");

        using var resposta = await cliente.PostAsJsonAsync("api/v1/auth/registo", new { nome = "Ana Fictícia", email, palavraPasse = "Segura123" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal(email, corpo.GetProperty("email").GetString());
        Assert.Equal("Ana Fictícia", corpo.GetProperty("nome").GetString());
        Assert.Equal(0, corpo.GetProperty("convitesPendentes").GetInt32());
        Assert.False(corpo.TryGetProperty("palavraPasse", out _));
        var hash = await this.fabrica.ComContextoAsync(c => c.Utilizadores.Where(u => u.Email == email).Select(u => u.PalavraPasseHash).SingleAsync());
        Assert.StartsWith("PBKDF2-SHA256$", hash, StringComparison.Ordinal);
    }

    /// <summary>
    /// OP-01: e-mail duplicado dá 409 e dados inválidos dão 400 com os campos em erro.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistoRecusaEmailDuplicadoEDadosInvalidos()
    {
        using var cliente = this.fabrica.CreateClient();
        var (email, _) = await ClienteApi.CriarContaAsync(cliente);

        using var duplicado = await cliente.PostAsJsonAsync("api/v1/auth/registo", new { nome = "Outra", email = email.ToUpperInvariant(), palavraPasse = "Segura123" });
        using var invalido = await cliente.PostAsJsonAsync("api/v1/auth/registo", new { nome = string.Empty, email = "x", palavraPasse = "123" });

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal("EMAIL_JA_REGISTADO", await ClienteApi.CodigoErroAsync(duplicado));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        var corpo = await ClienteApi.JsonAsync(invalido);
        Assert.Equal("VALIDACAO_INVALIDA", corpo.GetProperty("codigo").GetString());
        Assert.Equal(3, corpo.GetProperty("campos").GetArrayLength());
    }

    /// <summary>
    /// OP-02: o início de sessão devolve um token Bearer que expira 8 horas depois (RNF-009).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task LoginDevolveTokenComValidadeDeOitoHoras()
    {
        using var cliente = this.fabrica.CreateClient();
        var (email, _) = await ClienteApi.CriarContaAsync(cliente);
        var antes = DateTimeOffset.UtcNow;

        using var resposta = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email, palavraPasse = ClienteApi.PalavraPasse });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        Assert.Equal("Bearer", corpo.GetProperty("tipo").GetString());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("token").GetString()));
        var expiraEm = corpo.GetProperty("expiraEm").GetDateTimeOffset();
        Assert.InRange(expiraEm - antes, TimeSpan.FromHours(8) - TimeSpan.FromMinutes(1), TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// OP-02 e RF-012: e-mail inexistente e palavra-passe errada dão a mesma resposta 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task LoginComCredenciaisErradasDaMesmaResposta()
    {
        using var cliente = this.fabrica.CreateClient();
        var (email, _) = await ClienteApi.CriarContaAsync(cliente);

        using var errada = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email, palavraPasse = "Errada123" });
        using var inexistente = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email = DadosTeste.EmailUnico(), palavraPasse = "Errada123" });

        Assert.Equal(HttpStatusCode.Unauthorized, errada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(await errada.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
        Assert.Equal("CREDENCIAIS_INVALIDAS", await ClienteApi.CodigoErroAsync(errada));
    }

    /// <summary>
    /// RF-017: os endpoints protegidos recusam pedidos sem token ou com token alterado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EndpointProtegidoRecusaSemTokenOuComTokenAlterado()
    {
        var utilizador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var autenticado = utilizador.Cliente;
        using var semToken = this.fabrica.CreateClient();
        using var alterado = this.fabrica.CreateClient();
        var token = autenticado.DefaultRequestHeaders.Authorization!.Parameter!;
        alterado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^2] + (token[^2] == 'A' ? "BB" : "AA"));

        using var respostaSemToken = await semToken.GetAsync(new Uri("api/v1/conta", UriKind.Relative));
        using var respostaAlterado = await alterado.GetAsync(new Uri("api/v1/conta", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, respostaSemToken.StatusCode);
        Assert.Equal("NAO_AUTENTICADO", await ClienteApi.CodigoErroAsync(respostaSemToken));
        Assert.Equal(HttpStatusCode.Unauthorized, respostaAlterado.StatusCode);
    }

    /// <summary>
    /// OP-04 e OP-05: consulta e atualização dos dados da conta; e-mail de outra conta dá 409.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConsultaEAtualizacaoDaConta()
    {
        var utilizador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Ana");
        using var cliente = utilizador.Cliente;
        var (email, id) = (utilizador.Email, utilizador.Id);
        var (outroEmail, _) = await ClienteApi.CriarContaAsync(cliente, "Rui");
        var novoEmail = DadosTeste.EmailUnico("ana.costa");

        using var consulta = await cliente.GetAsync(new Uri("api/v1/conta", UriKind.Relative));
        using var atualizacao = await cliente.PutAsJsonAsync("api/v1/conta", new { nome = "Ana Costa", email = novoEmail });
        using var conflito = await cliente.PutAsJsonAsync("api/v1/conta", new { nome = "Ana Costa", email = outroEmail });

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var dados = await ClienteApi.JsonAsync(consulta);
        Assert.Equal(id, dados.GetProperty("id").GetGuid());
        Assert.Equal(email, dados.GetProperty("email").GetString());
        Assert.False(dados.TryGetProperty("palavraPasseHash", out _));
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        Assert.Equal(novoEmail, (await ClienteApi.JsonAsync(atualizacao)).GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode);
        Assert.Equal("EMAIL_JA_REGISTADO", await ClienteApi.CodigoErroAsync(conflito));
    }

    /// <summary>
    /// OP-06: a palavra-passe atual errada dá 403; a alteração correta faz a anterior deixar de ser aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AlteracaoDaPalavraPasse()
    {
        var utilizador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = utilizador.Cliente;
        var email = utilizador.Email;

        using var errada = await cliente.PutAsJsonAsync("api/v1/conta/palavra-passe", new { palavraPasseAtual = "Errada123", palavraPasseNova = "MaisSegura456" });
        using var certa = await cliente.PutAsJsonAsync("api/v1/conta/palavra-passe", new { palavraPasseAtual = ClienteApi.PalavraPasse, palavraPasseNova = "MaisSegura456" });
        using var loginAntigo = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email, palavraPasse = ClienteApi.PalavraPasse });
        using var loginNovo = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email, palavraPasse = "MaisSegura456" });

        Assert.Equal(HttpStatusCode.Forbidden, errada.StatusCode);
        Assert.Equal("PALAVRA_PASSE_ATUAL_INCORRETA", await ClienteApi.CodigoErroAsync(errada));
        Assert.Equal(HttpStatusCode.NoContent, certa.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAntigo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginNovo.StatusCode);
    }

    /// <summary>
    /// OP-03 e RF-016: depois de terminar a sessão, o mesmo token é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarSessaoInvalidaOToken()
    {
        var utilizador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = utilizador.Cliente;

        using var antes = await cliente.GetAsync(new Uri("api/v1/conta", UriKind.Relative));
        using var logout = await cliente.PostAsync(new Uri("api/v1/auth/logout", UriKind.Relative), null);
        using var depois = await cliente.GetAsync(new Uri("api/v1/conta", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
        Assert.Equal("NAO_AUTENTICADO", await ClienteApi.CodigoErroAsync(depois));
    }
}
