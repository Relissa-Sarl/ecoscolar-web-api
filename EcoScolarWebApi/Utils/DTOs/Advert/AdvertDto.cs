using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.Enums;

namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    /// <summary>
    /// DTO used to return advert information in a simplified form, suitable for listing and basic display purposes. 
    /// It includes essential details such as the advert's ID, type, title, price, publication date, notification date, status, user ID of the seller, seller's pseudo (username), 
    /// and the URL of the primary image if available. 
    /// This DTO is designed to provide a concise overview of an advert without exposing all the underlying details of the entity.
    /// </summary>
    /// <param name="id">The ID of the advert</param>
    /// <param name="type">The type of the advert</param>
    /// <param name="title">The title of the advert</param>
    /// <param name="price">The price of the advert</param>
    /// <param name="publicationDate">The date when the advert was published</param>
    /// <param name="notificationDate">The date when the advert was notified</param>
    /// <param name="status">The status of the advert</param>
    /// <param name="userId">The ID of the user who created the advert</param>
    /// <param name="sellerPseudo">The pseudo (username) of the seller</param>
    /// <param name="primaryImage">The URL of the primary image of the advert</param>
    public record AdvertReadDto(long id, string type, string title, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo, string? primaryImage)
    {
        /// <summary>
        /// Factory method to create an AdvertReadDto from an Adverts entity.
        /// It determines the type of advert based on the specific subclass of Adverts (Books, PhysicalItems, AdvertServices) and extracts the primary image if available.
        /// </summary>
        /// <param name="entity">The Adverts entity to convert</param>
        /// <returns>The AdvertReadDto instance</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static AdvertReadDto FromEntity(Models.Adverts entity)
        {
            string type = entity switch
            {
                Models.Books => "BOOK",
                Models.PhysicalItems => "PRODUCT",
                Models.AdvertServices => "SERVICE",
                _ => throw new InvalidOperationException("Unknown advert type")
            };
            
            string? primaryImage = (entity as Models.PhysicalItems)?.Pictures?.FirstOrDefault()?.Label;

            return new AdvertReadDto(
                id: entity.AdvertId,
                type: type,
                title:entity.Title,
                price: entity.Price,
                publicationDate: entity.CreatedAt,
                notificationDate: entity.NotificationDate,
                status: entity.Status,
                userId: entity.UserId,
                sellerPseudo: entity.User.UserName ?? "Anonyme",
                primaryImage: primaryImage
            );
        }
    }

    /// <summary>
    /// DTO used for creating new adverts, serving as a base class for specific advert types like services and products.
    /// </summary>
    /// <param name="Title">The title of the advert</param>
    /// <param name="Description">The description of the advert</param>
    /// <param name="Price">The price of the advert</param>
    /// <param name="UserId">The ID of the user who is creating the advert</param>
    public record AdvertCreateDto(string Title, string Description, decimal Price, string UserId)
    {
        /// <summary>
        /// Converts the AdvertBaseCreateDto to an Adverts entity. 
        /// This method initializes the common properties of the advert, such as title, description, price, user ID, status, creation date, and notification date.
        /// </summary>
        /// <returns>The Adverts entity</returns>
        public Adverts ToEntity()
        {
            var advert = new Adverts();
            this.MapToEntity(advert);
            return advert;
        }

        /// <summary>
        /// Maps the properties of the AdvertBaseCreateDto to an existing Adverts entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public virtual void MapToEntity(Adverts entity)
        {
            entity.Title = Title;
            entity.Description = Description;
            entity.Price = Price;
            entity.Status = AdvertStatus.ACTIVE;
            entity.UserId = UserId;
            entity.NotificationDate = DateTime.UtcNow.AddMonths(3);
        }
    }
}
