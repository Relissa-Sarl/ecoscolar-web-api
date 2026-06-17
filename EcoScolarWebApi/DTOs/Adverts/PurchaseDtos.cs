namespace EcoScolarWebApi.DTOs;

public record ReviewDto(int Rating, string? Comment);

/// <summary>
/// DTO for representing a user's purchase history.
/// </summary>
/// <param name="Id">The unique ID of the purchase/transaction.</param>
/// <param name="AdvertId">The ID of the related advert.</param>
/// <param name="AdvertTitle">The title of the purchased advert.</param>
/// <param name="Price">The purchase price.</param>
/// <param name="PurchaseDate">The date of the purchase.</param>
/// <param name="Status">The status of the purchase (e.g., "completed", "pending").</param>
/// <param name="ImageUrl">An optional URL for the advert's thumbnail image.</param>
/// <param name="SellerName">The name or pseudo of the seller.</param>
/// <param name="Review">The review details of the purchase/transaction.</param>
public record PurchaseReadDto(
    string Id,
    string AdvertId,
    string AdvertTitle,
    decimal Price,
    DateTime PurchaseDate,
    string Status,
    string? ImageUrl,
    string SellerName,
    ReviewDto? Review = null,
    string? OrderNumber = null
);