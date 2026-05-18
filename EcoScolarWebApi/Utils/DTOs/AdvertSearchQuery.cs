namespace EcoscolarWebApi.Utils.DTOs
{
    /// <summary>
    /// Query parameters for advert search (summary list).
    /// </summary>
    /// <remarks>
    /// For <c>GET api/v1/adverts/summary</c>, only <see cref="Q"/> and <see cref="Isbn"/> are honored by
    /// <see cref="Services.AdvertSearchService"/> and <see cref="Services.FakeAdvertSearchService"/>.
    /// <see cref="Category"/>, <see cref="MinPrice"/>, <see cref="MaxPrice"/>, <see cref="Subject"/>, and
    /// <see cref="Grade"/> are reserved for future filtering and are currently ignored by the server.
    /// </remarks>
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
