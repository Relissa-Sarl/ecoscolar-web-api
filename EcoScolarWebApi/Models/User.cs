using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EcoscolarWebApi.Models
{
    public class User : IdentityUser
    {
        // === User properties ===

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Nickname { get; set; }

        public string? BirthdayDate { get; set; }

        [Required]
        public bool IsOnboarded { get; set; } = false;

        // === Foreign keys ===

        [ForeignKey(nameof(Location))]
        public int? LocationId { get; set; }

        // === Navigation properties ===
        public Location? Location { get; set; }

        // === Many-to-many relationships ===
        public ICollection<UserLanguage> Languages { get; set; } = new List<UserLanguage>();
    }
}
