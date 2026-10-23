// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Erros;
using DoseCerta.Domain.Comum;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários da tradução do erro de proteção do titular da ficha própria (F003; Documentação da API, OP-24 a OP-26).
/// </summary>
public class FiltroDeErrosFichaPropriaTestes
{
    /// <summary>
    /// <c>CUIDADOR_PRINCIPAL_PROTEGIDO</c> dá 403, com o código e a mensagem da exceção no corpo (RF-033, RF-034).
    /// </summary>
    [Fact]
    public void CuidadorPrincipalProtegidoDa403()
    {
        const string Mensagem = "O titular da ficha própria é sempre o Cuidador principal.";

        var (estado, corpo) = FiltroDeErros.Converter(new DominioException(CodigosErro.CuidadorPrincipalProtegido, Mensagem));

        Assert.Equal(403, FiltroDeErros.EstadoHttp(CodigosErro.CuidadorPrincipalProtegido));
        Assert.Equal(403, estado);
        Assert.Equal("CUIDADOR_PRINCIPAL_PROTEGIDO", corpo.Codigo);
        Assert.Equal(Mensagem, corpo.Mensagem);
        Assert.Null(corpo.Campos);
    }
}
