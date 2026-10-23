// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json;
using DoseCerta.Domain.Comum;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Leitura dos pedidos <c>multipart/form-data</c> com ficheiros: a parte <c>dados</c> leva o JSON do pedido.
/// </summary>
internal static class LeitorMultipart
{
    /// <summary>
    /// Nome da parte com o JSON do pedido.
    /// </summary>
    public const string ParteDados = "dados";

    /// <summary>
    /// Lê e converte o JSON da parte <c>dados</c>.
    /// </summary>
    /// <typeparam name="T">Tipo do pedido.</typeparam>
    /// <param name="formulario">Formulário recebido.</param>
    /// <param name="opcoesJson">Opções JSON da API.</param>
    /// <returns>O pedido.</returns>
    /// <exception cref="ValidacaoException">A parte está em falta ou não tem JSON válido (RNF-028).</exception>
    public static T LerDados<T>(IFormCollection formulario, JsonSerializerOptions opcoesJson)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(formulario);

        var json = formulario[ParteDados].ToString();
        const string Regra = "É obrigatória e tem de conter os dados do pedido em JSON válido.";
        if (string.IsNullOrWhiteSpace(json))
        {
            throw ValidacaoException.DoCampo(ParteDados, Regra);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, opcoesJson) ?? throw ValidacaoException.DoCampo(ParteDados, Regra);
        }
        catch (JsonException)
        {
            throw ValidacaoException.DoCampo(ParteDados, Regra);
        }
    }
}
