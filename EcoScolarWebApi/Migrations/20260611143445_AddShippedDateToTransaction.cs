using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddShippedDateToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedDate",
                table: "Transactions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippedDate",
                table: "Transactions");
        }
    }
}
