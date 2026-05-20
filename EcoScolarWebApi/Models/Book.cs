using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models
{
    [Table("Books")]
    public class Book : PhysicalItem
    {
        [Required]
        [StringLength(20)]
        public string ISBN { get; set; }

        [Required]
        [StringLength(150)]
        public string Author { get; set; }

        [Required]
        [StringLength(150)]
        public string Publisher { get; set; }

        [Required]
        [StringLength(150)]
        public string Edition { get; set; }

        [Required]
        public Enums.Language WrittenLanguage { get; set; }
        
        [Required]
        public long BookCategoryId { get; set; }

        [ForeignKey("BookCategoryId")]
        public virtual BookCategory BookCategories { get; set; }
    }
}
