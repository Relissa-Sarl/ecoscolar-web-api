using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoScolarWebApi.Enums;

namespace EcoScolarWebApi.Models;

[Table("Transactions")]
public class Transaction
{
    [Key]
    public long TransactionId { get; set; }

    [MaxLength(32)]
    public string? OrderNumber { get; set; }

    [Required]
    public long AdvertId { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;

    public DateTime? ExpirationReservationTime { get; set; }

    public DateTime? ShippedDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PlatformFee { get; set; }

    [Required]
    public bool BuyerConsent { get; set; } = false;

    [Required]
    public bool SellerConsent { get; set; } = false;

    public DateTime? ReminderDate { get; set; }

    public string? StripeSessionId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public string? StripeTransferId { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime? TutorConfirmedAt { get; set; }

    public DateTime? PackageExpiresAt { get; set; }

    // === Foreign Keys ===

    [Required]
    public string BuyerId { get; set; } = default!;

	// === Navigation Properties ===

	[ForeignKey(nameof(AdvertId))]
    public virtual Advert Advert { get; set; } = default!;

	[ForeignKey(nameof(BuyerId))]
    public virtual User Buyer { get; set; } = default!;
}
