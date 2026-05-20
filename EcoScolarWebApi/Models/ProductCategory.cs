using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("ProductCategories")]
public class ProductCategory
{
    [Key]
    public long ProductCategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(1000)]
    public string Description { get; set; }
}