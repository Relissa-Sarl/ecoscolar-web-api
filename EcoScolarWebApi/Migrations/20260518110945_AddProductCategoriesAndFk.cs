using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoscolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoriesAndFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProductCategoriesProductCategoryId",
                table: "PhysicalItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProductCategoryId",
                table: "PhysicalItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    ProductCategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.ProductCategoryId);
                });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                column: "Description",
                value: "description");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalItems_ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropColumn(
                name: "ProductCategoriesProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                column: "Description",
                value: "desciption");
        }
    }
}
