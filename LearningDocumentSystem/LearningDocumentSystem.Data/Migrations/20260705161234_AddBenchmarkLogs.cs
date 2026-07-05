using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexBenchmarkLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentID = table.Column<int>(type: "int", nullable: true),
                    ChunkingStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalChunksGenerated = table.Column<int>(type: "int", nullable: false),
                    ProcessingTimeMs = table.Column<double>(type: "float", nullable: false),
                    AverageChunkSize = table.Column<double>(type: "float", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexBenchmarkLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_IndexBenchmarkLogs_Documents_DocumentID",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QueryBenchmarkLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LLMModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetrievalTimeMs = table.Column<double>(type: "float", nullable: false),
                    GenerationTimeMs = table.Column<double>(type: "float", nullable: false),
                    Top1CosineSimilarity = table.Column<double>(type: "float", nullable: false),
                    SelectedSourcesCount = table.Column<int>(type: "int", nullable: false),
                    UserRating = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryBenchmarkLogs", x => x.LogID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexBenchmarkLogs_ChunkingStrategy",
                table: "IndexBenchmarkLogs",
                column: "ChunkingStrategy");

            migrationBuilder.CreateIndex(
                name: "IX_IndexBenchmarkLogs_DocumentID",
                table: "IndexBenchmarkLogs",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_IndexBenchmarkLogs_EmbeddingModel",
                table: "IndexBenchmarkLogs",
                column: "EmbeddingModel");

            migrationBuilder.CreateIndex(
                name: "IX_IndexBenchmarkLogs_ExecutedAt",
                table: "IndexBenchmarkLogs",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueryBenchmarkLogs_CreatedAt",
                table: "QueryBenchmarkLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueryBenchmarkLogs_EmbeddingModel",
                table: "QueryBenchmarkLogs",
                column: "EmbeddingModel");

            migrationBuilder.CreateIndex(
                name: "IX_QueryBenchmarkLogs_LLMModel",
                table: "QueryBenchmarkLogs",
                column: "LLMModel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexBenchmarkLogs");

            migrationBuilder.DropTable(
                name: "QueryBenchmarkLogs");
        }
    }
}
