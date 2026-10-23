// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários da conversão das violações dos índices únicos da associação a uma ficha (F004, RF-043; D8).
/// </summary>
public class ConversorViolacaoUnicidadeAssociacaoTestes
{
    /// <summary>
    /// RF-043: a violação de <c>UQ_Convite_Pendente</c> dá <c>CONVITE_PENDENTE_EXISTE</c>, com a mesma mensagem da verificação prévia.
    /// </summary>
    /// <param name="mensagemSql">Mensagem do SQL Server (erro 2601 ou 2627).</param>
    [Theory]
    [InlineData("Cannot insert duplicate key row in object 'dbo.Convite' with unique index 'UQ_Convite_Pendente'. The duplicate key value is (0a1b, rui@exemplo.pt).")]
    [InlineData("Violation of UNIQUE KEY constraint 'UQ_Convite_Pendente'. Cannot insert duplicate key in object 'dbo.Convite'.")]
    public void ConvitePendenteRepetidoDaConvitePendenteExiste(string mensagemSql)
    {
        var erro = ConversorViolacaoUnicidade.DaMensagem(mensagemSql);

        Assert.NotNull(erro);
        Assert.Equal(CodigosErro.ConvitePendenteExiste, erro.Codigo);
        Assert.Equal(ConviteService.MensagemConvitePendenteExiste, erro.Message);
    }

    /// <summary>
    /// D8: a violação de <c>UQ_Associacao_FichaUtilizador</c> dá <c>PESSOA_JA_ASSOCIADA</c>, com a mesma mensagem do domínio.
    /// </summary>
    [Fact]
    public void AssociacaoRepetidaDaPessoaJaAssociada()
    {
        var erro = ConversorViolacaoUnicidade.DaMensagem(
            "Cannot insert duplicate key row in object 'dbo.Associacao' with unique index 'UQ_Associacao_FichaUtilizador'. The duplicate key value is (0a1b, 2c3d).");

        Assert.NotNull(erro);
        Assert.Equal(CodigosErro.PessoaJaAssociada, erro.Codigo);
        Assert.Equal(Ficha.MensagemPessoaJaAssociada, erro.Message);
    }

    /// <summary>
    /// D8, limitação conhecida: a violação de <c>UQ_Associacao_Principal</c> não é convertida, porque a documentação da API não define
    /// o erro de duas transferências do Cuidador principal em simultâneo.
    /// </summary>
    [Fact]
    public void IndiceDoCuidadorPrincipalNaoEConvertido()
    {
        Assert.Null(ConversorViolacaoUnicidade.DaMensagem(
            "Cannot insert duplicate key row in object 'dbo.Associacao' with unique index 'UQ_Associacao_Principal'. The duplicate key value is (0a1b)."));
    }
}
