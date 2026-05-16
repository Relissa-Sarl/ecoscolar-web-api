namespace EcoscolarWebApi.Utils.DTOs;

/// <summary>
/// Chaînes identiques au champ <c>type</c> de <see cref="EcoscolarWebApi.Utils.DTOs.Advert.AdvertReadDto"/> (mapping dans <c>FromEntity</c>, fichier Advert/AdvertDto.cs).
/// À utiliser pour les DTO catalogue (<see cref="AdvertSummaryDto"/> / <see cref="AdvertDetailDto"/>) sans dupliquer une enum ni modifier ce fichier tiers.
/// </summary>
public static class CatalogAdvertTypeCodes
{
    public const string Book = "BOOK";
    public const string Product = "PRODUCT";
    public const string Service = "SERVICE";
}
