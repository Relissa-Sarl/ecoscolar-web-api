using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Models
{
    public class Language
    {
        [Key]
        public string Label { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<UserLanguage> UserLanguages { get; set; } = new List<UserLanguage>();
    }
}
