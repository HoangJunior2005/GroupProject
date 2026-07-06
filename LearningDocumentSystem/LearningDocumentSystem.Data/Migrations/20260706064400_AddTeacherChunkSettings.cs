using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherChunkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherChunkSettings",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Recursive"),
                    ChunkSize = table.Column<int>(type: "int", nullable: false, defaultValue: 800),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    MinChunkLength = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherChunkSettings", x => x.TeacherId);
                    table.ForeignKey(
                        name: "FK_TeacherChunkSettings_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherChunkSettings");
        }
    }
}
