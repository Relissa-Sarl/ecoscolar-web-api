using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoscolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class DeleteUnnecesaryNavProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
