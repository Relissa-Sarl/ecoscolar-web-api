using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace EcoScolarWebApi.DTOs.Adverts;

public record ProductReadDto(long Id, string Title, string Description, decimal Price, DateTime PublicationDate, DateTime NotificationDate, AdvertStatus Status, string UserId, string SellerPseudo,
	List<string> Pictures, PhysicalItemCondition Condition, decimal? Weight, long? ProductCategoryId, string? ProductCategoryLabel)
{
	public static ProductReadDto FromEntity(PhysicalItem entity)
	{
		return new ProductReadDto(
			Id: entity.AdvertId,
			Title: entity.Title,
			Description: entity.Description,
			Price: entity.Price,
			PublicationDate: entity.CreatedAt,
			NotificationDate: entity.NotificationDate,
			Status: entity.Status,
			UserId: entity.UserId,
			SellerPseudo: entity.User?.UserName ?? "Anonyme",
			Condition: entity.Condition,
			Weight: entity.Weight ?? null,
			ProductCategoryId: entity.ProductCategoryId ?? null,
			ProductCategoryLabel: entity.ProductCategory?.Name ?? null,
			Pictures: entity.Pictures?.Select(p => p.Label).ToList() ?? []
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
public record ProductCreateDto(string Title, string Description, decimal Price, string UserId, string[]? Images, PhysicalItemCondition Condition, long? ProductCategoryId = null)
	: AdvertCreateDto(Title, Description, Price, UserId)
{
	/// <summary>
	/// Converts the ProductCreateDto to a PhysicalItems entity.
	/// </summary>
	/// <returns>The PhysicalItems entity</returns>
	public new PhysicalItem ToEntity()
	{
		var product = new PhysicalItem();
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
		if (entity is PhysicalItem product)
		{
			product.Condition = Condition;
			product.ProductCategoryId = ProductCategoryId;
			if (Images.IsNullOrEmpty())
			{
				product.Pictures.Add(new Picture { Label = "https://picsum.photos/800/600" });
			}
			else
            {
                product.Pictures = Images.Select(img => new Picture { Label = img }).ToList();
            }
		}
	}
}
