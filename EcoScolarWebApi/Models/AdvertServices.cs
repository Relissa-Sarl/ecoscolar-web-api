using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models
{
    [Table("Services")]
    public class AdvertServices : Advert
    {
        [Required]
        [StringLength(50)]
        public string StudyLevel { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [Required]
        public long SchoolGradeId { get; set; }

        [Required]
        public Enums.Language TeachingLanguage { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subjects Subjects { get; set; }

        [ForeignKey("SchoolGradeId")]
        public virtual SchoolGrades SchoolGrades { get; set; }
    }
}
