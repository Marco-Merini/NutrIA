using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriFlow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sources = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChunksRetrieved = table.Column<int>(type: "int", nullable: false),
                    LatenciaMs = table.Column<int>(type: "int", nullable: false),
                    ConsentimentoLGPD = table.Column<bool>(type: "bit", nullable: false),
                    DadosPseudonimizados = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Embeddings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    SourceTable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    ChunkType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChunkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    senha_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ativo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<int>(type: "int", nullable: true),
                    nome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_nascimento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sexo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    telefone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    peso_atual = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    altura = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    objetivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nivel_atividade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    preferencias_alimentares = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    condicoes_saude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medicamentos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    observacoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_cadastro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Pacientes_Usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Planos_Dieta",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    paciente_id = table.Column<int>(type: "int", nullable: true),
                    titulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    objetivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    calorias_diarias = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    proteinas_g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    carboidratos_g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    gorduras_g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    orientacoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    observacoes_nutricionista = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gerado_por_ia = table.Column<bool>(type: "bit", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planos_Dieta", x => x.id);
                    table.ForeignKey(
                        name: "FK_Planos_Dieta_Pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "Pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Progresso",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    paciente_id = table.Column<int>(type: "int", nullable: true),
                    data_registro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    peso = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    cintura_cm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    quadril_cm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    percentual_gordura = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    aderencia_plano = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    humor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    energia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progresso", x => x.id);
                    table.ForeignKey(
                        name: "FK_Progresso_Pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "Pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    paciente_id = table.Column<int>(type: "int", nullable: true),
                    data_sessao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    peso_sessao = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    anotacoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    proxima_consulta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Sessoes_Pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "Pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Refeicoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    plano_id = table.Column<int>(type: "int", nullable: true),
                    nome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    horario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    alimentos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    calorias = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refeicoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Refeicoes_Planos_Dieta_plano_id",
                        column: x => x.plano_id,
                        principalTable: "Planos_Dieta",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PatientId",
                table: "AuditLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_PatientId",
                table: "Embeddings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_PatientId_SourceTable",
                table: "Embeddings",
                columns: new[] { "PatientId", "SourceTable" });

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_usuario_id",
                table: "Pacientes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_Planos_Dieta_paciente_id",
                table: "Planos_Dieta",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_Progresso_paciente_id",
                table: "Progresso",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_Refeicoes_plano_id",
                table: "Refeicoes",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_paciente_id",
                table: "Sessoes",
                column: "paciente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Embeddings");

            migrationBuilder.DropTable(
                name: "Progresso");

            migrationBuilder.DropTable(
                name: "Refeicoes");

            migrationBuilder.DropTable(
                name: "Sessoes");

            migrationBuilder.DropTable(
                name: "Planos_Dieta");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
