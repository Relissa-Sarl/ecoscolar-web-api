using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Utils.DTOs.ReferenceData
{
	public class SchoolGradeCreateUpdateDto
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Required]
		[StringLength(100)]
		public string SchoolGrade { get; set; }
	}
}
