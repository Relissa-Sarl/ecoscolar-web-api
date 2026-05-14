using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("Adverts")]
    public class Adverts
    {
        public long AdvertId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime NotificationDate { get; set; }
        public AdvertStatus Status { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
    }
}
