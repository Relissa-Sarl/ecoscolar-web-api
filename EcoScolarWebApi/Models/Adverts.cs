using EcoscolarWebApi.Utils.enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Adverts")]
    public class Adverts
    {
        public long AdvertId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime NotificationDate { get; set; }
        public AdvertStatus Status { get; set; }
        public User User { get; set; }
    }
}
