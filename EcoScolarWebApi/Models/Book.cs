using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Books")]
public class Book : PhysicalItem
{
    [Required]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Publisher { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Edition { get; set; } = string.Empty;

    [Required]
    public Enums.LanguageEnum WrittenLanguage { get; set; }

    [Required]
    public long BookCategoryId { get; set; }

    [ForeignKey("BookCategoryId")]
    public virtual BookCategory BookCategory { get; set; } = null!;
}
