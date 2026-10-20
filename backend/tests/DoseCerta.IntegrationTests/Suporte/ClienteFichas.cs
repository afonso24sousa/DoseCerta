// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Pedidos e dados de apoio aos testes de integração das fichas e dos documentos (F002).
/// </summary>
public static class ClienteFichas
{
    /// <summary>
    /// Gets o conteúdo de um PDF mínimo.
    /// </summary>
    public static byte[] Pdf { get; } = Encoding.ASCII.GetBytes("%PDF-1.4\n%fictício\n%%EOF");

    /// <summary>
    /// Gets o cabeçalho de um PNG.
    /// </summary>
    public static byte[] Png { get; } = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };

    /// <summary>
    /// Cria o corpo de um pedido de criação de ficha de terceiro, com o criador como Cuidador principal.
    /// </summary>
    /// <param name="nome">Nome do utente.</param>
    /// <returns>O corpo do pedido.</returns>
    public static object PedidoTerceiro(string nome = "Maria Fictícia Santos")
    {
        return new
        {
            propria = false,
            nomeCompleto = nome,
            dataNascimento = "1941-05-17",
            contactoEmergencia = "+351 910 000 001",
            grauAutonomiaId = 2,
            condicoesSaudeIds = new[] { 1 },
            alergiasIds = new[] { 1 },
            cuidadorPrincipal = new { eu = true },
        };
    }

    /// <summary>
    /// Cria uma ficha de terceiro com o Utilizador do cliente como Cuidador principal.
    /// </summary>
    /// <param name="cliente">Cliente autenticado.</param>
    /// <param name="nome">Nome do utente.</param>
    /// <returns>O identificador da ficha.</returns>
    public static async Task<Guid> CriarFichaAsync(HttpClient cliente, string nome = "Maria Fictícia Santos")
    {
        ArgumentNullException.ThrowIfNull(cliente);
        using var resposta = await cliente.PostAsJsonAsync("api/v1/fichas", PedidoTerceiro(nome));
        resposta.EnsureSuccessStatusCode();
        var corpo = await ClienteApi.JsonAsync(resposta);
        return corpo.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Associa diretamente na base de dados um Utilizador a uma ficha (os convites são implementados com a F004).
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="utilizadorId">Utilizador.</param>
    /// <param name="papel">Papel na ficha.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public static Task AssociarAsync(FabricaApi fabrica, Guid fichaId, Guid utilizadorId, Papel papel)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.ComContextoAsync(async contexto =>
        {
            contexto.Associacoes.Add(new Associacao(fichaId, utilizadorId, papel, DadosTeste.Agora()));
            return await contexto.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Cria um pedido <c>multipart/form-data</c> com a parte <c>dados</c> e partes <c>documento</c> e <c>categoria</c>.
    /// </summary>
    /// <param name="dados">Corpo JSON do pedido.</param>
    /// <param name="documentos">Documentos: conteúdo, nome do ficheiro e categoria.</param>
    /// <returns>O conteúdo do pedido.</returns>
    public static MultipartFormDataContent Multipart(object dados, params (byte[] Conteudo, string Nome, string Categoria)[] documentos)
    {
        ArgumentNullException.ThrowIfNull(documentos);
        var conteudo = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(dados), Encoding.UTF8, "application/json"), "dados" },
        };
        foreach (var (bytes, nome, categoria) in documentos)
        {
            conteudo.Add(Ficheiro(bytes), "documento", nome);
            conteudo.Add(new StringContent(categoria), "categoria");
        }

        return conteudo;
    }

    /// <summary>
    /// Cria o pedido <c>multipart/form-data</c> de OP-15, com as partes <c>categoria</c> e <c>ficheiro</c>.
    /// </summary>
    /// <param name="bytes">Conteúdo do ficheiro.</param>
    /// <param name="nome">Nome do ficheiro.</param>
    /// <param name="categoria">Categoria do documento.</param>
    /// <returns>O conteúdo do pedido.</returns>
    public static MultipartFormDataContent Carregamento(byte[] bytes, string nome = "relatorio.pdf", string categoria = "RelatorioClinico")
    {
        return new MultipartFormDataContent
        {
            { new StringContent(categoria), "categoria" },
            { Ficheiro(bytes), "ficheiro", nome },
        };
    }

    private static ByteArrayContent Ficheiro(byte[] bytes)
    {
        var ficheiro = new ByteArrayContent(bytes);
        ficheiro.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return ficheiro;
    }
}
