using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectLeaderToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectLeaderID",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectLeaderID",
                table: "Subjects",
                column: "SubjectLeaderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Users_SubjectLeaderID",
                table: "Subjects",
                column: "SubjectLeaderID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Users_SubjectLeaderID",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectLeaderID",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SubjectLeaderID",
                table: "Subjects");
        }
    }
}
