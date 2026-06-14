using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("UserLanguages")]
public class UserLanguage
{
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public Language Language { get; set; } = null!;

    public string LanguageLevel { get; set; } = string.Empty;
}