using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Subject")]
    public class Subjects
    {
        [Key]
        public long SubjectId { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public virtual ICollection<AdvertServices> AdvertServices { get; set; } = new List<AdvertServices>();
    }
}
