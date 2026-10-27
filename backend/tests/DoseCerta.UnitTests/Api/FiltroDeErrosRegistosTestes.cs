// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Erros;
using DoseCerta.Domain.Comum;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários da tradução do erro de imutabilidade das ocorrências e das notas (F009; Documentação da API, secção 4).
/// </summary>
public class FiltroDeErrosRegistosTestes
{
    /// <summary>
    /// <c>REGISTO_IMUTAVEL</c> dá 405, com o código e a mensagem da exceção no corpo (RF-090, RF-091, RNF-049).
    /// </summary>
    [Fact]
    public void RegistoImutavelDa405()
    {
        const string Mensagem = "As ocorrências e as notas não podem ser alteradas nem eliminadas.";

        var (estado, corpo) = FiltroDeErros.Converter(new DominioException(CodigosErro.RegistoImutavel, Mensagem));

        Assert.Equal(405, FiltroDeErros.EstadoHttp(CodigosErro.RegistoImutavel));
        Assert.Equal(405, estado);
        Assert.Equal("REGISTO_IMUTAVEL", corpo.Codigo);
        Assert.Equal(Mensagem, corpo.Mensagem);
        Assert.Null(corpo.Campos);
    }
}
