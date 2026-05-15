namespace EcoscolarWebApi.Utils.DTOs
{
    public class AdvertSearchQuery
    {
        public string? Q { get; set; }
        public string? Isbn { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; } = null;
        public decimal? MaxPrice { get; set; }
        public string? Subject { get; set; }
        public string? Grade { get; set; }
    }
}
