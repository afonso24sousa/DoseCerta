// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários da conversão das violações de índices únicos em erros de negócio (RF-031).
/// O caminho completo, com a exceção real do SQL Server, é testado na integração (<c>UnidadeDeTrabalhoUnicidadeTestes</c>).
/// </summary>
public class ConversorViolacaoUnicidadeTestes
{
    /// <summary>
    /// A mensagem do SQL Server com o índice da ficha própria dá <c>FICHA_PROPRIA_JA_EXISTE</c>, com a mesma mensagem da verificação prévia.
    /// </summary>
    /// <param name="mensagemSql">Mensagem do SQL Server (erro 2601 ou 2627).</param>
    [Theory]
    [InlineData("Cannot insert duplicate key row in object 'dbo.Ficha' with unique index 'UQ_Ficha_UtilizadorProprio'. The duplicate key value is (0a1b).")]
    [InlineData("Violation of UNIQUE KEY constraint 'UQ_Ficha_UtilizadorProprio'. Cannot insert duplicate key in object 'dbo.Ficha'.")]
    public void IndiceDaFichaPropriaDaFichaPropriaJaExiste(string mensagemSql)
    {
        var erro = ConversorViolacaoUnicidade.DaMensagem(mensagemSql);

        Assert.NotNull(erro);
        Assert.Equal(CodigosErro.FichaPropriaJaExiste, erro.Codigo);
        Assert.Equal(FichaService.MensagemFichaPropriaJaExiste, erro.Message);
    }

    /// <summary>
    /// Outros índices únicos, ou nomes que só começam como o da ficha própria, não são convertidos.
    /// </summary>
    /// <param name="mensagemSql">Mensagem do SQL Server.</param>
    [Theory]
    [InlineData("Cannot insert duplicate key row in object 'dbo.Utilizador' with unique index 'UQ_Utilizador_Email'.")]
    [InlineData("Cannot insert duplicate key row in object 'dbo.Ficha' with unique index 'UQ_Ficha_UtilizadorProprioOutro'.")]
    [InlineData("")]
    public void OutrosIndicesNaoSaoConvertidos(string mensagemSql)
    {
        Assert.Null(ConversorViolacaoUnicidade.DaMensagem(mensagemSql));
    }

    /// <summary>
    /// Uma falha de gravação que não vem do SQL Server não é convertida, mesmo que a mensagem refira o índice.
    /// </summary>
    [Fact]
    public void FalhaSemErroDoSqlServerNaoEConvertida()
    {
        var semCausa = new DbUpdateException("Falha simulada.");
        var outraCausa = new DbUpdateException("Falha simulada.", new InvalidOperationException("'UQ_Ficha_UtilizadorProprio'"));

        Assert.Null(ConversorViolacaoUnicidade.Converter(semCausa));
        Assert.Null(ConversorViolacaoUnicidade.Converter(outraCausa));
        Assert.Throws<ArgumentNullException>(() => ConversorViolacaoUnicidade.Converter(null!));
    }

    /// <summary>
    /// O nome do índice convertido é o do modelo EF, que é único e filtrado; assim, mudar o nome no modelo faz falhar este teste.
    /// </summary>
    [Fact]
    public void IndiceConvertidoExisteNoModelo()
    {
        using var contexto = new FabricaDesignDoseCertaDbContext().CreateDbContext(Array.Empty<string>());
        var ficha = contexto.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Ficha))!;

        var indice = Assert.Single(ficha.GetIndexes(), i => i.GetDatabaseName() == ConversorViolacaoUnicidade.IndiceFichaPropria);

        Assert.True(indice.IsUnique);
        Assert.Equal("[UtilizadorProprioId] IS NOT NULL", indice.GetFilter());
    }
}
