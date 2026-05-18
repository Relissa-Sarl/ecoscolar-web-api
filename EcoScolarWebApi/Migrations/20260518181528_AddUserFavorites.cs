using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoscolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdvertId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => new { x.UserId, x.AdvertId });
                    table.ForeignKey(
                        name: "FK_UserFavorites_Adverts_AdvertId",
                        column: x => x.AdvertId,
                        principalTable: "Adverts",
                        principalColumn: "AdvertId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_AdvertId",
                table: "UserFavorites",
                column: "AdvertId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems");

            migrationBuilder.DropTable(
                name: "UserFavorites");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalItems_ProductCategories_ProductCategoryId",
                table: "PhysicalItems",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "ProductCategoryId");
        }
    }
}
