using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("SchoolGrades")]
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
    }
}
