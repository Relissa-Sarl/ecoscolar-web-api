using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Languages")]
public class Language
{
    [Key]
    public string Label { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty; // TODO - Rename in MCD Language -> Name

    [Required]
    public string NameFr { get; set; } = string.Empty;

    [Required]
    public string NameDe { get; set; } = string.Empty;

    [Required]
    public string NameIt { get; set; } = string.Empty;

    // === Many-to-many relationships ===

    public ICollection<UserLanguage> UserLanguages { get; set; } = [];
}
