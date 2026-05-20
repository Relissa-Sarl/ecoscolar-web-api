using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key]
        public string Label { get; set; }

        [Required]
        public string Name { get; set; }

        // === Many-to-many relationships ===

        public ICollection<UserLanguage> UserLanguages { get; set; } = [];
    }
}
