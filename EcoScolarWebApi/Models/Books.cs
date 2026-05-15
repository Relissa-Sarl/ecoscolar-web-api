using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Books")]
    public class Books : PhysicalItems
    {
        // ISBN isn't a int or a long because it can start with 0 and can have dashes, so we use string to store it
        public string ISBN { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string Edition { get; set; }
        public Language WrittenLanguage { get; set; }
        [ForeignKey("BookCategories")]
        public long BookCategoryId { get; set; }
        public virtual BookCategories BookCategory { get; set; }
    }
}
