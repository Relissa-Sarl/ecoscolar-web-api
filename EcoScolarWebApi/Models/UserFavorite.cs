using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bogus.DataSets;

namespace EcoScolarWebApi.Models;

[Table("UserFavorites")]
public class UserFavorite
{
	[Key]
	public int Id { get; set; }
	[Required]
	public required string UserId { get; set; }

	[ForeignKey("SellerId")]
	public virtual User? User { get; set; }

	[Required]
	public long AdvertId { get; set; }

	[ForeignKey("AdvertId")]
	public virtual Advert? Advert { get; set; }
}
