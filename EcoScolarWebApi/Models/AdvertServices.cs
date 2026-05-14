using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Services")]
    public class AdvertServices : Adverts
    {
        public string StudyLevel { get; set; }
        public Subjects SubjectId { get; set; }
        public SchoolGrades SchoolGradeId { get; set; }
    }
}
