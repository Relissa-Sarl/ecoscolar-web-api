namespace EcoscolarWebApi.Utils.DTOs
{
	public class AdvertSummaryDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
		/// <summary>Même sémantique que <see cref="EcoscolarWebApi.Utils.DTOs.Advert.AdvertReadDto"/> <c>type</c>. Valeurs : <see cref="CatalogAdvertTypeCodes"/>.</summary>
		public string Type { get; set; } = string.Empty;
		public string? Isbn { get; set; } = null;
		public string? Category { get; set; }
		public string? Subject { get; set; }
		public string? Grade { get; set; }
    }
}
