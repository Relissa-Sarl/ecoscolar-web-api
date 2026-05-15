using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Books")]
    public class Books : PhysicalItems
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
        public Language WrittenLanguage { get; set; }
        
        [Required]
        public long BookCategoryId { get; set; }

        [ForeignKey("BookCategoryId")]
        public virtual BookCategories BookCategory { get; set; }
    }
}
