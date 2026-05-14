using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Services")]
    public class AdvertServices : Adverts
    {
        public string StudyLevel { get; set; }
        public long SubjectId { get; set; }
        public long SchoolGradeId { get; set; }
        public Language TeachingLanguage { get; set; }
        public string SpecificStudyLevel { get; set; }
        public virtual Subjects Subject { get; set; }
        public virtual SchoolGrades SchoolGrade { get; set; }
    }
}
