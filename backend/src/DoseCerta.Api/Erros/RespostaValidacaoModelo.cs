// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DoseCerta.Api.Erros;

/// <summary>
/// Converte os erros de formato do pedido detetados pelo ASP.NET Core (por exemplo, JSON inválido
/// ou um valor que não pode ser convertido) numa resposta <c>VALIDACAO_INVALIDA</c> com o campo em erro (RNF-028).
/// </summary>
public static class RespostaValidacaoModelo
{
    /// <summary>
    /// Cria a resposta 400 a partir do estado do modelo.
    /// </summary>
    /// <param name="contexto">Contexto da ação.</param>
    /// <returns>A resposta com o corpo <see cref="ProblemaApi"/>.</returns>
    public static IActionResult Criar(ActionContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var campos = Campos(contexto.ModelState);
        var corpo = new ProblemaApi(CodigosErro.ValidacaoInvalida, "Os dados submetidos são inválidos.", campos);
        return new BadRequestObjectResult(corpo);
    }

    /// <summary>
    /// Obtém os campos em erro a partir do estado do modelo.
    /// </summary>
    /// <param name="estado">Estado do modelo.</param>
    /// <returns>A lista de campos em erro.</returns>
    public static IReadOnlyList<ErroCampoApi> Campos(ModelStateDictionary estado)
    {
        ArgumentNullException.ThrowIfNull(estado);
        return estado
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => new ErroCampoApi(NomeDoCampo(e.Key), RegraDoCampo(e.Key)))
            .DistinctBy(c => c.Campo)
            .ToList();
    }

    /// <summary>
    /// Normaliza a chave do estado do modelo para o nome do campo em camelCase.
    /// </summary>
    /// <param name="chave">Chave do estado do modelo, por exemplo <c>$.dataNascimento</c>.</param>
    /// <returns>O nome do campo.</returns>
    public static string NomeDoCampo(string chave)
    {
        var nome = (chave ?? string.Empty).Replace("$.", string.Empty, StringComparison.Ordinal).Trim('$', '.');
        if (nome.Length == 0)
        {
            return "corpo";
        }

        return char.ToLowerInvariant(nome[0]) + nome[1..];
    }

    private static string RegraDoCampo(string chave)
    {
        return NomeDoCampo(chave) == "corpo"
            ? "O corpo do pedido é obrigatório e tem de ser JSON válido."
            : "O valor tem um formato inválido para este campo.";
    }
}
