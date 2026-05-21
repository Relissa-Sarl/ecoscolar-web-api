using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Services")]
public class TutoringAdvert : Advert
{
    [Required]
    [StringLength(50)]
    public string StudyLevel { get; set; }

    [Required]
    public long SubjectId { get; set; }

    [Required]
    public long SchoolGradeId { get; set; }

    [Required]
    public Enums.LanguageEnum TeachingLanguage { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; }

    [ForeignKey("SchoolGradeId")]
    public virtual SchoolGrade SchoolGrade { get; set; }
}
