using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedLocationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Location",
                keyColumn: "LocationId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Location",
                keyColumn: "LocationId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Location",
                keyColumn: "LocationId",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Location",
                columns: new[] { "LocationId", "City", "PostalCode", "Region" },
                values: new object[,]
                {
                    { 1, "Lausanne", "1000", "Vaud" },
                    { 2, "Montreux", "1820", "Vaud" },
                    { 3, "Martigny", "1920", "Valais" }
                });
        }
    }
}
