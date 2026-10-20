// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Operações HTTP comuns aos testes de integração: criar conta, iniciar sessão e ler respostas JSON.
/// </summary>
public static class ClienteApi
{
    /// <summary>
    /// Palavra-passe fictícia usada nas contas criadas pelos testes.
    /// </summary>
    public const string PalavraPasse = "Segura123";

    /// <summary>
    /// Cria uma conta e devolve o e-mail usado.
    /// </summary>
    /// <param name="cliente">Cliente HTTP da API.</param>
    /// <param name="nome">Nome da conta.</param>
    /// <param name="email">E-mail; se omitido, é gerado.</param>
    /// <returns>O e-mail e o identificador da conta criada.</returns>
    public static async Task<(string Email, Guid Id)> CriarContaAsync(HttpClient cliente, string nome = "Pessoa Fictícia", string? email = null)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        var endereco = email ?? DadosTeste.EmailUnico();
        using var resposta = await cliente.PostAsJsonAsync("api/v1/auth/registo", new { nome, email = endereco, palavraPasse = PalavraPasse });
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return (endereco, corpo.GetProperty("id").GetGuid());
    }

    /// <summary>
    /// Inicia sessão e devolve o token JWT.
    /// </summary>
    /// <param name="cliente">Cliente HTTP da API.</param>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="palavraPasse">Palavra-passe.</param>
    /// <returns>O token JWT.</returns>
    public static async Task<string> IniciarSessaoAsync(HttpClient cliente, string email, string palavraPasse = PalavraPasse)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        using var resposta = await cliente.PostAsJsonAsync("api/v1/auth/login", new { email, palavraPasse });
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Cria um cliente HTTP autenticado com uma conta nova.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="nome">Nome da conta.</param>
    /// <returns>O cliente autenticado, o e-mail e o identificador da conta.</returns>
    public static async Task<(HttpClient Cliente, string Email, Guid Id)> NovoUtilizadorAutenticadoAsync(FabricaApi fabrica, string nome = "Pessoa Fictícia")
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var cliente = fabrica.CreateClient();
        var (email, id) = await CriarContaAsync(cliente, nome);
        var token = await IniciarSessaoAsync(cliente, email);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (cliente, email, id);
    }

    /// <summary>
    /// Lê o corpo JSON de uma resposta.
    /// </summary>
    /// <param name="resposta">Resposta HTTP.</param>
    /// <returns>O elemento JSON da raiz.</returns>
    public static Task<JsonElement> JsonAsync(HttpResponseMessage resposta)
    {
        ArgumentNullException.ThrowIfNull(resposta);
        return resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Lê o código de erro de uma resposta de erro da API.
    /// </summary>
    /// <param name="resposta">Resposta HTTP de erro.</param>
    /// <returns>O código de erro.</returns>
    public static async Task<string?> CodigoErroAsync(HttpResponseMessage resposta)
    {
        var corpo = await JsonAsync(resposta);
        return corpo.GetProperty("codigo").GetString();
    }
}
