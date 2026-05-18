using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoscolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FixProductCategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalItems_ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropColumn(
                name: "ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "ProductCategoryId", "Description", "Name" },
                values: new object[] { 1L, "Catégorie exemple pour produits non-livres", "Fournitures" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalItems_ProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoryId");

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

            migrationBuilder.DropIndex(
                name: "IX_PhysicalItems_ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "ProductCategoriesProductCategoryId",
                table: "PhysicalItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalItems_ProductCategoriesProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoriesProductCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoriesProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoriesProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId");
        }
    }
}
