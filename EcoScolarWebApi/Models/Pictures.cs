using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Pictures")]
    public class Pictures
    {
        [Key]
        public long PictureId { get; set; }

        [Required]
        [StringLength(500)]
        public string Label { get; set; }

        [Required]
        public long AdvertId { get; set; }

        [ForeignKey("AdvertId")]
        public virtual PhysicalItems Advert { get; set; } = null!;
    }
}
