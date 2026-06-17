using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("TutoringAdverts")]
public class TutoringAdvert : Advert // TODO : Rename in MCD Services -> to ServiceAdvert
{
    [Required]
    [StringLength(50)]
    public string StudyLevel { get; set; } = string.Empty;

    [Required]
    public long SubjectId { get; set; }

    [Required]
    public long SchoolGradeId { get; set; }

    [Required]
    public Enums.LanguageEnum TeachingLanguage { get; set; } // TODO : Check because we have a Language table, maybe we can use it instead of an enum.
                                                             // A TutoringAdvert can be taught in multiple languages, so maybe we need a many-to-many relationship between TutoringAdvert and Language.

    // === Tutoring sale (Price = hourly rate) ===

    // Maximum number of hours a student can book in a single reservation (drives the booking modal cap).
    public int MaxHours { get; set; } = 1;

    // Optional minimum number of hours per reservation.
    public int? MinHours { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("SchoolGradeId")]
    public virtual SchoolGrade SchoolGrade { get; set; } = null!;
}
