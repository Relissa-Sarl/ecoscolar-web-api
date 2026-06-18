using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Users;

public class FlagRequestDto
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
