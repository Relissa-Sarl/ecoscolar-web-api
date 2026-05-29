using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoScolarWebApi.Models;

[Table("UserSearchAlerts")]
public class UserSearchAlert
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public string? Q { get; set; }
    public string? Isbn { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Subjects { get; set; }
    public string? Grade { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
