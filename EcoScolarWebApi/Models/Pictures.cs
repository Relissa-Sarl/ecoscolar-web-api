using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Pictures")]
    public class Pictures
    {
        [Key]
        public long PictureId { get; set; }
        public string Label { get; set; }
        [ForeignKey("Advert")]
        public long AdvertId { get; set; }
        public virtual PhysicalItems Advert { get; set; } = new PhysicalItems();
    }
}
