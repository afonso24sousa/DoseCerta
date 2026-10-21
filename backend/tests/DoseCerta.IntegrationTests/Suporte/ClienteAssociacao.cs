// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Pedidos e dados de apoio aos testes de integração da associação de Cuidadores e Familiares (F004).
/// </summary>
public static class ClienteAssociacao
{
    /// <summary>
    /// Cria uma conta com o nome e o e-mail indicados e devolve um cliente autenticado com ela.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="nome">Nome da conta.</param>
    /// <param name="email">E-mail da conta.</param>
    /// <returns>O cliente autenticado e o identificador da conta.</returns>
    public static async Task<(HttpClient Cliente, Guid Id)> NovoUtilizadorComEmailAsync(FabricaApi fabrica, string nome, string email)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var cliente = fabrica.CreateClient();
        var (_, id) = await ClienteApi.CriarContaAsync(cliente, nome, email);
        var token = await ClienteApi.IniciarSessaoAsync(cliente, email);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (cliente, id);
    }

    /// <summary>
    /// Cria uma ficha de terceiro com convite para Cuidador principal: o criador fica Cuidador principal provisório (RF-025).
    /// </summary>
    /// <param name="cliente">Cliente autenticado do criador.</param>
    /// <param name="emailConvidado">E-mail da pessoa convidada para Cuidador principal.</param>
    /// <returns>O identificador da ficha.</returns>
    public static async Task<Guid> CriarFichaComConviteParaPrincipalAsync(HttpClient cliente, string emailConvidado)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        using var resposta = await cliente.PostAsJsonAsync("api/v1/fichas", new
        {
            propria = false,
            nomeCompleto = "Maria Fictícia Santos",
            dataNascimento = "1941-05-17",
            contactoEmergencia = "+351 910 000 001",
            cuidadorPrincipal = new { eu = false, email = emailConvidado },
        });
        resposta.EnsureSuccessStatusCode();
        var corpo = await ClienteApi.JsonAsync(resposta);
        return corpo.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Pede a lista das pessoas associadas a uma ficha (OP-18) e devolve-a, exigindo 200.
    /// </summary>
    /// <param name="cliente">Cliente autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <returns>As pessoas, pela ordem da resposta.</returns>
    public static async Task<IReadOnlyList<JsonElement>> PessoasAsync(HttpClient cliente, Guid fichaId)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        using var resposta = await cliente.GetAsync($"api/v1/fichas/{fichaId}/pessoas");
        resposta.EnsureSuccessStatusCode();
        var corpo = await ClienteApi.JsonAsync(resposta);
        return corpo.EnumerateArray().ToList();
    }
}
