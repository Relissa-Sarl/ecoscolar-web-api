using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Pictures")]
    public class Pictures
    {
        public long PictureId { get; set; }
        public string Label { get; set; }
        public long AdvertId { get; set; }
        public virtual PhysicalItems Advert { get; set; } = new PhysicalItems();
    }
}
