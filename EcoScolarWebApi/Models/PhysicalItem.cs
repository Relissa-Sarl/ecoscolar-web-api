using EcoScolarWebApi.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("PhysicalItems")]
public class PhysicalItem : Advert
{
    [Required]
    public Condition Condition { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Weight { get; set; }
    public virtual ICollection<Picture> Pictures { get; set; } = new List<Picture>();
    public long? ProductCategoryId { get; set; }

    [ForeignKey(nameof(ProductCategoryId))]
    public virtual ProductCategory? ProductCategory { get; set; }
}
