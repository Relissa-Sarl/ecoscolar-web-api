using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models;

[Table("UserFavorites")]
public class UserFavorite
{
	[Key]
	public int Id { get; set; }
	[Required]
	public required string UserId { get; set; }

	[ForeignKey("UserId")]
	public virtual User? User { get; set; }

	[Required]
	public long AdvertId { get; set; }

	[ForeignKey("AdvertId")]
	public virtual Adverts? Advert { get; set; }
}
