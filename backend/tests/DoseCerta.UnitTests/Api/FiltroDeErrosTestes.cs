// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json;
using DoseCerta.Api.Erros;
using DoseCerta.Domain.Comum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários da tradução de erros em respostas da API (Documentação da API, secção 3.3).
/// </summary>
public class FiltroDeErrosTestes
{
    /// <summary>
    /// Cada código de erro tem o código HTTP definido na Documentação da API.
    /// </summary>
    /// <param name="codigo">Código de erro.</param>
    /// <param name="estado">Código HTTP esperado.</param>
    [Theory]
    [InlineData(CodigosErro.ValidacaoInvalida, 400)]
    [InlineData(CodigosErro.ErroInterno, 500)]
    public void EstadoHttpCorrespondeAoContrato(string codigo, int estado)
    {
        Assert.Equal(estado, FiltroDeErros.EstadoHttp(codigo));
    }

    /// <summary>
    /// Um código desconhecido dá 500.
    /// </summary>
    [Fact]
    public void EstadoHttpDeCodigoDesconhecidoE500()
    {
        Assert.Equal(StatusCodes.Status500InternalServerError, FiltroDeErros.EstadoHttp("CODIGO_INEXISTENTE"));
    }

    /// <summary>
    /// Uma exceção de validação dá 400 com os campos em erro.
    /// </summary>
    [Fact]
    public void ConverterValidacaoIncluiOsCampos()
    {
        var erro = ValidacaoException.DoCampo("email", "Tem de ter um formato de e-mail válido.");

        var (estado, corpo) = FiltroDeErros.Converter(erro);

        Assert.Equal(400, estado);
        Assert.Equal(CodigosErro.ValidacaoInvalida, corpo.Codigo);
        var campo = Assert.Single(corpo.Campos!);
        Assert.Equal("email", campo.Campo);
    }

    /// <summary>
    /// Uma exceção de domínio dá o código HTTP do código de erro, sem lista de campos.
    /// </summary>
    [Fact]
    public void ConverterDominioUsaOCodigoDaExcecao()
    {
        var (estado, corpo) = FiltroDeErros.Converter(new DominioException(CodigosErro.ValidacaoInvalida, "Inválido."));

        Assert.Equal(400, estado);
        Assert.Equal(CodigosErro.ValidacaoInvalida, corpo.Codigo);
        Assert.Null(corpo.Campos);
    }

    /// <summary>
    /// Os construtores sem código da exceção de domínio usam o código de erro interno;
    /// os da exceção de validação usam o código de validação, sem campos.
    /// </summary>
    [Fact]
    public void ConstrutoresDasExcecoesDefinemOCodigo()
    {
        var causa = new InvalidOperationException("causa");

        Assert.Equal(CodigosErro.ErroInterno, new DominioException().Codigo);
        Assert.Equal(CodigosErro.ErroInterno, new DominioException("Erro.").Codigo);
        Assert.Same(causa, new DominioException("Erro.", causa).InnerException);
        Assert.Empty(new ValidacaoException().Campos);
        Assert.Equal(CodigosErro.ValidacaoInvalida, new ValidacaoException("Inválido.").Codigo);
        Assert.Empty(new ValidacaoException("Inválido.", causa).Campos);
    }

    /// <summary>
    /// Um pedido mal formado dá 400 com o código de validação.
    /// </summary>
    [Fact]
    public void ConverterPedidoMalFormadoDa400()
    {
        var (estado, corpo) = FiltroDeErros.Converter(new BadHttpRequestException("mal formado"));

        Assert.Equal(400, estado);
        Assert.Equal(CodigosErro.ValidacaoInvalida, corpo.Codigo);
    }

    /// <summary>
    /// Um erro inesperado dá 500 sem expor a mensagem interna.
    /// </summary>
    [Fact]
    public void ConverterErroInesperadoNaoExpoeDetalhes()
    {
        var (estado, corpo) = FiltroDeErros.Converter(new InvalidOperationException("detalhe interno"));

        Assert.Equal(500, estado);
        Assert.Equal(CodigosErro.ErroInterno, corpo.Codigo);
        Assert.DoesNotContain("detalhe interno", corpo.Mensagem, StringComparison.Ordinal);
    }

    /// <summary>
    /// O filtro escreve a resposta com o código HTTP e o corpo comum dos erros, sem detalhes internos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TryHandleEscreveARespostaDeErro()
    {
        var filtro = new FiltroDeErros(NullLogger<FiltroDeErros>.Instance);
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        var tratado = await filtro.TryHandleAsync(contexto, new InvalidOperationException("detalhe interno"), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var corpo = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.Equal(CodigosErro.ErroInterno, corpo.RootElement.GetProperty("codigo").GetString());
        Assert.DoesNotContain("detalhe interno", corpo.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Os erros de formato detetados pelo ASP.NET Core dão 400 com os campos em erro.
    /// </summary>
    [Fact]
    public void RespostaValidacaoModeloListaOsCampos()
    {
        var estado = new ModelStateDictionary();
        estado.AddModelError("$.dataNascimento", "formato");
        estado.AddModelError("$", "corpo");
        var contexto = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), estado);

        var resultado = Assert.IsType<BadRequestObjectResult>(RespostaValidacaoModelo.Criar(contexto));

        var corpo = Assert.IsType<ProblemaApi>(resultado.Value);
        Assert.Equal(CodigosErro.ValidacaoInvalida, corpo.Codigo);
        Assert.Equal(2, corpo.Campos!.Count);
        Assert.Contains(corpo.Campos, c => c.Campo == "dataNascimento");
        Assert.Contains(corpo.Campos, c => c.Campo == "corpo");
    }

    /// <summary>
    /// As chaves do estado do modelo são convertidas no nome do campo em camelCase.
    /// </summary>
    /// <param name="chave">Chave do estado do modelo.</param>
    /// <param name="campo">Nome do campo esperado.</param>
    [Theory]
    [InlineData("$.dataNascimento", "dataNascimento")]
    [InlineData("Email", "email")]
    [InlineData("$", "corpo")]
    [InlineData("", "corpo")]
    public void NomeDoCampoNormalizaAChave(string chave, string campo)
    {
        Assert.Equal(campo, RespostaValidacaoModelo.NomeDoCampo(chave));
    }
}
