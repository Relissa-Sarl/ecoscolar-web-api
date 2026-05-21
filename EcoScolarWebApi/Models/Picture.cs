using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Pictures")]
public class Picture
{
    [Key]
    public long PictureId { get; set; }

    [Required]
    [StringLength(500)]
    public string Label { get; set; }

    [Required]
    public long AdvertId { get; set; }

    [ForeignKey("AdvertId")]
    public virtual PhysicalItem Adverts { get; set; } = null!;
}
