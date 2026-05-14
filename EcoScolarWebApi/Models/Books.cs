using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Books")]
    public class Books : PhysicalItems
    {
        public int ISBN { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string Edition { get; set; }
        public BookCategories BookCategoriesId { get; set; }
    }
}
