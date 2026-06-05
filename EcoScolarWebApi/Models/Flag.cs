using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Flags")]
public class Flag
{
    [Key]
    public int FlagId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // === Foreign Keys ===

    [Required]
    public string ReporterId { get; set; } 

    [Required]
    public string FlaggedId { get; set; }

    // === Navigation Properties ===

    [ForeignKey(nameof(ReporterId))]
    public virtual User? Reporter { get; set; }

    [ForeignKey(nameof(FlaggedId))]
    public virtual User? Flagged { get; set; }
}