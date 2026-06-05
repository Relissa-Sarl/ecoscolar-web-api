using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("SearchAlerts")]
public class SearchAlert
{
    [Key]
    public int ResearchId { get; set; }

    [Required]
    public string AdvertSearch { get; set; } = string.Empty;

    [Required]
    public string AdvertType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxPrice { get; set; }

    public string? ISBN { get; set; }

    // === Foreign Keys ===

    public long? SubjectId { get; set; } 

    public long? BookCategoryId { get; set; }

    [Required]
    public string UserId { get; set; }

    // === Navigation Properties ===

    [ForeignKey(nameof(SubjectId))]
    public virtual Subject? Subject { get; set; }

    [ForeignKey(nameof(BookCategoryId))]
    public virtual BookCategory? BookCategory { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}