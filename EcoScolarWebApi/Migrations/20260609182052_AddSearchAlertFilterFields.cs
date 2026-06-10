using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchAlertFilterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SearchAlerts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                table: "SearchAlerts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProductCategoryId",
                table: "SearchAlerts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SchoolGradeId",
                table: "SearchAlerts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchAlerts_ProductCategoryId",
                table: "SearchAlerts",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchAlerts_SchoolGradeId",
                table: "SearchAlerts",
                column: "SchoolGradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchAlerts_ProductCategories_ProductCategoryId",
                table: "SearchAlerts",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchAlerts_SchoolGrades_SchoolGradeId",
                table: "SearchAlerts",
                column: "SchoolGradeId",
                principalTable: "SchoolGrades",
                principalColumn: "SchoolGradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchAlerts_ProductCategories_ProductCategoryId",
                table: "SearchAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchAlerts_SchoolGrades_SchoolGradeId",
                table: "SearchAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SearchAlerts_ProductCategoryId",
                table: "SearchAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SearchAlerts_SchoolGradeId",
                table: "SearchAlerts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SearchAlerts");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                table: "SearchAlerts");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "SearchAlerts");

            migrationBuilder.DropColumn(
                name: "SchoolGradeId",
                table: "SearchAlerts");
        }
    }
}
