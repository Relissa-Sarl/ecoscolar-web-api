using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("PublicComments")]
public class PublicComment
{
    [Key]
    public int CommentId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Answer { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AnsweredAt { get; set; }

    // === Foreign Keys ===

    [Required]
    public long AdvertId { get; set; }

    [Required]
    public string AuthorId { get; set; }

    // === Navigation Properties ===

    [ForeignKey(nameof(AdvertId))]
    public virtual Advert? Advert { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public virtual User? Author { get; set; }
}