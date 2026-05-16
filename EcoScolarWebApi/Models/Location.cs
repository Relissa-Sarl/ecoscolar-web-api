using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}