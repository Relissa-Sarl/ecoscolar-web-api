using Microsoft.AspNetCore.Identity;

namespace EcoscolarWebApi.Models;

public class User : IdentityUser
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }

	public virtual ICollection<UserFavorite> Favorites { get; set; } = [];
}
