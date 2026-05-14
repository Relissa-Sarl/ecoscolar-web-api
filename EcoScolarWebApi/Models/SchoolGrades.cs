using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("SchoolGrade")]
    public class SchoolGrades
    {
        public long SchoolGradeId { get; set; }
        public string Name { get; set; }
        public string SchoolGrade { get; set; }
        public virtual ICollection<AdvertServices> AdvertServices { get; set; } = new List<AdvertServices>();
    }
}
