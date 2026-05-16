namespace EcoscolarWebApi.Utils.DTOs;

/// <summary>
/// Chaînes identiques au mapping <see cref="Advert.AdvertReadDto.FromEntity"/> dans <c>Advert/AdvertDto.cs</c>.
/// À utiliser pour les DTO catalogue mock (<see cref="AdvertSummaryDto"/> / <see cref="AdvertDetailDto"/>) sans dupliquer une enum ni modifier ce fichier tiers.
/// </summary>
public static class CatalogAdvertTypeCodes
{
    public const string Book = "BOOK";
    public const string Product = "PRODUCT";
    public const string Service = "SERVICE";
}
