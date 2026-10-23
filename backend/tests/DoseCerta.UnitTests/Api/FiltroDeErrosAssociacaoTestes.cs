// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Erros;
using DoseCerta.Domain.Comum;

namespace DoseCerta.UnitTests.Api;

/// <summary>
/// Testes unitários da tradução dos erros da associação de Cuidadores e Familiares (F004; Documentação da API, OP-19 a OP-26).
/// </summary>
public class FiltroDeErrosAssociacaoTestes
{
    /// <summary>
    /// Cada código de F004 tem o texto da documentação da API e dá o código HTTP documentado, com o código e a mensagem no corpo.
    /// </summary>
    /// <param name="codigo">Constante de <see cref="CodigosErro"/>.</param>
    /// <param name="textoDocumentado">Código de erro como aparece na documentação da API.</param>
    /// <param name="estadoEsperado">Código HTTP documentado.</param>
    [Theory]
    [InlineData(CodigosErro.ConvitePendenteExiste, "CONVITE_PENDENTE_EXISTE", 409)]
    [InlineData(CodigosErro.PessoaJaAssociada, "PESSOA_JA_ASSOCIADA", 409)]
    [InlineData(CodigosErro.ConviteNaoEncontrado, "CONVITE_NAO_ENCONTRADO", 404)]
    [InlineData(CodigosErro.ConviteJaRespondido, "CONVITE_JA_RESPONDIDO", 409)]
    [InlineData(CodigosErro.PessoaNaoEncontrada, "PESSOA_NAO_ENCONTRADA", 404)]
    [InlineData(CodigosErro.CuidadorPrincipalSemSubstituto, "CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO", 409)]
    [InlineData(CodigosErro.FichaSemCuidador, "FICHA_SEM_CUIDADOR", 409)]
    [InlineData(CodigosErro.DestinatarioNaoECuidador, "DESTINATARIO_NAO_E_CUIDADOR", 409)]
    public void CodigoDeF004DaOEstadoDocumentado(string codigo, string textoDocumentado, int estadoEsperado)
    {
        const string Mensagem = "Mensagem de teste.";

        var (estado, corpo) = FiltroDeErros.Converter(new DominioException(codigo, Mensagem));

        Assert.Equal(textoDocumentado, codigo);
        Assert.Equal(estadoEsperado, FiltroDeErros.EstadoHttp(codigo));
        Assert.Equal(estadoEsperado, estado);
        Assert.Equal(textoDocumentado, corpo.Codigo);
        Assert.Equal(Mensagem, corpo.Mensagem);
        Assert.Null(corpo.Campos);
    }
}
