using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
namespace EcoScolarWebApi.DTOs.Cart
{
    /// <summary>
    /// DTO used for return the advert information for the cart
    /// </summary>
    /// <param  name="AdvertId">The TD of the advert</param>
    /// <param name="Type">The type of the advert</param>
    /// <param name="Title">The title of the Adverts</param>
    /// <param name="Price">The price of the Adverts</param>
    /// <param name="SellerPseudo">The pseudo (username) of the seller</param>
    /// <param name="PrimaryImage">The URL of the primary image of the Adverts</param>
    public class CartItemDto
    {
        [Required]
        public long AdvertId { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string SellerPseudo { get; set; }

        [Required]
        public string? PrimaryImage { get; set; }
    }
}   
