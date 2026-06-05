using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.ReferenceData;

public class ProductCategoryCreateUpdateDto
{
	[Required]
	[StringLength(100)]
	public string Name { get; set; }

    [Required]
    [StringLength(100)]
    public string NameFr { get; set; }

    [Required]
    [StringLength(100)]
    public string NameDe { get; set; }

    [Required]
    [StringLength(100)]
    public string NameIt { get; set; }

    [Required]
	[StringLength(1000)]
	public string Description { get; set; }
}
