using EcoScolarWebApi.DTOs.AdvertDtos;
using EcoScolarWebApi.DTOs.Adverts;

namespace EcoScolarWebApi.DTOs;

/// <summary>
/// Chaînes identiques au champ <c>type</c> de <see cref="AdvertReadDto"/> (mapping dans <c>FromEntity</c>, fichier Adverts/AdvertDto.cs).
/// À utiliser pour les DTO catalogue (<see cref="AdvertSummaryDto"/> / <see cref="AdvertDetailDto"/>) sans dupliquer une enum ni modifier ce fichier tiers.
/// </summary>
public static class CatalogAdvertTypeCodes
{
    public const string Books = "Books";
    public const string Product = "PRODUCT";
    public const string Service = "SERVICE";
}
