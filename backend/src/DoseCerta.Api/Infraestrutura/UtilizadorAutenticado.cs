// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Security.Claims;

namespace DoseCerta.Api.Infraestrutura;

/// <summary>
/// Obtém o identificador do Utilizador autenticado a partir das claims do token.
/// </summary>
/// <remarks>
/// Contrato com a I001: o token JWT emitido no login transporta o <c>Utilizador.Id</c> na claim <c>sub</c>.
/// </remarks>
public static class UtilizadorAutenticado
{
    /// <summary>
    /// Devolve o identificador do Utilizador autenticado.
    /// </summary>
    /// <param name="utilizador">Principal do pedido.</param>
    /// <returns>O identificador do Utilizador.</returns>
    /// <exception cref="UnauthorizedAccessException">Quando o token não identifica um Utilizador.</exception>
    public static Guid ObterId(this ClaimsPrincipal utilizador)
    {
        ArgumentNullException.ThrowIfNull(utilizador);

        var valor = utilizador.FindFirstValue(ClaimTypes.NameIdentifier) ?? utilizador.FindFirstValue("sub");
        return Guid.TryParse(valor, out var id)
            ? id
            : throw new UnauthorizedAccessException("O token não identifica um Utilizador.");
    }
}
