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

    // === Payment fields (generic — payments milestone P3) ===

    // Number of units purchased (e.g. tutoring hours). Defaults to 1 for single-item purchases.
    public int Quantity { get; set; } = 1;

    // Unit price frozen at purchase time (= hourly rate for a tutoring package).
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    // Total amount charged to the buyer (Quantity * UnitPrice, + PlatformFee).
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Stripe PaymentIntent id, required to issue refunds.
    public string? StripePaymentIntentId { get; set; }

    // Stripe Transfer id, set when funds are released to the seller.
    public string? StripeTransferId { get; set; }

    // === Tutoring escrow fields (B) ===

    // Set when the tutor declares the service rendered; anchors the auto-release countdown.
    public DateTime? TutorConfirmedAt { get; set; }

    // Backstop validity deadline for a tutoring package (payment date + configured validity).
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