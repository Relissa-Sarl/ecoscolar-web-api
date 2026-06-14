using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.Adverts;

/// <summary>
/// DTO used to return PhysicalItem information in a simplified form, suitable for listing and basic display purposes. 
/// It includes essential details such as the PhysicalItem's ID, type, title, price, publication date, notification date, status, user ID of the seller, seller's pseudo (username), 
/// and the URL of the primary image if available. 
/// This DTO is designed to provide a concise overview of an PhysicalItem without exposing all the underlying details of the entity.
/// </summary>
/// <param name="Id">The ID of the PhysicalItem</param>
/// <param name="Type">The type of the PhysicalItem</param>
/// <param name="Title">The title of the PhysicalItem</param>
/// <param name="Price">The price of the PhysicalItem</param>
/// <param name="PublicationDate">The date when the PhysicalItem was published</param>
/// <param name="NotificationDate">The date when the PhysicalItem was notified</param>
/// <param name="Status">The status of the PhysicalItem</param>
/// <param name="UserId">The ID of the user who created the PhysicalItem</param>
/// <param name="SellerPseudo">The pseudo (username) of the seller</param>
/// <param name="PrimaryImage">The URL of the primary image of the PhysicalItem</param>
/// <param name="BuyerName">The nickname of the buyer once the advert has been sold. Empty string when the advert has not yet been sold.</param>
/// <param name="Review">The review details of the advert/transaction if sold and reviewed.</param>
/// <param name="ExpiresInDays">The number of days until the advert expires</param>
public record AdvertReadDto(long Id, string Type, string Title, decimal Price, DateTime PublicationDate, DateTime NotificationDate, AdvertStatus Status, string UserId, string SellerPseudo, string? PrimaryImage, string BuyerName, int ExpiresInDays, ReviewDto? Review = null, long? TransactionId = null, string? TransactionStatus = null)
{
	/// <summary>
	/// Factory method to create an AdvertReadDto from an PhysicalItem entity.
	/// It determines the type of PhysicalItem based on the specific subclass of PhysicalItem (Books, PhysicalItems, AdvertServices) and extracts the primary image if available.
	/// </summary>
	/// <param name="entity">The PhysicalItem entity to convert</param>
	/// <returns>The AdvertReadDto instance</returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static AdvertReadDto FromEntity(Advert entity)
	{
		return FromEntity(entity, null);
	}

	/// <summary>
	/// Factory method to create an AdvertReadDto from an PhysicalItem entity, with a review.
	/// It determines the type of PhysicalItem based on the specific subclass of PhysicalItem (Books, PhysicalItems, AdvertServices) and extracts the primary image if available.
	/// </summary>
	/// <param name="entity">The PhysicalItem entity to convert</param>
	/// <param name="review">Optional review to attach</param>
	/// <returns>The AdvertReadDto instance</returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static AdvertReadDto FromEntity(Advert entity, ReviewDto? review, string buyerName = "")
	{
		string type = entity switch
		{
			Models.Book => "BOOK",
			Models.PhysicalItem => "PRODUCT",
			Models.TutoringAdvert => "SERVICE",
			_ => throw new InvalidOperationException("Unknown PhysicalItem type")
		};

		string? primaryImage = (entity as Models.PhysicalItem)?.Pictures?.FirstOrDefault()?.Label;
        // BuyerName is populated by the controller after joining with the Transaction table.

		return new AdvertReadDto(
			Id: entity.AdvertId,
			Type: type,
			Title: entity.Title,
			Price: entity.Price,
			PublicationDate: entity.CreatedAt,
			NotificationDate: entity.NotificationDate,
			Status: entity.Status,
			UserId: entity.SellerId,
			SellerPseudo: entity.Seller?.Nickname ?? entity.Seller?.UserName ?? "Anonyme",
			PrimaryImage: primaryImage,
			BuyerName: buyerName ?? string.Empty,
			ExpiresInDays: EcoScolarWebApi.Helpers.AdvertExpirationHelper.GetExpiresInDays(entity.CreatedAt),
			Review: review,
			TransactionId: null,
			TransactionStatus: null
		);
	}
}

/// <summary>
/// DTO used for creating new adverts, serving as a base class for specific PhysicalItem types like services and products.
/// </summary>
/// <param name="Title">The title of the PhysicalItem</param>
/// <param name="Description">The description of the PhysicalItem</param>
/// <param name="Price">The price of the PhysicalItem</param>
/// <param name="UserId">The ID of the user who is creating the PhysicalItem</param>
public record AdvertCreateDto(string Title, string Description, decimal Price, string UserId)
{
	/// <summary>
	/// Converts the AdvertBaseCreateDto to an PhysicalItem entity. 
	/// This method initializes the common properties of the PhysicalItem, such as title, description, price, user ID, status, creation date, and notification date.
	/// </summary>
	/// <returns>The PhysicalItem entity</returns>
	public Advert ToEntity()
	{
		var Adverts = new Advert();
		this.MapToEntity(Adverts);
		return Adverts;
	}

	/// <summary>
	/// Maps the properties of the AdvertBaseCreateDto to an existing PhysicalItem entity.
	/// </summary>
	/// <param name="entity">The PhysicalItem entity to map to</param>
	public virtual void MapToEntity(Advert entity)
	{
		entity.Title = Title;
		entity.Description = Description;
		entity.Price = Price;
		entity.Status = AdvertStatus.ACTIVE;
		entity.SellerId = UserId;
		entity.NotificationDate = DateTime.UtcNow;
	}
}