using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // === Foreign Keys ===

    [Required]
    public string ReviewerId { get; set; }

    [Required]
    public long TransactionId { get; set; }

    // === Navigation Properties ===

    [ForeignKey(nameof(ReviewerId))]
    public virtual User? Reviewer { get; set; }

    [ForeignKey(nameof(TransactionId))]
    public virtual Transaction? Transaction { get; set; }
}