using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("BookCategories")]
    public class BookCategories
    {
        [Key]
        public long BookCategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Books> Books { get; set; } = new List<Books>();
    }
}
