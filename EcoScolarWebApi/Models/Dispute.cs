using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Disputes")]
public class Dispute
{
    [Key]
    public int DisputeId { get; set; }

    [Required]
    public long TransactionId { get; set; }

    [Required]
    public EcoScolarWebApi.Enums.DisputeReason Reason { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    public EcoScolarWebApi.Enums.TicketStatus Status { get; set; } = EcoScolarWebApi.Enums.TicketStatus.Pending;

    public string? Resolution { get; set; }

    // === Navigation Properties ===

    [ForeignKey(nameof(TransactionId))]
    public virtual Transaction? Transaction { get; set; } 
}
