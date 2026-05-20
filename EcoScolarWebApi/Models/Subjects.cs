using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Subjects")]
    public class Subjects
    {
        [Key]
        public long SubjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        public virtual ICollection<AdvertServices> AdvertServices { get; set; } = new List<AdvertServices>();
    }
}
