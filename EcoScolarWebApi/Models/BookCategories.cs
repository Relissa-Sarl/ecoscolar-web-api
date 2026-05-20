using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("BookCategories")]
    public class BookCategories
    {
        [Key]
        public long BookCategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        //public virtual ICollection<Books> Books { get; set; } = new List<Books>();
    }
}
