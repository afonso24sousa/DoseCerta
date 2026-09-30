// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Infraestrutura;

/// <summary>
/// Converte as exceções de negócio e de validação em respostas <c>application/problem+json</c> claras,
/// em vez de erros 500 genéricos.
/// </summary>
public sealed class TratadorExcecoes : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetails;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratadorExcecoes"/> class.
    /// </summary>
    /// <param name="problemDetails">Serviço de escrita de <see cref="ProblemDetails"/>.</param>
    public TratadorExcecoes(IProblemDetailsService problemDetails)
    {
        this.problemDetails = problemDetails;
    }

    /// <summary>
    /// Devolve o código HTTP correspondente a um código de erro de negócio.
    /// </summary>
    /// <param name="codigo">Código de erro.</param>
    /// <returns>O código HTTP.</returns>
    public static int EstadoHttp(CodigoErro? codigo) => codigo switch
    {
        CodigoErro.UtilizadorNaoEncontrado => StatusCodes.Status401Unauthorized,
        CodigoErro.FichaNaoEncontrada or CodigoErro.AssociacaoNaoEncontrada => StatusCodes.Status404NotFound,
        CodigoErro.AcessoNegado => StatusCodes.Status403Forbidden,
        CodigoErro.FichaPropriaJaExiste
            or CodigoErro.CuidadorPrincipalFichaPropriaImutavel
            or CodigoErro.AssociacaoJaExiste
            or CodigoErro.FichaComHistorico
            or CodigoErro.EstadoFichaInvalido => StatusCodes.Status409Conflict,
        CodigoErro.FalhaGravacaoDocumento => StatusCodes.Status500InternalServerError,
        CodigoErro.NaoImplementado => StatusCodes.Status501NotImplemented,
        _ => StatusCodes.Status400BadRequest,
    };

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        ProblemDetails? detalhe = exception switch
        {
            ValidacaoException validacao => new ValidationProblemDetails(validacao.Erros.ToDictionary(e => e.Key, e => e.Value))
            {
                Title = validacao.Message,
                Status = StatusCodes.Status400BadRequest,
            },
            RegraNegocioException regra => new ProblemDetails
            {
                Title = regra.Message,
                Status = EstadoHttp(regra.Codigo),
                Extensions = { ["codigo"] = regra.Codigo?.ToString() },
            },
            UnauthorizedAccessException acesso => new ProblemDetails
            {
                Title = acesso.Message,
                Status = StatusCodes.Status401Unauthorized,
            },
            _ => null,
        };

        if (detalhe is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = detalhe.Status!.Value;
        return await this.problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = detalhe,
            Exception = exception,
        });
    }
}
