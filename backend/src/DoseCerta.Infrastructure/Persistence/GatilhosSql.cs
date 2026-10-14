// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.EntityFrameworkCore.Migrations;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Gatilhos de imutabilidade do histórico (modelo de dados, secção 13.2; DM-18; RNF-049).
/// O EF Core não gera gatilhos: as entidades declaram-nos com <c>HasTrigger</c> e a migração inicial
/// cria-os com <see cref="CriarGatilhosDoseCerta"/> e remove-os com <see cref="RemoverGatilhosDoseCerta"/>.
/// </summary>
public static class GatilhosSql
{
    /// <summary>
    /// Nome do gatilho que protege as tomas que saíram de Pendente.
    /// </summary>
    public const string NomeGatilhoToma = "TR_Toma_Historico";

    /// <summary>
    /// Nome do gatilho que protege as execuções realizadas.
    /// </summary>
    public const string NomeGatilhoExecucaoTarefa = "TR_ExecucaoTarefa_Historico";

    /// <summary>
    /// Nome do gatilho que impede alterar ou eliminar ocorrências.
    /// </summary>
    public const string NomeGatilhoOcorrencia = "TR_Ocorrencia_Imutavel";

    /// <summary>
    /// Nome do gatilho que impede alterar ou eliminar notas.
    /// </summary>
    public const string NomeGatilhoNota = "TR_Nota_Imutavel";

    /// <summary>
    /// Criação do gatilho da tabela Toma.
    /// </summary>
    public const string CriarGatilhoToma = """
        CREATE TRIGGER dbo.TR_Toma_Historico ON dbo.Toma
        AFTER UPDATE, DELETE
        AS
        BEGIN
          SET NOCOUNT ON;
          IF EXISTS (SELECT 1 FROM deleted WHERE Estado <> 'Pendente')
          BEGIN
            ROLLBACK TRANSACTION;
            THROW 51001, N'Uma toma confirmada ou não confirmada faz parte do histórico e não pode ser alterada nem eliminada.', 1;
          END;
        END;
        """;

    /// <summary>
    /// Criação do gatilho da tabela ExecucaoTarefa.
    /// </summary>
    public const string CriarGatilhoExecucaoTarefa = """
        CREATE TRIGGER dbo.TR_ExecucaoTarefa_Historico ON dbo.ExecucaoTarefa
        AFTER UPDATE, DELETE
        AS
        BEGIN
          SET NOCOUNT ON;
          IF EXISTS (SELECT 1 FROM deleted WHERE Estado <> 'PorConfirmar')
          BEGIN
            ROLLBACK TRANSACTION;
            THROW 51002, N'Uma execução realizada faz parte do histórico e não pode ser alterada nem eliminada.', 1;
          END;
        END;
        """;

    /// <summary>
    /// Criação do gatilho da tabela Ocorrencia.
    /// </summary>
    public const string CriarGatilhoOcorrencia = """
        CREATE TRIGGER dbo.TR_Ocorrencia_Imutavel ON dbo.Ocorrencia
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
          SET NOCOUNT ON;
          THROW 51003, N'As ocorrências não podem ser alteradas nem eliminadas.', 1;
        END;
        """;

    /// <summary>
    /// Criação do gatilho da tabela Nota.
    /// </summary>
    public const string CriarGatilhoNota = """
        CREATE TRIGGER dbo.TR_Nota_Imutavel ON dbo.Nota
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
          SET NOCOUNT ON;
          THROW 51004, N'As notas não podem ser alteradas nem eliminadas.', 1;
        END;
        """;

    /// <summary>
    /// Obtém os nomes dos quatro gatilhos do modelo de dados.
    /// </summary>
    /// <returns>Os nomes dos gatilhos.</returns>
    public static IReadOnlyList<string> Nomes()
    {
        return new[] { NomeGatilhoToma, NomeGatilhoExecucaoTarefa, NomeGatilhoOcorrencia, NomeGatilhoNota };
    }

    /// <summary>
    /// Cria os quatro gatilhos. Deve ser chamado no fim do método <c>Up</c> da migração inicial.
    /// </summary>
    /// <param name="migrationBuilder">Construtor da migração.</param>
    public static void CriarGatilhosDoseCerta(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql(CriarGatilhoToma);
        migrationBuilder.Sql(CriarGatilhoExecucaoTarefa);
        migrationBuilder.Sql(CriarGatilhoOcorrencia);
        migrationBuilder.Sql(CriarGatilhoNota);
    }

    /// <summary>
    /// Remove os quatro gatilhos. Deve ser chamado no início do método <c>Down</c> da migração inicial.
    /// </summary>
    /// <param name="migrationBuilder">Construtor da migração.</param>
    public static void RemoverGatilhosDoseCerta(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        foreach (var nome in Nomes())
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS dbo.{nome};");
        }
    }
}
