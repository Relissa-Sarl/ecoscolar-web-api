namespace EcoscolarWebApi.Utils.DTOs
{
	public class AdvertSummaryDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
	}
}	