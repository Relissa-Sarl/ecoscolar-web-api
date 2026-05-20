using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Locations_LocationId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_SchoolGrade_SchoolGradeId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Subject_SubjectId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "SchoolGrades");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Locations",
                table: "Locations");

            migrationBuilder.RenameTable(
                name: "Locations",
                newName: "Location");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Location",
                table: "Location",
                column: "LocationId");

            migrationBuilder.CreateTable(
                name: "SchoolGrades",
                columns: table => new
                {
                    SchoolGradeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolGrades", x => x.SchoolGradeId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.InsertData(
                table: "SchoolGrades",
                columns: new[] { "SchoolGradeId", "Grade", "Name" },
                values: new object[,]
                {
                    { 1L, "C1", "Cycle 1 (1H-4H)" },
                    { 2L, "C2", "Cycle 2 (5H-8H)" },
                    { 3L, "C3", "Cycle 3 (9H-11H)" },
                    { 4L, "S2-GYM", "Secondaire II - Gymnase" },
                    { 5L, "S2-MP", "Secondaire II - Maturité professionnelle" },
                    { 6L, "S2-CFC", "Secondaire II - CFC" },
                    { 7L, "S2-ECG", "Secondaire II - ECG" }
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "SubjectId", "Code", "Name" },
                values: new object[,]
                {
                    { 1L, "FR", "Français" },
                    { 2L, "DE", "Allemand" },
                    { 3L, "EN", "Anglais" },
                    { 4L, "MATH", "Mathématiques" },
                    { 5L, "SCI", "Sciences naturelles" },
                    { 6L, "HIST", "Histoire" },
                    { 7L, "GEO", "Géographie" },
                    { 8L, "EPS", "Éducation physique" },
                    { 9L, "ARTS", "Arts visuels" },
                    { 10L, "MUS", "Musique" },
                    { 11L, "ECO", "Économie et droit" },
                    { 12L, "INFO", "Informatique" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Location_LocationId",
                table: "AspNetUsers",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_SchoolGrades_SchoolGradeId",
                table: "Services",
                column: "SchoolGradeId",
                principalTable: "SchoolGrades",
                principalColumn: "SchoolGradeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Subjects_SubjectId",
                table: "Services",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Location_LocationId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_SchoolGrades_SchoolGradeId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Subjects_SubjectId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "SchoolGrades");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Location",
                table: "Location");

            migrationBuilder.RenameTable(
                name: "Location",
                newName: "Locations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Locations",
                table: "Locations",
                column: "LocationId");

            migrationBuilder.CreateTable(
                name: "SchoolGrades",
                columns: table => new
                {
                    SchoolGradeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SchoolGrades = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolGrade", x => x.SchoolGradeId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subjects = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.SubjectId);
                });

            migrationBuilder.InsertData(
                table: "SchoolGrades",
                columns: new[] { "SchoolGradeId", "Name", "SchoolGrades" },
                values: new object[,]
                {
                    { 1L, "Cycle 1 (1H-4H)", "C1" },
                    { 2L, "Cycle 2 (5H-8H)", "C2" },
                    { 3L, "Cycle 3 (9H-11H)", "C3" },
                    { 4L, "Secondaire II - Gymnase", "S2-GYM" },
                    { 5L, "Secondaire II - Maturité professionnelle", "S2-MP" },
                    { 6L, "Secondaire II - CFC", "S2-CFC" },
                    { 7L, "Secondaire II - ECG", "S2-ECG" }
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "SubjectId", "Name", "Subjects" },
                values: new object[,]
                {
                    { 1L, "Français", "FR" },
                    { 2L, "Allemand", "DE" },
                    { 3L, "Anglais", "EN" },
                    { 4L, "Mathématiques", "MATH" },
                    { 5L, "Sciences naturelles", "SCI" },
                    { 6L, "Histoire", "HIST" },
                    { 7L, "Géographie", "GEO" },
                    { 8L, "Éducation physique", "EPS" },
                    { 9L, "Arts visuels", "ARTS" },
                    { 10L, "Musique", "MUS" },
                    { 11L, "Économie et droit", "ECO" },
                    { 12L, "Informatique", "INFO" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Locations_LocationId",
                table: "AspNetUsers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_SchoolGrade_SchoolGradeId",
                table: "Services",
                column: "SchoolGradeId",
                principalTable: "SchoolGrades",
                principalColumn: "SchoolGradeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Subject_SubjectId",
                table: "Services",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
