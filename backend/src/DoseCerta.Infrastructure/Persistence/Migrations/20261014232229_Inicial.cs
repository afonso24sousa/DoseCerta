using System;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DoseCerta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Alergia",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alergia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CondicaoSaude",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CondicaoSaude", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrauAutonomia",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ordem = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrauAutonomia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilizador",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    PalavraPasseHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizador", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ficha",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    NomeCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ContactoEmergencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GrauAutonomiaId = table.Column<short>(type: "smallint", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UtilizadorProprioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'Ativa'"),
                    CriadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AtivaDesdeUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ArquivadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ficha", x => x.Id);
                    table.CheckConstraint("CK_Ficha_Arquivo", "([Estado] = 'Arquivada' AND [ArquivadaEmUtc] IS NOT NULL AND [ArquivadaEmUtc] >= [AtivaDesdeUtc]) OR ([Estado] = 'Ativa' AND [ArquivadaEmUtc] IS NULL)");
                    table.CheckConstraint("CK_Ficha_Estado", "[Estado] IN ('Ativa','Arquivada')");
                    table.CheckConstraint("CK_Ficha_Nascimento", "[DataNascimento] >= '1900-01-01'");
                    table.CheckConstraint("CK_Ficha_Tipo", "([Tipo] = 'Propria' AND [UtilizadorProprioId] IS NOT NULL) OR ([Tipo] = 'Terceira' AND [UtilizadorProprioId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Ficha_CriadaPorId",
                        column: x => x.CriadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ficha_GrauAutonomiaId",
                        column: x => x.GrauAutonomiaId,
                        principalSchema: "dbo",
                        principalTable: "GrauAutonomia",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ficha_UtilizadorProprioId",
                        column: x => x.UtilizadorProprioId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SessaoRevogada",
                schema: "dbo",
                columns: table => new
                {
                    Jti = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilizadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevogadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiraEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessaoRevogada", x => x.Jti);
                    table.CheckConstraint("CK_SessaoRevogada_Validade", "[ExpiraEmUtc] <= DATEADD(HOUR, 8, [RevogadaEmUtc])");
                    table.ForeignKey(
                        name: "FK_SessaoRevogada_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Associacao",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilizadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EPrincipal = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    EProvisorio = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associacao", x => x.Id);
                    table.CheckConstraint("CK_Associacao_Papel", "[Papel] IN ('Cuidador','Familiar')");
                    table.CheckConstraint("CK_Associacao_Principal", "[EPrincipal] = 0 OR [Papel] = 'Cuidador'");
                    table.CheckConstraint("CK_Associacao_Provisorio", "[EProvisorio] = 0 OR [EPrincipal] = 1");
                    table.ForeignKey(
                        name: "FK_Associacao_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Associacao_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Compromisso",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Local = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AntecedenciaAvisoMin = table.Column<int>(type: "int", nullable: false, defaultValue: 1440),
                    AvisoEnviadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CriadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compromisso", x => x.Id);
                    table.CheckConstraint("CK_Compromisso_Antecedencia", "[AntecedenciaAvisoMin] IN (30, 60, 120, 1440, 2880)");
                    table.ForeignKey(
                        name: "FK_Compromisso_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Compromisso_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Convite",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailConvidado = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    PapelOferecido = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ParaPrincipal = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    ConvidadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'Pendente'"),
                    UtilizadorConvidadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RespondidoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convite", x => x.Id);
                    table.CheckConstraint("CK_Convite_Estado", "[Estado] IN ('Pendente','Aceite','Recusado')");
                    table.CheckConstraint("CK_Convite_Papel", "[PapelOferecido] IN ('Cuidador','Familiar')");
                    table.CheckConstraint("CK_Convite_Principal", "[ParaPrincipal] = 0 OR [PapelOferecido] = 'Cuidador'");
                    table.CheckConstraint("CK_Convite_Resposta", "([Estado] = 'Pendente' AND [RespondidoEmUtc] IS NULL AND [UtilizadorConvidadoId] IS NULL) OR ([Estado] <> 'Pendente' AND [RespondidoEmUtc] IS NOT NULL AND [UtilizadorConvidadoId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Convite_ConvidadoPorId",
                        column: x => x.ConvidadoPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Convite_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Convite_UtilizadorConvidadoId",
                        column: x => x.UtilizadorConvidadoId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FichaAlergia",
                schema: "dbo",
                columns: table => new
                {
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlergiaId = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaAlergia", x => new { x.FichaId, x.AlergiaId });
                    table.ForeignKey(
                        name: "FK_FichaAlergia_AlergiaId",
                        column: x => x.AlergiaId,
                        principalSchema: "dbo",
                        principalTable: "Alergia",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FichaAlergia_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichaCondicaoSaude",
                schema: "dbo",
                columns: table => new
                {
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondicaoSaudeId = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaCondicaoSaude", x => new { x.FichaId, x.CondicaoSaudeId });
                    table.ForeignKey(
                        name: "FK_FichaCondicaoSaude_CondicaoSaudeId",
                        column: x => x.CondicaoSaudeId,
                        principalSchema: "dbo",
                        principalTable: "CondicaoSaude",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FichaCondicaoSaude_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nota",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nota_AutorId",
                        column: x => x.AutorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Nota_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notificacao",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DestinatarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ReferenciaTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanteLimiteUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.Id);
                    table.CheckConstraint("CK_Notificacao_Referencia", "([Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares') AND [ReferenciaTipo] = 'Toma') OR ([Tipo] = 'TarefaNaoConfirmada' AND [ReferenciaTipo] = 'ExecucaoTarefa') OR ([Tipo] = 'NovaOcorrencia' AND [ReferenciaTipo] = 'Ocorrencia') OR ([Tipo] = 'ProximoCompromisso' AND [ReferenciaTipo] = 'Compromisso') OR ([Tipo] = 'ConviteRecebido' AND [ReferenciaTipo] = 'Convite')");
                    table.CheckConstraint("CK_Notificacao_Tipo", "[Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares','TarefaNaoConfirmada','NovaOcorrencia','ProximoCompromisso','ConviteRecebido')");
                    table.ForeignKey(
                        name: "FK_Notificacao_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notificacao_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Ocorrencia",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OcorreuEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencia_AutorId",
                        column: x => x.AutorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ocorrencia_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TarefaRecorrente",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PeriodicidadeTipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IntervaloDias = table.Column<byte>(type: "tinyint", nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    ToleranciaMinutos = table.Column<short>(type: "smallint", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1"),
                    CriadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaRecorrente", x => x.Id);
                    table.CheckConstraint("CK_Tarefa_Intervalo", "([PeriodicidadeTipo] = 'CadaNDias' AND [IntervaloDias] BETWEEN 2 AND 30) OR ([PeriodicidadeTipo] <> 'CadaNDias' AND [IntervaloDias] IS NULL)");
                    table.CheckConstraint("CK_Tarefa_Periodicidade", "[PeriodicidadeTipo] IN ('Diaria','DiasSemana','CadaNDias')");
                    table.CheckConstraint("CK_Tarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
                    table.ForeignKey(
                        name: "FK_TarefaRecorrente_CriadaPorId",
                        column: x => x.CriadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TarefaRecorrente_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tratamento",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Medicamento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Dose = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    GrauImportancia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'EmCurso'"),
                    TerminadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CriadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tratamento", x => x.Id);
                    table.CheckConstraint("CK_Tratamento_Datas", "[DataFim] IS NULL OR [DataFim] >= [DataInicio]");
                    table.CheckConstraint("CK_Tratamento_Estado", "[Estado] IN ('EmCurso','Terminado')");
                    table.CheckConstraint("CK_Tratamento_Grau", "[GrauImportancia] IN ('Leve','Moderado','Grave')");
                    table.CheckConstraint("CK_Tratamento_Termino", "([Estado] = 'Terminado' AND [TerminadoEmUtc] IS NOT NULL) OR ([Estado] = 'EmCurso' AND [TerminadoEmUtc] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Tratamento_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tratamento_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnvioEmail",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    NotificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConviteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnderecoDestino = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'Pendente'"),
                    Reenvios = table.Column<byte>(type: "tinyint", nullable: false, defaultValueSql: "0"),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PrimeiraTentativaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UltimaTentativaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ProximaTentativaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvioEmail", x => x.Id);
                    table.CheckConstraint("CK_EnvioEmail_Coerencia", "([Estado] = 'Pendente' AND [ProximaTentativaEmUtc] IS NOT NULL) OR ([Estado] = 'Enviado' AND [ProximaTentativaEmUtc] IS NULL AND [UltimaTentativaEmUtc] IS NOT NULL) OR ([Estado] = 'NaoEntregue' AND [ProximaTentativaEmUtc] IS NULL AND [Reenvios] = 3)");
                    table.CheckConstraint("CK_EnvioEmail_Estado", "[Estado] IN ('Pendente','Enviado','NaoEntregue')");
                    table.CheckConstraint("CK_EnvioEmail_Origem", "[NotificacaoId] IS NOT NULL OR [ConviteId] IS NOT NULL");
                    table.CheckConstraint("CK_EnvioEmail_Reenvios", "[Reenvios] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_EnvioEmail_ConviteId",
                        column: x => x.ConviteId,
                        principalSchema: "dbo",
                        principalTable: "Convite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnvioEmail_NotificacaoId",
                        column: x => x.NotificacaoId,
                        principalSchema: "dbo",
                        principalTable: "Notificacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecucaoTarefa",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrevistaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ToleranciaMinutos = table.Column<short>(type: "smallint", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'PorConfirmar'"),
                    ConfirmadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CuidadoresNotificadosEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucaoTarefa", x => x.Id);
                    table.CheckConstraint("CK_ExecucaoTarefa_Confirmacao", "([Estado] = 'Realizada' AND [ConfirmadaPorId] IS NOT NULL AND [ConfirmadaEmUtc] IS NOT NULL) OR ([Estado] = 'PorConfirmar' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL)");
                    table.CheckConstraint("CK_ExecucaoTarefa_Estado", "[Estado] IN ('PorConfirmar','Realizada')");
                    table.CheckConstraint("CK_ExecucaoTarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
                    table.ForeignKey(
                        name: "FK_ExecucaoTarefa_ConfirmadaPorId",
                        column: x => x.ConfirmadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecucaoTarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HorarioTarefa",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoraLocal = table.Column<TimeOnly>(type: "time(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioTarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorarioTarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TarefaDiaSemana",
                schema: "dbo",
                columns: table => new
                {
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaDiaSemana", x => new { x.TarefaId, x.DiaSemana });
                    table.CheckConstraint("CK_TarefaDiaSemana_Dia", "[DiaSemana] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_TarefaDiaSemana_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documento",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TratamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    NomeOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CaminhoRelativo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Formato = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TamanhoBytes = table.Column<int>(type: "int", nullable: false),
                    CarregadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarregadoEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documento", x => x.Id);
                    table.CheckConstraint("CK_Documento_Categoria", "[Categoria] IN ('ReceitaMedica','RelatorioClinico','DocumentoIdentificacao')");
                    table.CheckConstraint("CK_Documento_Formato", "[Formato] IN ('PDF','JPG','PNG')");
                    table.CheckConstraint("CK_Documento_Receita", "[TratamentoId] IS NULL OR [Categoria] = 'ReceitaMedica'");
                    table.CheckConstraint("CK_Documento_Tamanho", "[TamanhoBytes] BETWEEN 1 AND 10485760");
                    table.ForeignKey(
                        name: "FK_Documento_CarregadoPorId",
                        column: x => x.CarregadoPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documento_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documento_TratamentoId",
                        column: x => x.TratamentoId,
                        principalSchema: "dbo",
                        principalTable: "Tratamento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HorarioToma",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TratamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoraLocal = table.Column<TimeOnly>(type: "time(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioToma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorarioToma_TratamentoId",
                        column: x => x.TratamentoId,
                        principalSchema: "dbo",
                        principalTable: "Tratamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Toma",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TratamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrevistaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Medicamento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Dose = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    GrauImportancia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "'Pendente'"),
                    ConfirmadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    NaoConfirmadaDetetadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CuidadoresNotificadosEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    FamiliaresNotificadosEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toma", x => x.Id);
                    table.CheckConstraint("CK_Toma_Confirmacao", "([Estado] IN ('Confirmada','ConfirmadaComAtraso') AND [ConfirmadaPorId] IS NOT NULL AND [ConfirmadaEmUtc] IS NOT NULL AND [NaoConfirmadaDetetadaEmUtc] IS NULL) OR ([Estado] = 'Pendente' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL AND [NaoConfirmadaDetetadaEmUtc] IS NULL) OR ([Estado] = 'NaoConfirmada' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL AND [NaoConfirmadaDetetadaEmUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Toma_Estado", "[Estado] IN ('Pendente','Confirmada','ConfirmadaComAtraso','NaoConfirmada')");
                    table.CheckConstraint("CK_Toma_Familiares", "[FamiliaresNotificadosEmUtc] IS NULL OR [GrauImportancia] = 'Grave'");
                    table.CheckConstraint("CK_Toma_Grau", "[GrauImportancia] IN ('Leve','Moderado','Grave')");
                    table.CheckConstraint("CK_Toma_Limite24h", "([ConfirmadaEmUtc] IS NULL OR [ConfirmadaEmUtc] < DATEADD(HOUR, 24, [PrevistaEmUtc])) AND ([NaoConfirmadaDetetadaEmUtc] IS NULL OR [NaoConfirmadaDetetadaEmUtc] >= DATEADD(HOUR, 24, [PrevistaEmUtc]))");
                    table.CheckConstraint("CK_Toma_Tolerancia", "([Estado] <> 'Confirmada' OR [ConfirmadaEmUtc] <= DATEADD(MINUTE, CASE [GrauImportancia] WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, [PrevistaEmUtc])) AND ([Estado] <> 'ConfirmadaComAtraso' OR [ConfirmadaEmUtc] > DATEADD(MINUTE, CASE [GrauImportancia] WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, [PrevistaEmUtc]))");
                    table.ForeignKey(
                        name: "FK_Toma_ConfirmadaPorId",
                        column: x => x.ConfirmadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Toma_TratamentoId",
                        column: x => x.TratamentoId,
                        principalSchema: "dbo",
                        principalTable: "Tratamento",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Alergia",
                columns: new[] { "Id", "Designacao" },
                values: new object[,]
                {
                    { (short)1, "Medicamentos" },
                    { (short)2, "Alimentos" },
                    { (short)3, "Látex" },
                    { (short)4, "Picadas de insetos" },
                    { (short)5, "Pólen ou ácaros" },
                    { (short)6, "Outra alergia" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "CondicaoSaude",
                columns: new[] { "Id", "Designacao" },
                values: new object[,]
                {
                    { (short)1, "Diabetes" },
                    { (short)2, "Hipertensão arterial" },
                    { (short)3, "Doença cardíaca" },
                    { (short)4, "Doença respiratória crónica" },
                    { (short)5, "Demência ou doença de Alzheimer" },
                    { (short)6, "Doença de Parkinson" },
                    { (short)7, "Epilepsia" },
                    { (short)8, "Insuficiência renal" },
                    { (short)9, "Outra condição relevante" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "GrauAutonomia",
                columns: new[] { "Id", "Designacao", "Ordem" },
                values: new object[,]
                {
                    { (short)1, "Autónomo", (byte)1 },
                    { (short)2, "Autonomia parcial", (byte)2 },
                    { (short)3, "Dependente", (byte)3 },
                    { (short)4, "Acamado", (byte)4 }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Alergia_Designacao",
                schema: "dbo",
                table: "Alergia",
                column: "Designacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associacao_Utilizador",
                schema: "dbo",
                table: "Associacao",
                columns: new[] { "UtilizadorId", "FichaId" })
                .Annotation("SqlServer:Include", new[] { "Papel" });

            migrationBuilder.CreateIndex(
                name: "UQ_Associacao_FichaUtilizador",
                schema: "dbo",
                table: "Associacao",
                columns: new[] { "FichaId", "UtilizadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Associacao_Principal",
                schema: "dbo",
                table: "Associacao",
                column: "FichaId",
                unique: true,
                filter: "[EPrincipal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Compromisso_AvisoPendente",
                schema: "dbo",
                table: "Compromisso",
                column: "DataHoraUtc",
                filter: "[AvisoEnviadoEmUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compromisso_FichaData",
                schema: "dbo",
                table: "Compromisso",
                columns: new[] { "FichaId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_CondicaoSaude_Designacao",
                schema: "dbo",
                table: "CondicaoSaude",
                column: "Designacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Convite_EmailEstado",
                schema: "dbo",
                table: "Convite",
                columns: new[] { "EmailConvidado", "Estado" });

            migrationBuilder.CreateIndex(
                name: "UQ_Convite_Pendente",
                schema: "dbo",
                table: "Convite",
                columns: new[] { "FichaId", "EmailConvidado" },
                unique: true,
                filter: "[Estado] = 'Pendente'");

            migrationBuilder.CreateIndex(
                name: "IX_Documento_Ficha",
                schema: "dbo",
                table: "Documento",
                columns: new[] { "FichaId", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "UQ_Documento_Caminho",
                schema: "dbo",
                table: "Documento",
                column: "CaminhoRelativo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvioEmail_PendenteProxima",
                schema: "dbo",
                table: "EnvioEmail",
                column: "ProximaTentativaEmUtc",
                filter: "[Estado] = 'Pendente'");

            migrationBuilder.CreateIndex(
                name: "UQ_EnvioEmail_Convite",
                schema: "dbo",
                table: "EnvioEmail",
                column: "ConviteId",
                unique: true,
                filter: "[ConviteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_EnvioEmail_Notificacao",
                schema: "dbo",
                table: "EnvioEmail",
                column: "NotificacaoId",
                unique: true,
                filter: "[NotificacaoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoTarefa_PorConfirmar",
                schema: "dbo",
                table: "ExecucaoTarefa",
                column: "PrevistaEmUtc",
                filter: "[Estado] = 'PorConfirmar' AND [CuidadoresNotificadosEmUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_ExecucaoTarefa_TarefaInstante",
                schema: "dbo",
                table: "ExecucaoTarefa",
                columns: new[] { "TarefaId", "PrevistaEmUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ficha_Estado",
                schema: "dbo",
                table: "Ficha",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "UQ_Ficha_UtilizadorProprio",
                schema: "dbo",
                table: "Ficha",
                column: "UtilizadorProprioId",
                unique: true,
                filter: "[UtilizadorProprioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_GrauAutonomia_Designacao",
                schema: "dbo",
                table: "GrauAutonomia",
                column: "Designacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_HorarioTarefa_TarefaHora",
                schema: "dbo",
                table: "HorarioTarefa",
                columns: new[] { "TarefaId", "HoraLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_HorarioToma_TratamentoHora",
                schema: "dbo",
                table: "HorarioToma",
                columns: new[] { "TratamentoId", "HoraLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nota_FichaRegisto",
                schema: "dbo",
                table: "Nota",
                columns: new[] { "FichaId", "RegistadaEmUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_DestinatarioData",
                schema: "dbo",
                table: "Notificacao",
                columns: new[] { "DestinatarioId", "CriadaEmUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_Notificacao_Origem",
                schema: "dbo",
                table: "Notificacao",
                columns: new[] { "DestinatarioId", "Tipo", "ReferenciaId", "InstanteLimiteUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencia_FichaRegisto",
                schema: "dbo",
                table: "Ocorrencia",
                columns: new[] { "FichaId", "RegistadaEmUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SessaoRevogada_ExpiraEmUtc",
                schema: "dbo",
                table: "SessaoRevogada",
                column: "ExpiraEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_FichaAtiva",
                schema: "dbo",
                table: "TarefaRecorrente",
                columns: new[] { "FichaId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_Toma_PendentePrevista",
                schema: "dbo",
                table: "Toma",
                column: "PrevistaEmUtc",
                filter: "[Estado] = 'Pendente'");

            migrationBuilder.CreateIndex(
                name: "UQ_Toma_TratamentoInstante",
                schema: "dbo",
                table: "Toma",
                columns: new[] { "TratamentoId", "PrevistaEmUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tratamento_FichaEstado",
                schema: "dbo",
                table: "Tratamento",
                columns: new[] { "FichaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "UQ_Utilizador_Email",
                schema: "dbo",
                table: "Utilizador",
                column: "Email",
                unique: true);

            migrationBuilder.CriarGatilhosDoseCerta();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoverGatilhosDoseCerta();

            migrationBuilder.DropTable(
                name: "Associacao",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Compromisso",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Documento",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EnvioEmail",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExecucaoTarefa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FichaAlergia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FichaCondicaoSaude",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HorarioTarefa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HorarioToma",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Nota",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Ocorrencia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SessaoRevogada",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TarefaDiaSemana",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Toma",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Convite",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notificacao",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Alergia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CondicaoSaude",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TarefaRecorrente",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Tratamento",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Ficha",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Utilizador",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GrauAutonomia",
                schema: "dbo");
        }
    }
}
