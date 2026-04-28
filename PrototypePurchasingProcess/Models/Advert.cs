namespace PrototypePurchasingProcess.Models
{
    public class Advert
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public DateTime PublicationDate { get; set; }
        public DateTime NotificationDate { get; set; }
        public string Status { get; set; }
    }
}
