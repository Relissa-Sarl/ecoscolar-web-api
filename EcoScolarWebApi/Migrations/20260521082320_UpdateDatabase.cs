using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "Subjects",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "SchoolGrade",
                table: "SchoolGrades",
                newName: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Subjects",
                newName: "Subject");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "SchoolGrades",
                newName: "SchoolGrade");
        }
    }
}
