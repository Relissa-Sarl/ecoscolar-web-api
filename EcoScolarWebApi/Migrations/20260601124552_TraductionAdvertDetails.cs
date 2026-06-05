using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoScolarWebApi.Migrations
{
    /// <inheritdoc />
    public partial class TraductionAdvertDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameDe",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameFr",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameDe",
                table: "SchoolGrades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameFr",
                table: "SchoolGrades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "SchoolGrades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameDe",
                table: "ProductCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameFr",
                table: "ProductCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "ProductCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameDe",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameFr",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameDe",
                table: "BookCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameFr",
                table: "BookCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "BookCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Textbooks", "Schulbücher", "Manuels scolaires", "Libri di scuola" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 2L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Ouvrages de référence pour les étudiants.", "Reference works", "Referenzwerke", "Ouvrages de référence", "Opere di riferimento" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 3L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour l'apprentissage des langues.", "Languages", "Sprachen", "Langues", "Lingue" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 4L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Manuels et ressources pour l'enseignement des mathématiques.", "Mathematics", "Mathematik", "Mathématiques", "Matematica" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 5L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour les sciences naturelles.", "Natural Sciences", "Naturwissenschaften", "Sciences naturelles", "Scienze della natura" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 6L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour l'histoire et la géographie.", "History and Geography", "Geschichte und Geographie", "Histoire et géographie", "Storia e geografia" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 7L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour l'économie et le droit.", "Economics and Law", "Wirtschaft und Recht", "Économie et droit", "Economia e diritto" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 8L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour les arts et la musique.", "Arts and Music", "Kunst und Musik", "Arts et musique", "Arte e musica" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 9L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour l'informatique.", "Computer Science", "Informatik", "Informatique", "Informatica" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 10L,
                columns: new[] { "Description", "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cours et ressources pour la formation professionnelle.", "Vocational training", "Berufliche Bildung", "Formation professionnelle", "Formazione professionale" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Label",
                keyValue: "DE",
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "German", "Deutsch", "Allemand", "Tedesco" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Label",
                keyValue: "FR",
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "French", "Französisch", "Français", "Francese" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Label",
                keyValue: "IT",
                columns: new[] { "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Italien", "Italien", "Italiano" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 1L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Stationery", "Papierware", "Papeterie", "Cartoleria" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 2L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Writing supplies", "Schreibmaterial", "Matériel d'écriture", "Materiale da scrittura" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 3L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Notebooks and binders", "Hefte und Mappe", "Cahiers et classeurs", "Quaderni e cartelle" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 4L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Art supplies", "Künstlerisches Material", "Matériel artistique", "Materiale artistico" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 5L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Scientific equipment", "Wissenschaftliches Material", "Matériel scientifique", "Materiale scientifico" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 6L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Sports equipment", "Sportausrüstung", "Équipement sportif", "Equipaggiamento sportivo" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 7L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Computer equipment", "Informatikmaterial", "Matériel informatique", "Materiale informatico" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 8L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "School bags and cases", "Schulrucksäcke und Bücherfächer", "Sacs et cartables", "Ziole e borse scolastiche" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 9L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Calculators", "Taschenrechner", "Calculatrices", "Calcolatrici" });

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 10L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Laboratory supplies", "Laboratoriumsausrüstung", "Accessoires de laboratoire", "Accessori del laboratorio" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 1L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cycle 1 (Grades 1–4)", "Stufe 1 (1H-4H)", "Cycle 1 (1H-4H)", "Livello 1 (1H-4H)" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 2L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cycle 2 (Grades 5–8)", "Stufe 2 (5H-8H)", "Cycle 2 (5H-8H)", "Livello 2 (5H-8H)" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 3L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Cycle 3 (Grades 9–11)", "Stufe 3 (9H-11H)", "Cycle 3 (9H-11H)", "Livello 3 (9H-11H)" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 4L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Upper Secondary School - High School", "Sekundarstufe II - Gymnasium", "Secondaire II - Gymnase", "Secondaria II - Ginnasio" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 5L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Upper Secondary Level – Vocational Maturity", "Sekundarstufe II - Berufsmaturität", "Secondaire II - Maturité professionnelle", "Secondaria II - Maturità professionale" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 6L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Upper Secondary School – CFC", "Sekundarstufe II - CFC", "Secondaire II - CFC", "Secondaria II - CFC" });

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 7L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Upper Secondary Level - ECG", "Sekundarstufe II - ECG", "Secondaire II - ECG", "Secondaria II - ECG" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 1L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "French", "Französisch", "Français", "Francese" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 2L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "German", "Deutsch", "Allemand", "Tedesco" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 3L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "English", "Englisch", "Anglais", "Inglese" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 4L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Mathematics", "Mathematik", "Mathématiques", "Matematica" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 5L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Natural Sciences", "Naturwissenschaften", "Sciences naturelles", "Scienze della natura" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 6L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "History", "Geschichte", "Histoire", "Storia" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 7L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Geography", "Geographie", "Géographie", "Geografia" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 8L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Physical Education", "Physikalische Bildung", "Éducation physique", "Educazione fisica" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 9L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Visual Arts", "Visuelle Künste", "Arts visuels", "Arti visive" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 10L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Music", "Musik", "Musique", "Musica" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 11L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Economics and Law", "Wirtschaft und Recht", "Économie et droit", "Economia e diritto" });

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 12L,
                columns: new[] { "Name", "NameDe", "NameFr", "NameIt" },
                values: new object[] { "Computer Science", "Informatik", "Informatique", "Informatica" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameDe",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameFr",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameDe",
                table: "SchoolGrades");

            migrationBuilder.DropColumn(
                name: "NameFr",
                table: "SchoolGrades");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "SchoolGrades");

            migrationBuilder.DropColumn(
                name: "NameDe",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "NameFr",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "NameDe",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "NameFr",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "NameDe",
                table: "BookCategories");

            migrationBuilder.DropColumn(
                name: "NameFr",
                table: "BookCategories");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "BookCategories");

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 1L,
                column: "Name",
                value: "Manuels scolaires");

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 2L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Dictionnaires, encyclopédies et atlas scolaires.", "Ouvrages de référence" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 3L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Français, allemand, italien, anglais et langues locales.", "Langues" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 4L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Supports de mathématiques pour cycles 1 à secondaire II.", "Mathématiques" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 5L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Biologie, chimie, physique et sciences de la nature.", "Sciences naturelles" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 6L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Histoire suisse, géographie et éducation citoyenne.", "Histoire et géographie" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 7L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Introduction à l'économie, droit et gestion.", "Économie et droit" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 8L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Supports pour arts visuels, musique et activités créatives.", "Arts et musique" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 9L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Initiation au numérique, algorithmique et outils informatiques.", "Informatique" });

            migrationBuilder.UpdateData(
                table: "BookCategories",
                keyColumn: "BookCategoryId",
                keyValue: 10L,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Manuels liés aux filières CFC et maturité professionnelle.", "Formation professionnelle" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Label",
                keyValue: "DE",
                column: "Name",
                value: "Deutsch");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Label",
                keyValue: "FR",
                column: "Name",
                value: "Français");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 1L,
                column: "Name",
                value: "Papeterie");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 2L,
                column: "Name",
                value: "Matériel d'écriture");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 3L,
                column: "Name",
                value: "Cahiers et classeurs");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 4L,
                column: "Name",
                value: "Matériel artistique");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 5L,
                column: "Name",
                value: "Matériel scientifique");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 6L,
                column: "Name",
                value: "Équipement sportif");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 7L,
                column: "Name",
                value: "Matériel informatique");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 8L,
                column: "Name",
                value: "Sacs et cartables");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 9L,
                column: "Name",
                value: "Calculatrices");

            migrationBuilder.UpdateData(
                table: "ProductCategories",
                keyColumn: "ProductCategoryId",
                keyValue: 10L,
                column: "Name",
                value: "Accessoires de laboratoire");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 1L,
                column: "Name",
                value: "Cycle 1 (1H-4H)");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 2L,
                column: "Name",
                value: "Cycle 2 (5H-8H)");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 3L,
                column: "Name",
                value: "Cycle 3 (9H-11H)");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 4L,
                column: "Name",
                value: "Secondaire II - Gymnase");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 5L,
                column: "Name",
                value: "Secondaire II - Maturité professionnelle");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 6L,
                column: "Name",
                value: "Secondaire II - CFC");

            migrationBuilder.UpdateData(
                table: "SchoolGrades",
                keyColumn: "SchoolGradeId",
                keyValue: 7L,
                column: "Name",
                value: "Secondaire II - ECG");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 1L,
                column: "Name",
                value: "Français");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 2L,
                column: "Name",
                value: "Allemand");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 3L,
                column: "Name",
                value: "Anglais");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 4L,
                column: "Name",
                value: "Mathématiques");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 5L,
                column: "Name",
                value: "Sciences naturelles");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 6L,
                column: "Name",
                value: "Histoire");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 7L,
                column: "Name",
                value: "Géographie");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 8L,
                column: "Name",
                value: "Éducation physique");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 9L,
                column: "Name",
                value: "Arts visuels");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 10L,
                column: "Name",
                value: "Musique");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 11L,
                column: "Name",
                value: "Économie et droit");

            migrationBuilder.UpdateData(
                table: "Subjects",
                keyColumn: "SubjectId",
                keyValue: 12L,
                column: "Name",
                value: "Informatique");
        }
    }
}
