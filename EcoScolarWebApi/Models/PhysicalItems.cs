using EcoScolarWebApi.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models
{
    [Table("PhysicalItems")]
    public class PhysicalItems : Advert
    {
        [Required]
        public Condition Condition { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; }
        public virtual ICollection<Pictures> Pictures { get; set; } = new List<Pictures>();
        public long? ProductCategoryId { get; set; }

        [ForeignKey(nameof(ProductCategoryId))]
        public virtual ProductCategories? ProductCategories { get; set; }
    }
}
