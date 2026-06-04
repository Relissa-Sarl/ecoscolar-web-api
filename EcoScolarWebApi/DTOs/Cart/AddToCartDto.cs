using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Cart
{
    public class AddToCartDto
    {
        [Required]
        public long AdvertId { get; set; }
    }
}
