using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EcoScolarWebApi.Models;

public class User : IdentityUser
{
    // === Seller properties ===

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Nickname { get; set; }

    public string? DateOfBirth { get; set; }

    [Required]
    public bool IsOnboarded { get; set; } = false;

    public bool IsBanned { get; set; } = false;

    // Stripe Connect (Express) recipient account used to pay out the seller
    public string? StripeAccountId { get; set; }

    [Required]
    public bool IsStripeOnboarded { get; set; } = false;

    // === Foreign keys ===

    [ForeignKey(nameof(Location))]
    public int? LocationId { get; set; }

    // === Navigation properties ===
    public Location? Location { get; set; }
    public virtual ICollection<Review> ReviewsGiven { get; set; } = [];
    public virtual ICollection<Review> ReviewsReceived { get; set; } = [];

    public virtual ICollection<UserLanguage> Languages { get; set; } = new List<UserLanguage>();
    public virtual ICollection<UserFavorite> Favorites { get; set; } = [];
    public virtual ICollection<CartItem> CartItems { get; set; } = [];
}
