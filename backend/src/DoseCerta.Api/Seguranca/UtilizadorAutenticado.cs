// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Security.Claims;
using DoseCerta.Domain.Comum;

namespace DoseCerta.Api.Seguranca;

/// <summary>
/// Lê do token validado o identificador do Utilizador (<c>sub</c>), da credencial (<c>jti</c>) e a expiração (<c>exp</c>).
/// O papel numa ficha nunca é lido do token: é sempre lido da base de dados (RNF-004).
/// </summary>
public static class UtilizadorAutenticado
{
    /// <summary>
    /// Obtém o identificador do Utilizador autenticado.
    /// </summary>
    /// <param name="principal">Utilizador do pedido.</param>
    /// <returns>O identificador do Utilizador.</returns>
    /// <exception cref="DominioException">O token não tem um identificador válido (<c>NAO_AUTENTICADO</c>).</exception>
    public static Guid Id(ClaimsPrincipal principal)
    {
        return LerGuid(principal, "sub");
    }

    /// <summary>
    /// Obtém o identificador da credencial (<c>jti</c>).
    /// </summary>
    /// <param name="principal">Utilizador do pedido.</param>
    /// <returns>O identificador da credencial.</returns>
    /// <exception cref="DominioException">O token não tem um identificador válido (<c>NAO_AUTENTICADO</c>).</exception>
    public static Guid Jti(ClaimsPrincipal principal)
    {
        return LerGuid(principal, "jti");
    }

    /// <summary>
    /// Obtém o instante de expiração do token, em UTC.
    /// </summary>
    /// <param name="principal">Utilizador do pedido.</param>
    /// <returns>O instante de expiração, em UTC.</returns>
    /// <exception cref="DominioException">O token não tem uma expiração válida (<c>NAO_AUTENTICADO</c>).</exception>
    public static DateTime ExpiraEmUtc(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var valor = principal.FindFirst("exp")?.Value;
        if (!long.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos))
        {
            throw NaoAutenticado();
        }

        return DateTimeOffset.FromUnixTimeSeconds(segundos).UtcDateTime;
    }

    private static Guid LerGuid(ClaimsPrincipal principal, string tipo)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return Guid.TryParse(principal.FindFirst(tipo)?.Value, out var id) ? id : throw NaoAutenticado();
    }

    private static DominioException NaoAutenticado()
    {
        return new DominioException(CodigosErro.NaoAutenticado, ValidadorDeCredencial.MensagemNaoAutenticado);
    }
}
