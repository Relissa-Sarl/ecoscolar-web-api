using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("SchoolGrade")]
    public class SchoolGrades
    {
        [Key]
        public long SchoolGradeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string SchoolGrade { get; set; }

        public virtual ICollection<AdvertServices> AdvertServices { get; set; } = new List<AdvertServices>();
    }
}
