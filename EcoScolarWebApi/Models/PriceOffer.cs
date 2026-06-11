using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("PriceOffers")]
public class PriceOffer
{
    [Required]
    public long AdvertId { get; set; }

    [Required]
    public string BuyerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === Navigation Properties ===

    [ForeignKey(nameof(AdvertId))]
    public virtual Advert? Advert { get; set; }

    [ForeignKey(nameof(BuyerId))]
    public virtual User? Buyer { get; set; }
}