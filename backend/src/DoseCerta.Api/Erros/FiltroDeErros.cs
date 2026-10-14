// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Collections.Frozen;
using DoseCerta.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;

namespace DoseCerta.Api.Erros;

/// <summary>
/// Traduz as exceções de domínio e de validação em respostas <see cref="ProblemaApi"/>, com o código HTTP
/// e o código de erro da API (Documentação da API, secção 3.3; RNF-028). Os erros inesperados dão 500
/// sem detalhes internos na resposta e ficam registados no log.
/// </summary>
public sealed partial class FiltroDeErros : IExceptionHandler
{
    private static readonly FrozenDictionary<string, int> EstadosPorCodigo = new Dictionary<string, int>
    {
        [CodigosErro.ValidacaoInvalida] = StatusCodes.Status400BadRequest,
        [CodigosErro.ErroInterno] = StatusCodes.Status500InternalServerError,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private readonly ILogger<FiltroDeErros> registo;

    /// <summary>
    /// Initializes a new instance of the <see cref="FiltroDeErros"/> class.
    /// </summary>
    /// <param name="registo">Registo de eventos.</param>
    public FiltroDeErros(ILogger<FiltroDeErros> registo)
    {
        this.registo = registo;
    }

    /// <summary>
    /// Obtém o código HTTP associado a um código de erro da API.
    /// </summary>
    /// <param name="codigo">Código de erro da API.</param>
    /// <returns>O código HTTP; 500 se o código não for conhecido.</returns>
    public static int EstadoHttp(string codigo)
    {
        return EstadosPorCodigo.TryGetValue(codigo, out var estado) ? estado : StatusCodes.Status500InternalServerError;
    }

    /// <summary>
    /// Converte uma exceção no corpo e no código HTTP da resposta.
    /// </summary>
    /// <param name="erro">Exceção a converter.</param>
    /// <returns>O código HTTP e o corpo da resposta.</returns>
    public static (int Estado, ProblemaApi Corpo) Converter(Exception erro)
    {
        return erro switch
        {
            ValidacaoException validacao => (
                StatusCodes.Status400BadRequest,
                new ProblemaApi(
                    validacao.Codigo,
                    validacao.Message,
                    validacao.Campos.Select(c => new ErroCampoApi(c.Campo, c.Regra)).ToList())),
            DominioException dominio => (EstadoHttp(dominio.Codigo), new ProblemaApi(dominio.Codigo, dominio.Message)),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                new ProblemaApi(CodigosErro.ValidacaoInvalida, "O pedido é inválido.")),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemaApi(CodigosErro.ErroInterno, "Ocorreu um erro inesperado. Tente novamente mais tarde.")),
        };
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (estado, corpo) = Converter(exception);
        if (estado == StatusCodes.Status500InternalServerError)
        {
            this.RegistarErroInesperado(httpContext.Request.Method, httpContext.Request.Path, exception);
        }

        httpContext.Response.StatusCode = estado;
        await httpContext.Response.WriteAsJsonAsync(corpo, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Error, Message = "Erro inesperado ao tratar {Metodo} {Caminho}.")]
    private partial void RegistarErroInesperado(string metodo, string caminho, Exception erro);
}
