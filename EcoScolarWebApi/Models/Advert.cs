using EcoScolarWebApi.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Adverts")]
public class Advert
{
    [Key]
    public long AdvertId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; }

    [Required] 
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime NotificationDate { get; set; }

    [Required]
    public AdvertStatus Status { get; set; }

    [Required]
    public string SellerId { get; set; }

    [ForeignKey(nameof(SellerId))] // TODO : Rename to SellerId
    public virtual User Seller { get; set; }
}
