using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("SchoolGrades")]
    public class SchoolGrade
    {
        [Key]
        public long SchoolGradeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Grade { get; set; }

        public virtual ICollection<AdvertService> AdvertServices { get; set; } = [];
    }
}
