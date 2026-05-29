using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("TutoringAdverts")]
public class TutoringAdvert : Advert // TODO : Rename in MCD Services -> to ServiceAdvert
{
    [Required]
    [StringLength(50)]
    public string StudyLevel { get; set; }

    [Required]
    public long SubjectId { get; set; }

    [Required]
    public long SchoolGradeId { get; set; }

    [Required]
    public Enums.LanguageEnum TeachingLanguage { get; set; } // TODO : Check because we have a Language table, maybe we can use it instead of an enum.
                                                             // A TutoringAdvert can be taught in multiple languages, so maybe we need a many-to-many relationship between TutoringAdvert and Language.

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; }

    [ForeignKey("SchoolGradeId")]
    public virtual SchoolGrade SchoolGrade { get; set; }
}
