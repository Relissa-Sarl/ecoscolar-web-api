using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Utils.DTOs.ReferenceData
{
	public class SubjectCreateUpdateDto
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Required]
		[StringLength(100)]
		public string Subject { get; set; }
	}
}
