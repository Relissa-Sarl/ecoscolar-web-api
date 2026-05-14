using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Subject")]
    public class Subjects
    {
        public long SubjectId { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
    }
}
