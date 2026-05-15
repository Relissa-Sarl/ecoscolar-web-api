using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Services")]
    public class AdvertServices : Adverts
    {
        [Required]
        [StringLength(50)]
        public string StudyLevel { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [Required]
        public long SchoolGradeId { get; set; }

        [Required]
        public Language TeachingLanguage { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subjects Subject { get; set; }

        [ForeignKey("SchoolGradeId")]
        public virtual SchoolGrades SchoolGrade { get; set; }
    }
}
