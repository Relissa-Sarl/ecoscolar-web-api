using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("TutoringAdverts")]
public class TutoringAdvert : Advert
{
    [Required]
    [StringLength(50)]
    public string StudyLevel { get; set; } = string.Empty;

    [Required]
    public long SubjectId { get; set; }

    [Required]
    public long SchoolGradeId { get; set; }

    [Required]
    public Enums.LanguageEnum TeachingLanguage { get; set; }

    public int MaxHours { get; set; } = 10;

    public int? MinHours { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("SchoolGradeId")]
    public virtual SchoolGrade SchoolGrade { get; set; } = null!;
}
