using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DoseCerta.Api.Dados.Migrations
{
    /// <inheritdoc />
    public partial class I003FichaPropria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alergias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EhAusencia = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alergias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CondicoesSaude",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EhAusencia = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CondicoesSaude", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrausAutonomia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrausAutonomia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilizadores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fichas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ContactoEmergenciaNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactoEmergenciaTelefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrauAutonomiaId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorProprietarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataArquivo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fichas", x => x.Id);
                    table.CheckConstraint("CK_Fichas_Estado", "[Estado] IN ('Ativa', 'Arquivada')");
                    table.ForeignKey(
                        name: "FK_Fichas_GrausAutonomia_GrauAutonomiaId",
                        column: x => x.GrauAutonomiaId,
                        principalTable: "GrausAutonomia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_Utilizadores_UtilizadorProprietarioId",
                        column: x => x.UtilizadorProprietarioId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssociacoesFicha",
                columns: table => new
                {
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilizadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EhCuidadorPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    DataAssociacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociacoesFicha", x => new { x.FichaId, x.UtilizadorId });
                    table.CheckConstraint("CK_AssociacoesFicha_Papel", "[Papel] IN ('Cuidador', 'Familiar')");
                    table.CheckConstraint("CK_AssociacoesFicha_PrincipalEhCuidador", "[EhCuidadorPrincipal] = 0 OR [Papel] = 'Cuidador'");
                    table.ForeignKey(
                        name: "FK_AssociacoesFicha_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociacoesFicha_Utilizadores_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosFicha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NomeOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoConteudo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChaveArmazenamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataCarregamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosFicha", x => x.Id);
                    table.CheckConstraint("CK_DocumentosFicha_Categoria", "[Categoria] IN ('ReceitaMedica', 'RelatorioClinico', 'DocumentoIdentificacao')");
                    table.ForeignKey(
                        name: "FK_DocumentosFicha_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichasAlergias",
                columns: table => new
                {
                    AlergiaId = table.Column<int>(type: "int", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasAlergias", x => new { x.AlergiaId, x.FichaId });
                    table.ForeignKey(
                        name: "FK_FichasAlergias_Alergias_AlergiaId",
                        column: x => x.AlergiaId,
                        principalTable: "Alergias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichasAlergias_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichasCondicoesSaude",
                columns: table => new
                {
                    CondicaoSaudeId = table.Column<int>(type: "int", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasCondicoesSaude", x => new { x.CondicaoSaudeId, x.FichaId });
                    table.ForeignKey(
                        name: "FK_FichasCondicoesSaude_CondicoesSaude_CondicaoSaudeId",
                        column: x => x.CondicaoSaudeId,
                        principalTable: "CondicoesSaude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichasCondicoesSaude_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Alergias",
                columns: new[] { "Id", "EhAusencia", "Nome" },
                values: new object[,]
                {
                    { 1, true, "Sem alergias conhecidas" },
                    { 2, false, "Penicilina e derivados" },
                    { 3, false, "Sulfonamidas" },
                    { 4, false, "Anti-inflamatórios não esteroides (AINE)" },
                    { 5, false, "Ácido acetilsalicílico" },
                    { 6, false, "Iodo e meios de contraste" },
                    { 7, false, "Látex" },
                    { 8, false, "Marisco" },
                    { 9, false, "Frutos de casca rija" },
                    { 10, false, "Ovo" },
                    { 11, false, "Proteína do leite" },
                    { 12, false, "Pólenes" },
                    { 13, false, "Ácaros" },
                    { 14, false, "Picada de abelha ou vespa" }
                });

            migrationBuilder.InsertData(
                table: "CondicoesSaude",
                columns: new[] { "Id", "EhAusencia", "Nome" },
                values: new object[,]
                {
                    { 1, true, "Sem condições de saúde relevantes" },
                    { 2, false, "Hipertensão arterial" },
                    { 3, false, "Diabetes mellitus tipo 1" },
                    { 4, false, "Diabetes mellitus tipo 2" },
                    { 5, false, "Dislipidemia" },
                    { 6, false, "Insuficiência cardíaca" },
                    { 7, false, "Fibrilhação auricular" },
                    { 8, false, "Asma" },
                    { 9, false, "Doença pulmonar obstrutiva crónica (DPOC)" },
                    { 10, false, "Doença renal crónica" },
                    { 11, false, "Demência" },
                    { 12, false, "Doença de Parkinson" },
                    { 13, false, "Epilepsia" },
                    { 14, false, "Depressão" },
                    { 15, false, "Osteoporose" }
                });

            migrationBuilder.InsertData(
                table: "GrausAutonomia",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Autónomo" },
                    { 2, "Parcialmente dependente" },
                    { 3, "Dependente" },
                    { 4, "Totalmente dependente (acamado)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alergias_Nome",
                table: "Alergias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociacoesFicha_UtilizadorId",
                table: "AssociacoesFicha",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "UX_AssociacoesFicha_CuidadorPrincipal",
                table: "AssociacoesFicha",
                column: "FichaId",
                unique: true,
                filter: "[EhCuidadorPrincipal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CondicoesSaude_Nome",
                table: "CondicoesSaude",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosFicha_FichaId",
                table: "DocumentosFicha",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_GrauAutonomiaId",
                table: "Fichas",
                column: "GrauAutonomiaId");

            migrationBuilder.CreateIndex(
                name: "UX_Fichas_UtilizadorProprietarioId",
                table: "Fichas",
                column: "UtilizadorProprietarioId",
                unique: true,
                filter: "[UtilizadorProprietarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FichasAlergias_FichaId",
                table: "FichasAlergias",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasCondicoesSaude_FichaId",
                table: "FichasCondicoesSaude",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_GrausAutonomia_Nome",
                table: "GrausAutonomia",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilizadores_Email",
                table: "Utilizadores",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssociacoesFicha");

            migrationBuilder.DropTable(
                name: "DocumentosFicha");

            migrationBuilder.DropTable(
                name: "FichasAlergias");

            migrationBuilder.DropTable(
                name: "FichasCondicoesSaude");

            migrationBuilder.DropTable(
                name: "Alergias");

            migrationBuilder.DropTable(
                name: "CondicoesSaude");

            migrationBuilder.DropTable(
                name: "Fichas");

            migrationBuilder.DropTable(
                name: "GrausAutonomia");

            migrationBuilder.DropTable(
                name: "Utilizadores");
        }
    }
}
