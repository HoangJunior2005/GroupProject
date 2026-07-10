using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMBenchmarkToChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ExecutionTimeMs",
                table: "ChatMessages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Feedback",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "ChatMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ExecutionTimeMs",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "ChatMessages");
        }
    }
}
