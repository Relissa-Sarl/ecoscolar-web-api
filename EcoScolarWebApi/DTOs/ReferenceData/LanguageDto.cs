using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.ReferenceData
{
    // DTO to create or update a language
    public record LanguageRequest(
        [Required][StringLength(10)] string Label,
        [Required][StringLength(100)] string Name,
        [Required][StringLength(100)] string NameFr,
        [Required][StringLength(100)] string NameDe,
        [Required][StringLength(100)] string NameIt
    );

    // DTO to return language data in responses
    public record LanguageResponse(
        string Label,
        string Name,
        string NameFr,
        string NameDe,
        string NameIt
    );
}
