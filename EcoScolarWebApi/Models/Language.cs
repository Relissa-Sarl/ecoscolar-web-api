using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Languages")]
public class Language
{
    [Key]
    public string Label { get; set; }

    [Required]
    public string Name { get; set; } // TODO - Rename in MCD Language -> Name

    [Required]
    public string NameFr { get; set; }

    [Required]
    public string NameDe { get; set; }

    [Required]
    public string NameIt { get; set; }

    // === Many-to-many relationships ===

    public ICollection<UserLanguage> UserLanguages { get; set; } = [];
}
