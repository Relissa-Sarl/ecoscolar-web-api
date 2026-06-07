using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

/// <summary>
/// Message in a support ticket conversation (user or support team).
/// </summary>
[Table("SupportTicketMessages")]
public class SupportTicketMessage
{
    [Key]
    public int Id { get; set; }

    public int TicketId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public bool IsFromSupport { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TicketId))]
    public virtual SupportTicket Ticket { get; set; } = null!;
}
