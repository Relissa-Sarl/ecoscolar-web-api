using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoscolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSeeder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Manuels par niveau et programme cantonal.", "Manuels scolaires" });

            migrationBuilder.InsertData(
                table: "BookCategories",
                columns: new[] { "BookCategoryId", "Description", "Name" },
                values: new object[,]
                {
                    { 2L, "Dictionnaires, encyclopédies et atlas scolaires.", "Ouvrages de référence" },
                    { 3L, "Français, allemand, italien, anglais et langues locales.", "Langues" },
                    { 4L, "Supports de mathématiques pour cycles 1 à secondaire II.", "Mathématiques" },
                    { 5L, "Biologie, chimie, physique et sciences de la nature.", "Sciences naturelles" },
                    { 6L, "Histoire suisse, géographie et éducation citoyenne.", "Histoire et géographie" },
                    { 7L, "Introduction à l'économie, droit et gestion.", "Économie et droit" },
                    { 8L, "Supports pour arts visuels, musique et activités créatives.", "Arts et musique" },
                    { 9L, "Initiation au numérique, algorithmique et outils informatiques.", "Informatique" },
                    { 10L, "Manuels liés aux filières CFC et maturité professionnelle.", "Formation professionnelle" }
                });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 1L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Papiers, enveloppes, étiquettes et consommables.", "Papeterie" });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "ProductCategoryId", "Description", "Name" },
                values: new object[,]
                {
                    { 2L, "Stylos, crayons, feutres et surligneurs.", "Matériel d'écriture" },
                    { 3L, "Cahiers, classeurs, intercalaires et chemises.", "Cahiers et classeurs" },
                    { 4L, "Peinture, pinceaux, papiers dessin et outils créatifs.", "Matériel artistique" },
                    { 5L, "Kits pédagogiques, microscopes et accessoires.", "Matériel scientifique" },
                    { 6L, "Ballons, cordes, protections et matériel EPS.", "Équipement sportif" },
                    { 7L, "Claviers, souris, périphériques et accessoires.", "Matériel informatique" },
                    { 8L, "Sacs d'école, cartables et trousses.", "Sacs et cartables" },
                    { 9L, "Calculatrices scientifiques et financières.", "Calculatrices" },
                    { 10L, "Blouses, lunettes de protection et consommables.", "Accessoires de laboratoire" }
                });

            migrationBuilder.UpdateData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 1L,
                columns: new[] { "Name", "SchoolGrade" },
                values: new object[] { "Cycle 1 (1H-4H)", "C1" });

            migrationBuilder.InsertData(
                table: "SchoolGrade",
                columns: new[] { "SchoolGradeId", "Name", "SchoolGrade" },
                values: new object[,]
                {
                    { 2L, "Cycle 2 (5H-8H)", "C2" },
                    { 3L, "Cycle 3 (9H-11H)", "C3" },
                    { 4L, "Secondaire II - Gymnase", "S2-GYM" },
                    { 5L, "Secondaire II - Maturité professionnelle", "S2-MP" },
                    { 6L, "Secondaire II - CFC", "S2-CFC" },
                    { 7L, "Secondaire II - ECG", "S2-ECG" }
                });

            migrationBuilder.UpdateData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 1L,
                columns: new[] { "Name", "Subject" },
                values: new object[] { "Français", "FR" });

            migrationBuilder.InsertData(
                table: "Subject",
                columns: new[] { "SubjectId", "Name", "Subject" },
                values: new object[,]
                {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 11L);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 12L);

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "description", "first cat" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 1L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Catégorie exemple pour produits non-livres", "Fournitures" });

            migrationBuilder.UpdateData(
                table: "SchoolGrade",
                keyColumn: "SchoolGradeId",
                keyValue: 1L,
                columns: new[] { "Name", "SchoolGrade" },
                values: new object[] { "École Supérieur", "ES" });

            migrationBuilder.UpdateData(
                table: "Subject",
                keyColumn: "SubjectId",
                keyValue: 1L,
                columns: new[] { "Name", "Subject" },
                values: new object[] { "Math", "MATH" });
        }
    }
}
