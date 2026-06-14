using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.ReferenceData;

public class SchoolGradeCreateUpdateDto
{
	[Required]
	[StringLength(100)]
	public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NameFr { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NameDe { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NameIt { get; set; } = string.Empty;

    [Required]
	[StringLength(100)]
	public string SchoolGrade { get; set; } = string.Empty;
}
