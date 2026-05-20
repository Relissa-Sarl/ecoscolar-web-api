namespace EcoscolarWebApi.Utils.DTOs
{
	public class AdvertDetailDto
	{
		public Guid Id { get; set; }
		public string Type { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public string? Isbn { get; set; }
		public string? Category { get; set; }
		public string? Subjects { get; set; }
		public string? Grade { get; set; }
		public string Description { get; set; } = string.Empty;
		public string? ImageUrl { get; set; }
	}
}
