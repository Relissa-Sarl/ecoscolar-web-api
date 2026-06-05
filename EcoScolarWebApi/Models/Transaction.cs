using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Transactions")]
public class Transaction
{
    [Key]
    public long TransactionId { get; set; }

    [Required]
    public long AdvertId { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    public string Status { get; set; } = string.Empty;

    public DateTime? ExpirationReservationTime { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PlatformFee { get; set; }

    [Required]
    public bool BuyerConsent { get; set; } = false;

    [Required]
    public bool SellerConsent { get; set; } = false;

    public DateTime? ReminderDate { get; set; }

    public string? StripeSessionId { get; set; }

    // === Foreign Keys ===

    [Required]
    public string BuyerId { get; set; } = default!;

	// === Navigation Properties ===

	[ForeignKey(nameof(AdvertId))]
    public virtual Advert Advert { get; set; } = default!;

	[ForeignKey(nameof(BuyerId))]
    public virtual User Buyer { get; set; } = default!;
}