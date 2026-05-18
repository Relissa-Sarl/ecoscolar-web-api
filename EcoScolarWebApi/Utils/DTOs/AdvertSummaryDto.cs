namespace EcoscolarWebApi.Utils.DTOs
{
	public class AdvertSummaryDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public string Type { get; set; } = string.Empty;
		public string? Isbn { get; set; } = null;
		public string? Category { get; set; }
		public string? Subject { get; set; }
		public string? Grade { get; set; }
    }
}
