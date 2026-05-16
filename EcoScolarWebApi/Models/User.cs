using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    public class User : IdentityUser
    {
        // === User properties ===

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string BirthdayDate { get; set; }

        [ForeignKey(nameof(Location))]
        public int LocationId { get; set; }

        // === Navigation properties ===
        public Location Location { get; set; }

        // === Many-to-many relationship with Language through UserLanguage ===
        public ICollection<UserLanguage> Languages { get; set; } = new List<UserLanguage>();
    }
}
