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

    public long? ProductCategoryId { get; set; }

    public long? SchoolGradeId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinPrice { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === Foreign Keys ===

    public long? SubjectId { get; set; }

    public long? BookCategoryId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    // === Navigation Properties ===

    [ForeignKey(nameof(SubjectId))]
    public virtual Subject? Subject { get; set; }

    [ForeignKey(nameof(BookCategoryId))]
    public virtual BookCategory? BookCategory { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(ProductCategoryId))]
    public virtual ProductCategory? ProductCategory { get; set; }

    [ForeignKey(nameof(SchoolGradeId))]
    public virtual SchoolGrade? SchoolGrade { get; set; }
}