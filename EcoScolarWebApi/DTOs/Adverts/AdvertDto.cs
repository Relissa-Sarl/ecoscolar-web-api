using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.Adverts;

/// <summary>
/// DTO used to return Adverts information in a simplified form, suitable for listing and basic display purposes. 
/// It includes essential details such as the Adverts's ID, type, title, price, publication date, notification date, status, user ID of the seller, seller's pseudo (username), 
/// and the URL of the primary image if available. 
/// This DTO is designed to provide a concise overview of an Adverts without exposing all the underlying details of the entity.
/// </summary>
/// <param name="Id">The ID of the Adverts</param>
/// <param name="Type">The type of the Adverts</param>
/// <param name="Title">The title of the Adverts</param>
/// <param name="Price">The price of the Adverts</param>
/// <param name="PublicationDate">The date when the Adverts was published</param>
/// <param name="NotificationDate">The date when the Adverts was notified</param>
/// <param name="Status">The status of the Adverts</param>
/// <param name="UserId">The ID of the user who created the Adverts</param>
/// <param name="SellerPseudo">The pseudo (username) of the seller</param>
/// <param name="PrimaryImage">The URL of the primary image of the Adverts</param>
public record AdvertReadDto(long Id, string Type, string Title, decimal Price, DateTime PublicationDate, DateTime NotificationDate, AdvertStatus Status, string UserId, string SellerPseudo, string? PrimaryImage)
{
	/// <summary>
	/// Factory method to create an AdvertReadDto from an Adverts entity.
	/// It determines the type of Adverts based on the specific subclass of Adverts (Books, PhysicalItems, AdvertServices) and extracts the primary image if available.
	/// </summary>
	/// <param name="entity">The Adverts entity to convert</param>
	/// <returns>The AdvertReadDto instance</returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static AdvertReadDto FromEntity(Advert entity)
	{
		string type = entity switch
		{
			Models.Book => "BOOK",
			Models.PhysicalItem => "PRODUCT",
			Models.TutoringAdvert => "SERVICE",
			_ => throw new InvalidOperationException("Unknown Adverts type")
		};

		string? primaryImage = (entity as Models.PhysicalItem)?.Pictures?.FirstOrDefault()?.Label;

		return new AdvertReadDto(
			Id: entity.AdvertId,
			Type: type,
			Title: entity.Title,
			Price: entity.Price,
			PublicationDate: entity.CreatedAt,
			NotificationDate: entity.NotificationDate,
			Status: entity.Status,
			UserId: entity.UserId,
			SellerPseudo: entity.User?.UserName ?? "Anonyme",
			PrimaryImage: primaryImage
		);
	}
}

/// <summary>
/// DTO used for creating new adverts, serving as a base class for specific Adverts types like services and products.
/// </summary>
/// <param name="Title">The title of the Adverts</param>
/// <param name="Description">The description of the Adverts</param>
/// <param name="Price">The price of the Adverts</param>
/// <param name="UserId">The ID of the user who is creating the Adverts</param>
public record AdvertCreateDto(string Title, string Description, decimal Price, string UserId)
{
	/// <summary>
	/// Converts the AdvertBaseCreateDto to an Adverts entity. 
	/// This method initializes the common properties of the Adverts, such as title, description, price, user ID, status, creation date, and notification date.
	/// </summary>
	/// <returns>The Adverts entity</returns>
	public Advert ToEntity()
	{
		var Adverts = new Advert();
		this.MapToEntity(Adverts);
		return Adverts;
	}

	/// <summary>
	/// Maps the properties of the AdvertBaseCreateDto to an existing Adverts entity.
	/// </summary>
	/// <param name="entity">The Adverts entity to map to</param>
	public virtual void MapToEntity(Advert entity)
	{
		entity.Title = Title;
		entity.Description = Description;
		entity.Price = Price;
		entity.Status = AdvertStatus.ACTIVE;
		entity.UserId = UserId;
		entity.NotificationDate = DateTime.UtcNow.AddMonths(3);
	}
}
