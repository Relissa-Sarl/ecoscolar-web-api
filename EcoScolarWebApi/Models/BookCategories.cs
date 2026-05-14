using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("BookCategories")]
    public class BookCategories
    {
        public long BookCategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
