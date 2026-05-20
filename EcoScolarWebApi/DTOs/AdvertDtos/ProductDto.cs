using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.AdvertDtos
{
    public record ProductReadDto(long id, string title, string description, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo, 
        List<string> pictures, Condition condition, decimal? weight, long? productCategoryId, string? productCategoryLabel)
    {
        public static ProductReadDto FromEntity(PhysicalItems entity)
        {
            return new ProductReadDto(
                id: entity.AdvertId,
                title: entity.Title,
                description: entity.Description,
                price: entity.Price,
                publicationDate: entity.CreatedAt,
                notificationDate: entity.NotificationDate,
                status: entity.Status,
                userId: entity.UserId,
                sellerPseudo: entity.User?.UserName ?? "Anonyme",
                condition: entity.Condition,
                weight: entity.Weight ?? null,
                productCategoryId: entity.ProductCategoryId ?? null,
                productCategoryLabel: entity.ProductCategories?.Name ?? null,
                pictures: entity.Pictures?.Select(p => p.Label).ToList() ?? new List<string>()
            );
        }
    }

    /// <summary>
    /// DTO used for creating new product adverts, inheriting from AdvertCreateDto and adding specific properties related to products, such as an array of image URLs and the condition of the product.
    /// </summary>
    /// <param name="Title">The title of the product advert</param>
    /// <param name="Description">The description of the product advert</param>
    /// <param name="Price">The price of the product advert</param>
    /// <param name="UserId">The ID of the user who is creating the product advert</param>
    /// <param name="Images">The array of image URLs for the product advert</param>
    /// <param name="Condition">The condition of the product advert</param>
    /// <param name="ProductCategoryId">The ID of the product category to which the product advert belongs</param>
    public record ProductCreateDto(string Title, string Description, decimal Price, string UserId, Pictures[] Images, Condition Condition, long? ProductCategoryId = null)
        : AdvertCreateDto(Title, Description, Price, UserId)
    {
        /// <summary>
        /// Converts the ProductCreateDto to a PhysicalItems entity.
        /// </summary>
        /// <returns>The PhysicalItems entity</returns>
        public PhysicalItems ToEntity()
        {
            var product = new PhysicalItems();
            this.MapToEntity(product);
            return product;
        }

        /// <summary>
        /// Maps the properties of the ProductCreateDto to an existing Adverts entity, specifically to a PhysicalItems entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public override void MapToEntity(Advert entity)
        {
            base.MapToEntity(entity);
            if (entity is PhysicalItems product)
            {
                product.Condition = Condition;
                product.ProductCategoryId = ProductCategoryId;
                product.Pictures = Images.Select(img => new Pictures { Label = img.Label }).ToList();
            }
        }
    }
}
