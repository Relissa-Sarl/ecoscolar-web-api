using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("Pictures")]
public class Picture
{
    [Key]
    public long PictureId { get; set; }

    /// <summary>Kept for backward compatibility. Use <see cref="PublicUrl"/> for new code.</summary>
    [Required]
    [StringLength(500)]
    public string Label { get; set; } = string.Empty;

    /// <summary>MinIO object key, e.g. adverts/42/V1StGXR8.jpg</summary>
    [StringLength(500)]
    public string? ObjectKey { get; set; }

    /// <summary>MIME type of the stored file, e.g. image/jpeg</summary>
    [StringLength(50)]
    public string? ContentType { get; set; }

    /// <summary>Publicly accessible URL served from MinIO.</summary>
    [StringLength(1000)]
    public string? PublicUrl { get; set; }

    /// <summary>Display order within the advert (0 = primary image).</summary>
    public int SortOrder { get; set; }

    [Required]
    public long PhysicalItemId { get; set; }

    [ForeignKey("PhysicalItemId")]
    public virtual PhysicalItem PhysicalItem { get; set; } = null!;
}
