using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Utils.DTOs.ReferenceData
{
	public class BookCategoryCreateUpdateDto
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Required]
		[StringLength(1000)]
		public string Description { get; set; }
	}
}
