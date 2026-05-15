using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Services")]
    public class AdvertServices : Adverts
    {
        public string StudyLevel { get; set; }
        [ForeignKey("Subjects")]
        public long SubjectId { get; set; }
        [ForeignKey("SchoolGrades")]
        public long SchoolGradeId { get; set; }
        public Language TeachingLanguage { get; set; }
        public virtual Subjects Subject { get; set; }
        public virtual SchoolGrades SchoolGrade { get; set; }
    }
}
